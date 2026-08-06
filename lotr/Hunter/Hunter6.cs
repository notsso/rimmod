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
}
