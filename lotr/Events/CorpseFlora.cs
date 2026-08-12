using System.Collections.Generic;
using System.Linq;
using System;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

namespace lotr {
    public class IncidentWorker_CorpseFloraGrown : IncidentWorker {
        // НАСТРОЙКИ БАЛАНСА И ЛОРА
        private const int MinCorpsesInCluster = 5; // Минимальный размер "горы трупов" для накопления духовности
        private const float SearchRadius = 5.9f;    // Радиус плотности кучи тел
        private const int MaxPlantsToSpawn = 3;    // Сколько максимум цветов может прорасти за один инцидент

        // 1. ПРОВЕРКА УСЛОВИЙ (Вызывается игрой перед запуском события)
        protected override bool CanFireNowSub(IncidentParms parms) {
            if (!base.CanFireNowSub(parms)) return false;

            // Нам нужна карта, на которой происходит событие
            Map map = (Map)parms.target;
            if (map == null) return false;

            // Ищем, есть ли на карте хотя бы один подходящий эпицентр смерти
            return FindCorpseEpicenters(map).Any();
        }

        // 2. ВЫПОЛНЕНИЕ ИНЦИДЕНТА
        protected override bool TryExecuteWorker(IncidentParms parms) {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // Получаем список всех трупов, вокруг которых скопилась духовность
            List<Corpse> epicenters = FindCorpseEpicenters(map);
            if (epicenters.Count == 0) return false;

            int plantsSpawned = 0;
            IntVec3 lastSpawnCell = IntVec3.Invalid;

            // Пытаемся заспавнить цветы рядом с этими местами
            foreach (var corpse in epicenters.InRandomOrder()) {
                if (plantsSpawned >= MaxPlantsToSpawn) break;

                // Ищем свободную клетку для мистического цветка в радиусе 2 клеток от трупа
                if (CellFinder.TryFindRandomCellNear(corpse.Position, map, 2, c => IsValidForBloodPlant(c, map), out IntVec3 spawnCell)) {
                    ThingDef plantDef = ThingDef.Named("lotr_CorpseLily");
                    if (plantDef != null) {
                        // Спавним багровый цветок
                        Plant newPlant = (Plant)GenSpawn.Spawn(plantDef, spawnCell, map, WipeMode.Vanish);
                        newPlant.Growth = 0.05f; // Появляется как маленький росток

                        // Эпичный визуальный эффект ударной волны/вспышки на 1 тик в клетке прорастания
                        FleckMaker.Static(spawnCell, map, FleckDefOf.ExplosionFlash, 0.6f);

                        lastSpawnCell = spawnCell;
                        plantsSpawned++;
                    }
                }
            }

            // Если хотя бы одно растение успешно проросло, выводим уведомление игроку
            if (plantsSpawned > 0) {
                if (PawnUtility.ShouldSendNotificationAbout(map.mapPawns.FreeColonists.FirstOrDefault())) {
                    Messages.Message("Трупы привлекли опасную флору.", new TargetInfo(lastSpawnCell, map), MessageTypeDefOf.PositiveEvent, true);
                }
                return true;
            }

            return false;
        }

        // Вспомогательный метод для поиска кучи трупов
        private List<Corpse> FindCorpseEpicenters(Map map) {
            List<Corpse> result = new List<Corpse>();

            // Собираем все трупы на земле
            List<Corpse> allCorpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse)
                .Cast<Corpse>()
                .Where(c => c != null && c.Spawned)
                .ToList();

            if (allCorpses.Count < MinCorpsesInCluster) return result;

            foreach (var corpse in allCorpses) {
                // Считаем соседей в радиусе
                int count = allCorpses.Count(other => other.Position.InHorDistOf(corpse.Position, SearchRadius));
                if (count >= MinCorpsesInCluster) {
                    result.Add(corpse);
                }
            }

            return result;
        }

        private bool IsValidForBloodPlant(IntVec3 cell, Map map) {
            if (!cell.InBounds(map)) return false;
            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain == null || terrain.IsWater) return false;

