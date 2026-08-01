using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

namespace lotr {
    public abstract class Beyonder_Hediff : HediffWithComps {
        private int sanityTickCounter = 0;

        // Общее для всех отображение процентов
        public override string SeverityLabel {
            get {
                string baseLabel = base.SeverityLabel;
                string percent = (this.Severity).ToStringPercent();

                if (!baseLabel.NullOrEmpty()) {
                    return $"{baseLabel} ({percent})";
                }

                return percent;
            }
        }

        // Общая логика безумия тикает для ВСЕХ потусторонних
        public override void Tick() {
            base.Tick();

            sanityTickCounter++;
            if (sanityTickCounter >= 60) {
                sanityTickCounter = 0;
                CheckSpiritualityAndSanity();
            }
        }

        // Общий метод проверки рассудка.
        protected virtual void CheckSpiritualityAndSanity() {
            if (this.pawn == null || this.pawn.health == null) return;

            Need_Spirituality spirituality = this.pawn.needs.TryGetNeed<Need_Spirituality>();
            if (spirituality == null) return;


            Hediff sanityLoss = this.pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("lotr_SanityLoss"));

            if (spirituality.CurLevel <= 0.2f) {
                if (sanityLoss == null) {
                    HediffDef sanityDef = DefDatabase<HediffDef>.GetNamed("lotr_SanityLoss");
                    HealthUtility.AdjustSeverity(this.pawn, sanityDef, 0.01f);
                } else {
                    sanityLoss.Severity += 0.01f;
                }
            } else if (spirituality.CurLevel > 0.30f && sanityLoss != null) {
                sanityLoss.Severity -= 0.01f;
            }

            if (sanityLoss != null && sanityLoss.Severity >= 1.0f) {
                this.pawn.Kill(null, sanityLoss);

                if (this.pawn.Faction == Faction.OfPlayer) {
                    Find.LetterStack.ReceiveLetter(
                        "Потеря контроля",
                        $"{this.pawn.LabelShort} полностью потерял контроль над потусторонними силами. Разум пешки окончательно разрушился, вызвав мгновенную смерть тела.",
                        LetterDefOf.Death,
                        this.pawn
                    );
                }
            }
        }
    }

    public class Hunter9_Hediff : Beyonder_Hediff {
        private int ticksCounter = 0;

        public override void Tick() {
            base.Tick();

            // Специфичная логика Охотника: регенерация ран
            ticksCounter++;
            if (ticksCounter >= 180) {
                ticksCounter = 0;
                TryHealWounds();
            }
        }

        private void TryHealWounds() {
            if (this.pawn == null || this.pawn.health == null) return;

            float healAmount = 0.1f;
            if (this.CurStageIndex == 1) healAmount = 0.2f;
            if (this.CurStageIndex == 2) healAmount = 0.3f;

            if (healAmount <= 0f) return;

            List<Hediff_Injury> injuries = this.pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(x => x.Severity > 0f)
                .ToList();

            if (injuries.Any()) {
                Hediff_Injury worstInjury = injuries.OrderByDescending(x => x.Severity).First();
                worstInjury.Severity -= healAmount;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
    public static class Patch_Pawn_JobTracker_EndCurrentJob {
        [HarmonyPrefix]
        public static void Prefix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob, bool canReturnToPool, ref bool __state) {
            if (__instance.curJob != null && __instance.curJob.def == JobDefOf.Hunt && condition == JobCondition.Succeeded) {
                __state = true;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob, bool canReturnToPool, ref bool __state, Pawn ___pawn) {
            if (__state && ___pawn != null && ___pawn.IsColonist) {
                Hediff hediff = ___pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Hunter9_Hediff"));

                if (hediff != null) {
                    hediff.Severity += 0.5f;
                }
            }
        }
    }
}
