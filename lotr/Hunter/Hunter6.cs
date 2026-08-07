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

    // Способность hunter6 (conspirator): подстрекание
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

    // Способность hunter6 (conspirator): замешательство
    public class Hediff_Confusion : HediffWithComps {
        public override void PostAdd(DamageInfo? dinfo) {
            base.PostAdd(dinfo);

            if (pawn != null && pawn.Spawned && !pawn.Dead) {
                pawn.jobs.StopAll();

                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    MentalStateDefOf.Wander_Psychotic,
                    reason: "Эффект Замешательства",
                    forceWake: true,
                    transitionSilently: false
                );

                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "???", 3f);
            }
        }

        public override void PostRemoved() {
            base.PostRemoved();

            if (pawn != null && pawn.Spawned && pawn.InMentalState) {
                if (pawn.MentalStateDef == MentalStateDefOf.Wander_Psychotic) {
                    pawn.MentalState.RecoverFromState();
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Рассудок вернулся", 2.5f);
                }
            }
        }
    }
}