            List<Thing> thingList = cell.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++) {
                if (thingList[i].def.category == ThingCategory.Plant || thingList[i].def.category == ThingCategory.Building) {
                    return false;
                }
            }
            return true;
        }
    }

    public class Plant_CorpseLily : Plant {
        private const int ConsumeCheckInterval = 60;          // порог для счётчика (с учётом TickLong даёт ~3.3 сек)
        private const int DamageToCorpse = 10;                // урон трупу
        private const float GrowthPerAttack = 0.01f;          // прирост роста за атаку
        private const float ConsumeRadius = 6f;               // радиус поиска трупов
        private const float LivingAttackRadius = 10f;          // радиус атаки живых
        private const int BaseLivingDamage = 2;               // Начальный урон по живым
        private const float HediffSeverityPerHit = 0.15f;     // прирост тяжести дебаффа за удар

        private const float ReproductionGrowthThreshold = 1f;
        private const int ReproductionCooldownTicks = 30000; // 1 день (30000 тиков)
        private int reproductionCooldownTicksLeft = 0;

        private Corpse targetCorpse;
        private int tickCounter;

        public override float GrowthRate => 0f;

        public override void TickLong() {
            base.TickLong();
            if (!Spawned || Growth >= 1f) return;

            tickCounter += 200;
            if (tickCounter >= ConsumeCheckInterval) {
                tickCounter = 0;
                // 1. Атака живых существ
                TryAttackLiving();

                // 2. Если живых нет – поглощаем трупы
                TryConsumeCorpse();
            }

            if (Growth >= ReproductionGrowthThreshold) {
                reproductionCooldownTicksLeft -= 200; // TickLong уменьшает на 200
                if (reproductionCooldownTicksLeft <= 0) {
                    TryReproduce();
                    reproductionCooldownTicksLeft = ReproductionCooldownTicks;
                }
            }
        }

        private bool TryAttackLiving() {
            Pawn victim = FindLivingTarget();
            if (victim == null) return false;

            int damage = Mathf.RoundToInt(BaseLivingDamage * (1 + Growth * 2));
            DamageWorker.DamageResult res = victim.TakeDamage(new DamageInfo(DamageDefOf.Cut, damage, instigator: this));
            float consumedSpirituality = SpiritualityUtility.ConsumeSpirituality(victim, damage * 5, false);

            Growth = Mathf.Min(1f, Growth + (res.totalDamageDealt + consumedSpirituality / 10) / 100);

            HediffDef debuffDef = HediffDef.Named("CorpseLilyDebuff");
            if (debuffDef != null) {
                Hediff hediff = victim.health.hediffSet.GetFirstHediffOfDef(debuffDef);
                if (hediff == null) {
                    hediff = HediffMaker.MakeHediff(debuffDef, victim);
                    hediff.Severity = HediffSeverityPerHit;
                    victim.health.AddHediff(hediff);
                } else {
                    hediff.Severity = Mathf.Min(1f, hediff.Severity + HediffSeverityPerHit);
                }
            }

            FleckMaker.ThrowMicroSparks(victim.DrawPos, Map);
            // Log.Message($"[CorpseLily] {this} атаковал {victim.LabelShort} (урон {damage}, рост {Growth:P0})");
            return true;
        }

        private bool TryConsumeCorpse() {
            if (targetCorpse == null || !targetCorpse.Spawned || !targetCorpse.Position.InHorDistOf(Position, ConsumeRadius)) {
                targetCorpse = FindCorpseInRadius();
            }

            if (targetCorpse != null) {
                targetCorpse.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, DamageToCorpse, instigator: this));
                Growth = Mathf.Min(1f, Growth + GrowthPerAttack);

                FleckMaker.ThrowSmoke(targetCorpse.DrawPos, Map, 0.5f);

                if (targetCorpse.Destroyed) {
                    targetCorpse = null;
                }
            }
            return true;
        }

        private void TryReproduce() {
            // Ищем свободную клетку рядом для саженца
            if (CellFinder.TryFindRandomCellNear(Position, Map, 5, c => IsValidForOffspring(c), out IntVec3 spawnCell)) {
                Plant offspring = (Plant)GenSpawn.Spawn(ThingDef.Named("lotr_CorpseLily"), spawnCell, Map);
                offspring.Growth = 0.05f;
                Growth = 0.6f; // сбрасываем собственный рост
                FleckMaker.Static(spawnCell, Map, FleckDefOf.ExplosionFlash, 0.5f);
                Log.Message($"[CorpseLily] {this} дал потомство, рост снижен до {Growth:P0}");
            }
        }

        private bool IsValidForOffspring(IntVec3 cell) {
            if (!cell.InBounds(Map)) return false;
            TerrainDef terrain = cell.GetTerrain(Map);
            if (terrain == null || terrain.IsWater) return false;
            return cell.GetFirstThing<Plant>(Map) == null && cell.GetFirstThing<Building>(Map) == null;
        }

        private Corpse FindCorpseInRadius() {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, ConsumeRadius, true)) {
                if (!cell.InBounds(Map)) continue;
                Corpse corpse = cell.GetFirstThing<Corpse>(Map);
                if (corpse != null) return corpse;
            }
            return null;
        }

        private Pawn FindLivingTarget() {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, LivingAttackRadius, true)) {
                if (!cell.InBounds(Map)) continue;
                Pawn pawn = cell.GetFirstThing<Pawn>(Map);
                if (pawn != null && pawn.RaceProps.IsFlesh && !pawn.Dead)
                    return pawn;
            }
            return null;
        }
    }
}
