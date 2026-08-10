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

    // Способность hunter7 (pyromaniac): Огненная броня
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


    public class SummonedWeapon : ThingWithComps {
        public int ticksLeft = -1;

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", -1);
        }
    }

    public class SummonedFireWeapon : SummonedWeapon { }

    // Способность hunter7 (pyromaniac): огненная броня
    public class Hediff_FireLight : HediffWithComps {
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

    // Способность hunter7 (pyromaniac): тушение огня
    public class CompProperties_AbilityExtinguishFire : CompProperties_AbilityEffect {
        public float radius = 1f;
        public bool extinguishCaster = true;
        public bool extinguishAllies = true;
        public float spiritCostMultiplier = 1f;

        public CompProperties_AbilityExtinguishFire() {
            compClass = typeof(CompAbilityEffect_ExtinguishFire);
        }
    }

    public class CompAbilityEffect_ExtinguishFire : CompAbilityEffect {
        public new CompProperties_AbilityExtinguishFire Props => (CompProperties_AbilityExtinguishFire)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            Pawn caster = parent.pawn;
            if (caster == null || caster.Map == null) return;

            Map map = caster.Map;
            float radius = Props.radius;
            IntVec3 center = target.Cell;

            // 1. Тушение пожаров на клетках
            List<Thing> fireThings = map.listerThings.ThingsOfDef(ThingDefOf.Fire);
            foreach (Thing fire in fireThings.ToList()) {
                if (fire.Position.DistanceToSquared(center) <= radius * radius) {
                    fire.Destroy(DestroyMode.Vanish);
                }
            }

            // 2. Тушение горящих существ
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            foreach (Pawn pawn in pawns) {
                if (pawn == null || pawn.Dead) continue;
                if (pawn.Position.DistanceToSquared(center) > radius * radius) continue;

                if (pawn == caster && !Props.extinguishCaster) continue;
                if (pawn.Faction == caster.Faction && !Props.extinguishAllies) continue;

                if (pawn.IsBurning()) {
                    foreach (Thing thing in map.thingGrid.ThingsListAt(pawn.Position).ToList()) {
                        if (thing.def == ThingDefOf.Fire) {
                            thing.Destroy(DestroyMode.Vanish);
                        }
                    }
                    FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, 1.5f);
                }
            }

            // 3. Тушение горящих зданий
            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            foreach (Thing building in buildings) {
                if (building == null || building.Destroyed) continue;
                if (building.Position.DistanceToSquared(center) > radius * radius) continue;

                bool hasFire = false;
                foreach (Thing thing in map.thingGrid.ThingsListAt(building.Position).ToList()) {
                    if (thing.def == ThingDefOf.Fire) {
                        thing.Destroy(DestroyMode.Vanish);
                        hasFire = true;
                    }
                }
                if (hasFire) {
                    FleckMaker.ThrowSmoke(building.DrawPos, building.Map, 1.5f);
                }
            }

            // Визуальный эффект в центре
            for (int i = 0; i < 5; i++) {
                IntVec3 offset = new IntVec3(Rand.Range(-2, 2), 0, Rand.Range(-2, 2));
                Vector3 pos = (center + offset).ToVector3Shifted();
                FleckMaker.ThrowSmoke(pos, map, 0.8f);
            }
        }
    }

    public class Verb_ExtinguishFire : Verb_CastAbility {
        protected override bool TryCastShot() {
            if (this.ability != null) {
                this.ability.Activate(this.currentTarget, this.currentDestination);
                return true;
            }
            return false;
        }

        public override void DrawHighlight(LocalTargetInfo target) {
            base.DrawHighlight(target);

            if (!target.IsValid || CasterPawn == null)
                return;

            // Достаём радиус из CompProperties_AbilityExtinguishFire
            float radius = 1f; // по умолчанию
            if (this.ability != null) {
                var comp = this.ability.comps
                    .OfType<CompAbilityEffect_ExtinguishFire>()
                    .FirstOrDefault();
                if (comp != null)
                    radius = comp.Props.radius;
            }

            // Рисуем круг вокруг точки прицеливания
            Vector3 center = target.Cell.ToVector3Shifted();
            GenDraw.DrawRadiusRing(center.ToIntVec3(), radius, new Color(0.4f, 0.6f, 1f)); // голубоватый — цвет воды/тушения
        }
    }
}
