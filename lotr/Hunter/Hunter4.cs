using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public class Hunter4_Hediff : Hunter5_Hediff {
        public override float SpiritualityFactor => 40f;

        public Hunter4_Hediff() {
            maxProgressPerCategory = 1f;
        }
    }

    public class Firestorm : Tornado {
        private int ticksToNextFlame = 0;
        public Pawn instigator;
        public int lifeTicks;
        public float minRadius;
        public float maxRadius;
        public int flameInterval;
        public float areaRadius;
        private int buildingDamageTicks = 0;
        private const int BuildingDamageInterval = 30;
        private IntVec3 moveTarget = IntVec3.Invalid;

        public override void SpawnSetup(Map map, bool respawningAfterLoad) {
            base.SpawnSetup(map, respawningAfterLoad);
            ticksToNextFlame = flameInterval;
        }

        protected override void Tick() {
            if (!this.Spawned) return;

            lifeTicks--;
            if (lifeTicks <= 0) {
                this.Destroy();
                return;
            }

            buildingDamageTicks--;
            if (buildingDamageTicks <= 0) {
                buildingDamageTicks = BuildingDamageInterval;
                DamageBuildingsInRadius(8f);
            }

            // Генерация огненных вспышек
            ticksToNextFlame--;
            if (ticksToNextFlame <= 0) {
                ticksToNextFlame = flameInterval;

                float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Rand.Range(2f, areaRadius);
                int x = Mathf.RoundToInt(Mathf.Cos(angle) * distance);
                int z = Mathf.RoundToInt(Mathf.Sin(angle) * distance);
                IntVec3 randomCell = this.Position + new IntVec3(x, 0, z);

                if (randomCell.InBounds(this.Map)) {
                    GenExplosion.DoExplosion(
                        center: randomCell,
                        map: this.Map,
                        radius: Rand.Range(minRadius, maxRadius),
                        damType: DamageDefOf.Flame,
                        instigator: this.instigator,
                        damAmount: 10,
                        armorPenetration: 3.0f,
                        explosionSound: null,
                        weapon: null,
                        projectile: null,
                        intendedTarget: null,
                        postExplosionSpawnThingDef: ThingDefOf.Filth_Ash,
                        postExplosionSpawnChance: 0.4f,
                        postExplosionSpawnThingCount: 1,
                        applyDamageToExplosionCellsNeighbors: true,
                        preExplosionSpawnThingDef: null,
                        preExplosionSpawnChance: 0f,
                        preExplosionSpawnThingCount: 1,
                        chanceToStartFire: 0.8f,
                        damageFalloff: false,
                        ignoredThings: new List<Thing> { this }
                    );
                }
            }
        }

        private void DamageBuildingsInRadius(float radius) {
            if (!this.Spawned) return;

            float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Rand.Range(0f, radius);
            int x = Mathf.RoundToInt(Mathf.Cos(angle) * distance);
            int z = Mathf.RoundToInt(Mathf.Sin(angle) * distance);
            IntVec3 center = this.Position + new IntVec3(x, 0, z);

            if (!center.InBounds(this.Map)) return;

            float blastRadius = 4.0f;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, blastRadius, true)) {
                if (!cell.InBounds(this.Map)) continue;
                Building building = cell.GetEdifice(this.Map);
                if (building != null && building.def?.building != null) {
                    DamageInfo dinfo = new DamageInfo(
                        DamageDefOf.Bomb,
                        amount: 50,
                        armorPenetration: 2.0f,
                        instigator: this.instigator
                    );
                    building.TakeDamage(dinfo);
                }
            }

            GenExplosion.DoExplosion(
                center: center,
                map: this.Map,
                radius: blastRadius,
                damType: DamageDefOf.Flame,
                instigator: this.instigator,
                damAmount: 0,
                armorPenetration: 0f,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 0,
                applyDamageToExplosionCellsNeighbors: false,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 0,
                chanceToStartFire: 0f,
                damageFalloff: false
            );
        }
    }

    public class CompProperties_Firestorm : CompProperties_AbilityEffect {
        public int lifeTicks = 300;          // время жизни в тиках
        public float minExplosionRadius = 1.5f;
        public float maxExplosionRadius = 2.9f;
        public int flameInterval = 10;
        public float areaRadius = 5;

        public CompProperties_Firestorm() {
            compClass = typeof(CompAbilityEffect_CastFirestorm);
        }
    }

    public class CompAbilityEffect_CastFirestorm : CompAbilityEffect {
        public new CompProperties_Firestorm Props => (CompProperties_Firestorm)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);
            Map map = this.parent.pawn.Map;

            if (map != null && target.Cell.InBounds(map)) {
                ThingDef tornadoDef = ThingDef.Named("FirestormThing");
                if (tornadoDef != null) {
                    Firestorm firestorm = (Firestorm)GenSpawn.Spawn(tornadoDef, target.Cell, map, WipeMode.Vanish);
                    firestorm.instigator = this.parent.pawn;

                    firestorm.lifeTicks = Props.lifeTicks;
                    firestorm.minRadius = Props.minExplosionRadius;
                    firestorm.maxRadius = Props.maxExplosionRadius;
                    firestorm.flameInterval = Props.flameInterval;
                    firestorm.areaRadius = Props.areaRadius;
                }
            }
        }
    }
}
