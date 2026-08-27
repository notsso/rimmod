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

    public class Plant_BloodRedChestnut : Plant {
        public override void PlantCollected(Pawn by, PlantDestructionMode plantDestructionMode) {
            Thing ingredient = ThingMaker.MakeThing(ThingDef.Named("lotr_BloodRedChestnut"));
            ingredient.stackCount = 1;
            GenPlace.TryPlaceThing(ingredient, Position, Map, ThingPlaceMode.Near);

            this.Destroy(DestroyMode.KillFinalizeLeavingsOnly);
        }
    }

    public class Plant_ShadowPoisonFlower : Plant { }
}
