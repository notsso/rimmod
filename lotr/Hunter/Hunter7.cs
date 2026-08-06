using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public class Hunter7_Hediff : Hunter8_Hediff {
        public override float SpiritualityFactor => 5f;

        public Hunter7_Hediff() {
            maxProgressPerCategory = 0.4f;
        }
    }

    // Способность hunter7 (pyromaniac): огненные вороны
    public class CompProperties_AbilityLaunchFireRavens : CompProperties_AbilityEffect {
        public PawnKindDef ravenPawnKind;
        public int lifetime = 3600;
        public int maxCount = 3;

        public CompProperties_AbilityLaunchFireRavens() {
            compClass = typeof(CompAbilityEffect_LaunchFireRavens);
        }
    }

    public class CompAbilityEffect_LaunchFireRavens : CompAbilityEffect {
        public new CompProperties_AbilityLaunchFireRavens Props => (CompProperties_AbilityLaunchFireRavens)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Map map = caster.Map;

            if (map == null) return;

            int existingRavensCount = 0;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned) {
                if (p.def == LotrDefOf.lotr_FireRavenRace) {
                    CompFireRavenController controller = p.TryGetComp<CompFireRavenController>();
                    if (controller != null && controller.casterOwner == caster) {
                        existingRavensCount++;
                    }
                }
            }

            int maxTotalRavens = Props.maxCount;

            int ravensToSpawn = Mathf.Min(1, maxTotalRavens - existingRavensCount);

            if (ravensToSpawn <= 0) {
                return;
            }

            int spawnedCount = 0;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(caster.Position, 2f, false).InRandomOrder()) {
                if (spawnedCount >= ravensToSpawn) break;

                if (cell.Walkable(map) && !cell.Fogged(map)) {
                    Pawn raven = PawnGenerator.GeneratePawn(Props.ravenPawnKind, caster.Faction);
                    GenSpawn.Spawn(raven, cell, map);

                    CompFireRavenController controller = raven.TryGetComp<CompFireRavenController>();
                    if (controller != null) {
                        controller.casterOwner = caster;
                        controller.lifetime = Props.lifetime;
                    }

                    spawnedCount++;
                }
            }
        }
    }

    public class CompProperties_AbilityGiveHediff : CompProperties_AbilityEffect {
        public HediffDef hediffDef;
        public float severity = 0f;
        public bool applyToCaster = true;
        public bool showFleck = true;

        public CompProperties_AbilityGiveHediff() {
            compClass = typeof(CompAbilityEffect_GiveHediff);
        }
    }

    public class CompAbilityEffect_GiveHediff : CompAbilityEffect {
        public new CompProperties_AbilityGiveHediff Props => (CompProperties_AbilityGiveHediff)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);

            Pawn targetPawn = target.Pawn;
            if (targetPawn == null) return;

            if (targetPawn.health.hediffSet.HasHediff(Props.hediffDef)) return;

            Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, targetPawn);
            if (Props.severity > 0f)
                hediff.Severity = Props.severity;
            targetPawn.health.AddHediff(hediff);

            if (Props.showFleck)
                FleckMaker.Static(targetPawn.Position, targetPawn.Map, FleckDefOf.MicroSparks, 1.5f);
        }
    }


    public class SummonedWeapon : ThingWithComps { }

    public class SummonedFireWeapon : SummonedWeapon { }

    // класс для hediff firearmor
    public class Hediff_FireArmor : HediffWithComps {
        // Храним ссылку на заспавненную невидимую лампочку
        private ThingWithComps lightSource = null;

        public override void PostAdd(DamageInfo? dinfo) {
            base.PostAdd(dinfo);
            SpawnLight();
        }

        public override void PostRemoved() {
            base.PostRemoved();
            DespawnLight();
        }

        // Каждый тик проверяем позицию пешки
        public override void Tick() {
            base.Tick();

            if (this.pawn == null || !this.pawn.Spawned || this.pawn.Map == null) {
                DespawnLight();
                return;
            }

            // Если лампочки нет — спавним
            if (lightSource == null || !lightSource.Spawned) {
                SpawnLight();
            }
            // ИСПРАВЛЕНО: Если пешка сделала шаг на новую клетку
            else if (lightSource.Position != this.pawn.Position) {
                // Вместо багнутой телепортации позиции, мы пересоздаем свет в новой точке.
                // Это заставляет движок RimWorld мгновенно перерисовать световое пятно на экране!
                DespawnLight();
                SpawnLight();
            }
        }

        private void SpawnLight() {
            if (this.pawn == null || !this.pawn.Spawned || this.pawn.Map == null) return;
            if (lightSource != null && lightSource.Spawned) return;

            ThingDef lightDef = LotrDefOf.lotr_FireLightSpawner;
            if (lightDef != null) {
                lightSource = GenSpawn.Spawn(lightDef, this.pawn.Position, this.pawn.Map) as ThingWithComps;
            }
        }

        private void DespawnLight() {
            if (lightSource != null && lightSource.Spawned) {
                lightSource.Destroy(DestroyMode.Vanish);
                lightSource = null;
            }
        }
    }
}
