using System.Collections.Generic;
using System.Linq;

using RimWorld;

using Verse;
using Verse.AI;

namespace lotr {
    public class Verb_SlumberCharm : Verb_CharmBase {
        private const float Radius = 3f;

        protected override bool TryCastShot() {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
                return false;

            IntVec3 center = currentTarget.Cell;
            Map map = caster.Map;

            List<Pawn> pawns = map.mapPawns.AllPawnsSpawned
                .Where(p => p.Position.InHorDistOf(center, Radius)
                            && p != caster
                            && !p.Dead)
                .ToList();

            foreach (Pawn pawn in pawns) {
                pawn.health.AddHediff(HediffDef.Named("Hediff_ForcedSleep_Charm"));
            }

            CompApparelReloadable reloadableCompSource = base.ReloadableCompSource;
            if (reloadableCompSource != null)
                reloadableCompSource.UsedOnce();

            return true;
        }

        // Подсветка радиуса при наведении
        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter) {
            needLOSToCenter = false;
            return Radius;
        }
    }

    public class HediffCompProperties_ForcedSleep : HediffCompProperties {
        public HediffCompProperties_ForcedSleep() {
            compClass = typeof(HediffComp_ForcedSleep);
        }
    }

    public class HediffComp_ForcedSleep : HediffComp {
        private bool ShouldForceSleep => Pawn != null && !Pawn.Dead && !Pawn.Downed;

        public override void CompPostPostAdd(DamageInfo? dinfo) {
            base.CompPostPostAdd(dinfo);
            TryForceSleep();
        }

        public override void CompPostPostRemoved() {
            base.CompPostPostRemoved();
            WakeUp();
        }

        private void TryForceSleep() {
            if (!ShouldForceSleep)
                return;

            // Прерываем текущее занятие
            if (Pawn.jobs.curJob != null) {
                Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
            }

            // Опускаем потребность во сне до нуля, чтобы пешка не проснулась сразу
            Need_Rest rest = Pawn.needs?.rest;
            if (rest != null) {
                rest.CurLevel = rest.CurLevel - 0.2f;
            }

            // Создаём работу "лечь спать" на текущей клетке
            Job job = JobMaker.MakeJob(JobDefOf.LayDown, Pawn.Position);
            Pawn.jobs.StartJob(job, JobCondition.InterruptForced);
        }

        private void WakeUp() {
            if (Pawn == null || Pawn.Dead)
                return;

            // Останавливаем сон, если он активен
            if (Pawn.CurJobDef == JobDefOf.LayDown || Pawn.CurJobDef == JobDefOf.Wait_Asleep) {
                Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
            }

            // Небольшое восстановление потребности, чтобы пешка не легла обратно сразу
            Need_Rest rest = Pawn.needs?.rest;
            if (rest != null && rest.CurLevel < 0.2f) {
                rest.CurLevel = 0.2f;
            }
        }
    }
}
