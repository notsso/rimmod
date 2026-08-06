using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public class Hunter6_Hediff : Hunter7_Hediff {
        public override float SpiritualityFactor => 10f;

        public Hunter6_Hediff() {
            maxProgressPerCategory = 1f;
        }
    }

    // Способность hunter6 (conspirator): огненное слияние (телепорт)
    public class CompProperties_FireTeleport : CompProperties_AbilityEffect {
        public ThingDef projectileDef;
        public float teleportSpeed = 50f;

        public CompProperties_FireTeleport() {
            compClass = typeof(CompAbilityEffect_FireTeleport);
        }
    }

    public class CompAbilityEffect_FireTeleport : CompAbilityEffect {
        public new CompProperties_FireTeleport Props => (CompProperties_FireTeleport)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            Pawn caster = parent.pawn;
            if (caster == null || !target.Cell.IsValid) return;

            Map map = caster.Map;
            IntVec3 startPos = caster.Position;

            ThingDef projectileDef = Props.projectileDef;
            if (projectileDef == null) return;

            Projectile_FireTeleport projectile = ThingMaker.MakeThing(projectileDef) as Projectile_FireTeleport;
            if (projectile == null) return;

            projectile.wasDrafted = caster.drafter.Drafted;
            projectile.teleportPawn = caster;

            projectile.Launch(
                launcher: caster,
                origin: startPos.ToVector3Shifted(),
                usedTarget: target,
                intendedTarget: target,
                hitFlags: ProjectileHitFlags.All
            );

            caster.DeSpawn();

            FleckMaker.ThrowSmoke(startPos.ToVector3(), map, 0.8f);
            FleckMaker.Static(startPos, map, FleckDefOf.MicroSparks, 1.0f);
        }
    }

    public class CompProperties_AbilityIncite : CompProperties_AbilityEffect {
        public float baseSuccessChance = 0.75f;
        public bool affectAllies = false;

        public CompProperties_AbilityIncite() {
            compClass = typeof(CompAbilityEffect_Incite);
        }
    }

    public class CompAbilityEffect_Incite : CompAbilityEffect {
        public new CompProperties_AbilityIncite Props => (CompProperties_AbilityIncite)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            Pawn targetPawn = target.Pawn;
            if (targetPawn == null || targetPawn.Dead) return;

            if (!Props.affectAllies && targetPawn.Faction == parent.pawn.Faction)
                return;

            float psychicSensitivity = targetPawn.GetStatValue(StatDefOf.PsychicSensitivity, true);
            float finalChance = Props.baseSuccessChance * psychicSensitivity;

            if (Rand.Chance(finalChance)) {
                targetPawn.mindState.mentalStateHandler.TryStartMentalState(
                    MentalStateDefOf.Berserk,
                    "Подстрекательство",
                    true
                );
                MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "Бунт!", 3f);
                FleckMaker.Static(targetPawn.Position, targetPawn.Map, FleckDefOf.PsycastAreaEffect, 1.5f);
            } else {
                MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "Сопротивление!", 2f);
            }
        }
    }

    // Состояние здоровья - замешательство
    public class Hediff_Confusion : HediffWithComps {
        private int tickCounter = 0;

        public override void Tick() {
            base.Tick();
            if (pawn == null || pawn.Destroyed) return;

            tickCounter++;
            // Каждые 3-6 секунд (150–300 тиков) заставляем цель сделать случайное действие
            if (tickCounter >= Rand.RangeInclusive(150, 300)) {
                tickCounter = 0;
                if (pawn.Spawned && !pawn.Dead) {
                    float random = Rand.Range(0, 1);
                    // 40% шанс остановиться
                    if (random < 0.4f) {
                        pawn.jobs.StopAll();
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "?", 2f);
                    }
                    // 20% шанс сменить цель
                    else if (random < 0.6f) {
                        Thing newTarget = FindRandomTarget(pawn);
                        if (newTarget != null) {
                            pawn.mindState.enemyTarget = newTarget;
                            pawn.jobs.StopAll();
                            Job newJob = JobMaker.MakeJob(JobDefOf.AttackMelee, newTarget);
                            pawn.jobs.StartJob(newJob);
                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Цель!", 2f);
                        }
                    }
                    // 40% – ничего не делать
                }
            }
        }

        private Thing FindRandomTarget(Pawn pawn) {
            // Ищем любого врага на карте в пределах 20 клеток
            return GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                PathEndMode.OnCell,
                TraverseParms.For(pawn),
                20f,
                x => x is Pawn p && p != pawn && p.Faction != pawn.Faction && !p.Dead
            );
        }
    }
}
