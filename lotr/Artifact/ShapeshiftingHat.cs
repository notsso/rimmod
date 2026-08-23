using System.Linq;

using RimWorld;

using Verse;
using Verse.AI;

namespace lotr {
    // Лазурный свет: AoE вокруг кастера, замедляет врагов
    public class Verb_AzureLight : Verb_CastBase {
        private const float Radius = 12f;

        protected override bool TryCastShot() {
            Pawn casterPawn = caster as Pawn;
            if (casterPawn == null)
                return false;

            CompApparelReloadable reloadable = base.ReloadableCompSource;
            if (reloadable == null || !reloadable.CanBeUsed(out string reason))
                return false;

            reloadable.UsedOnce();

            foreach (Pawn pawn in casterPawn.Map.mapPawns.AllPawnsSpawned) {
                if (pawn.Position.InHorDistOf(casterPawn.Position, Radius)
                    && pawn.HostileTo(casterPawn)
                    && !pawn.Dead) {
                    pawn.health.AddHediff(HediffDef.Named("lotr_HediffAzureSlow"));
                }
            }

            return true;
        }

        // Подсветка радиуса при наведении
        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter) {
            needLOSToCenter = false;
            return Radius;
        }
    }

    // Водное лечение: плохо залечивает все ранения
    public class Verb_WaterHeal : Verb_CastBase {
        protected override bool TryCastShot() {
            Pawn targetPawn = currentTarget.Thing as Pawn;
            if (targetPawn == null || targetPawn.Dead)
                return false;

            CompApparelReloadable reloadable = base.ReloadableCompSource;
            if (reloadable == null || !reloadable.CanBeUsed(out string reason))
                return false;

            reloadable.UsedOnce();

            // Перевязываем все постоянные и временные раны с качеством 5%–15%
            foreach (Hediff_Injury injury in targetPawn.health.hediffSet.hediffs.OfType<Hediff_Injury>()) {
                if (injury.IsPermanent())
                    continue; // постоянные раны не перевязываются
                injury.Tended(Rand.Range(0.05f, 0.15f), 1f);
            }

            return true;
        }
    }

    public class CompProperties_ShapeshiftingHat : CompProperties {
        public float severityRiseIntervalTicks = 1000f;
        public float severityRisePerInterval = 0.01f;
        public CompProperties_ShapeshiftingHat() {
            compClass = typeof(CompShapeshiftingHat);
        }
    }

    public class CompShapeshiftingHat : ThingComp {
        public CompProperties_ShapeshiftingHat Props => (CompProperties_ShapeshiftingHat)props;

        public override void CompTick() {
            base.CompTick();

            Apparel apparel = parent as Apparel;
            if (apparel == null) return;

            Pawn wearer = apparel.Wearer;
            if (wearer == null || wearer.Dead || !wearer.Spawned) return;

            Hediff waterNeed = wearer.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("lotr_HediffWaterNeed"));
            if (waterNeed == null) {
                waterNeed = wearer.health.AddHediff(HediffDef.Named("lotr_HediffWaterNeed"));
                waterNeed.Severity = 0f;
            }

            // Проверяем наличие мысли "промокший"
            bool hasSoakingWetThought = false;
            if (wearer.needs?.mood?.thoughts?.memories != null) {
                hasSoakingWetThought = wearer.needs.mood.thoughts.memories
                    .Memories.Any(m => m.def == ThoughtDefOf.SoakingWet);
            }

            if (hasSoakingWetThought) {
                waterNeed.Severity = 0f;
                return;
            }

            if (wearer.IsHashIntervalTick((int)Props.severityRiseIntervalTicks)) {
                waterNeed.Severity += Props.severityRisePerInterval;
            }

            if (waterNeed.Severity >= 1f) {
                wearer.Kill(null, waterNeed);
            }
        }
    }
}
