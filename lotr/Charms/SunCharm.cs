using System.Collections.Generic;
using System.Linq;

using RimWorld;

using UnityEngine;

using Verse;
using Verse.AI;

namespace lotr {
    public class Verb_SunBeam : Verb_CastBase {
        protected override bool TryCastShot() {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
                return false;

            SunBeam sunBeam = (SunBeam)GenSpawn.Spawn(
                ThingDef.Named("lotr_SunBeam"),
                currentTarget.Cell,
                caster.Map,
                WipeMode.Vanish);

            sunBeam.duration = 600;
            sunBeam.instigator = caster;
            sunBeam.weaponDef = (EquipmentSource != null) ? EquipmentSource.def : null;
            sunBeam.StartStrike();

            CompApparelReloadable reloadableCompSource = base.ReloadableCompSource;
            if (reloadableCompSource != null)
                reloadableCompSource.UsedOnce();

            return true;
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter) {
            needLOSToCenter = false;
            return SunBeam.Radius;
        }
    }

    public class SunBeam : OrbitalStrike {
        public override void StartStrike() {
            base.StartStrike();
            MoteMaker.MakePowerBeamMote(base.Position, base.Map); // визуальный столб света
        }

        protected override void Tick() {
            base.Tick();
            if (base.Destroyed)
                return;

            if (this.IsHashIntervalTick(10)) {
                DealDamageInRadius(Radius);
            }
        }

        private void DealDamageInRadius(float radius) {
            // Собираем все уникальные вещи в радиусе
            HashSet<Thing> things = new HashSet<Thing>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(base.Position, radius, true)) {
                if (!cell.InBounds(base.Map))
                    continue;

                List<Thing> cellThings = cell.GetThingList(base.Map);
                for (int i = 0; i < cellThings.Count; i++) {
                    things.Add(cellThings[i]);
                }
            }

            // Наносим урон каждой вещи
            foreach (Thing thing in things) {
                // Дистанция от центра луча
                float distance = thing.Position.DistanceTo(base.Position);
                // Множитель урона: 1.0 в центре, минимум 0.2 на краю
                float damageMult = Mathf.Clamp01(1f - (distance / radius)) * 0.5f + 0.5f;

                // Базовый урон (для трупов меньше)
                int baseDamage = (thing is Corpse)
                    ? SunBeam.CorpseLightDamageAmountRange.RandomInRange
                    : SunBeam.LightDamageAmountRange.RandomInRange;

                int damageAmount = Mathf.RoundToInt(baseDamage * damageMult);

                Pawn pawn = thing as Pawn;
                BattleLogEntry_DamageTaken battleLogEntry = null;
                if (pawn != null) {
                    battleLogEntry = new BattleLogEntry_DamageTaken(
                        pawn,
                        RulePackDefOf.DamageEvent_PowerBeam,
                        this.instigator as Pawn);
                    Find.BattleLog.Add(battleLogEntry);
                }

                DamageInfo damageInfo = new DamageInfo(
                    DefDatabase<DamageDef>.GetNamed("lotr_Light"),
                    damageAmount,
                    0f,
                    -1f,
                    this.instigator,
                    null,
                    this.weaponDef,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    null,
                    true,
                    true,
                    QualityCategory.Normal,
                    true,
                    false);

                thing.TakeDamage(damageInfo).AssociateWithLog(battleLogEntry);
            }
        }

        public const float Radius = 10f;
        private const int FiresStartedPerTick = 4; // используется для количества ударов в тик
        private static readonly IntRange LightDamageAmountRange = new IntRange(35, 50);
        private static readonly IntRange CorpseLightDamageAmountRange = new IntRange(1, 2);
        public static List<Thing> tmpThings = new List<Thing>();
    }
}