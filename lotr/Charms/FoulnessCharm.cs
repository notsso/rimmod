using System.Linq;

using RimWorld;

using Verse;
using Verse.AI;

namespace lotr {
    public class Verb_FoulnessRoar : Verb_CastBase {
        private const float Radius = 20f;

        protected override bool TryCastShot() {
            if (caster == null || !caster.Spawned || !currentTarget.IsValid)
                return false;

            IntVec3 center = currentTarget.Cell;
            Map map = caster.Map;

            var targets = map.mapPawns.AllPawnsSpawned
                .Where(p => p.Position.InHorDistOf(center, Radius)
                            && p != caster
                            && !p.Dead);

            foreach (Pawn pawn in targets) {
                // Добавляем основной Hediff (замедление + ослабление сознания)
                if (!pawn.health.hediffSet.HasHediff(HediffDef.Named("lotr_HediffFoulnessRoar"))) {
                    pawn.health.AddHediff(HediffDef.Named("lotr_HediffFoulnessRoar"));
                }

                // Если цель — Потусторонний, наносим урон рассудку
                if (BeyonderUtility.IsBeyonder(pawn)) {
                    BeyonderUtility.AdjustSanityLoss(pawn, 0.5f, "Foulness Roar");
                }
            }

            CompApparelReloadable reloadableCompSource = base.ReloadableCompSource;
            if (reloadableCompSource != null)
                reloadableCompSource.UsedOnce();

            return true;
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter) {
            needLOSToCenter = false;
            return Radius;
        }
    }
}
