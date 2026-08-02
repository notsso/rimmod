using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

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

            if (sanityLoss != null && sanityLoss.Severity >= 0.95f && Rand.Chance(0.1f)) {
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

        // кнопка для когитации
        public override IEnumerable<Gizmo> GetGizmos() {
            // Сначала возвращаем базовые кнопки
            if (base.GetGizmos() != null) {
                foreach (Gizmo gizmo in base.GetGizmos()) {
                    yield return gizmo;
                }
            }

            // Проверяем, что пешка может выполнять команды игрока
            if (pawn != null && pawn.IsColonistPlayerControlled) {
                // Создаем и настраиваем кнопку действия
                Command_Action cogitationButton = new Command_Action {
                    defaultLabel = "Заняться когитацией",
                    defaultDesc = "Погрузиться в ментальный транс для стабилизации Сиквенций, очищения разума и восстановления духовных сил.",

                    icon = TexCommand.GatherSpotActive,

                    action = delegate {
                        // Безопасно создаем задачу когитации
                        JobDef jobDef = DefDatabase<JobDef>.GetNamed("lotr_CogitationJob", false);
                        if (jobDef != null) {
                            Job cogitationJob = JobMaker.MakeJob(jobDef);

                            // Заставляем пешку немедленно бросить текущие дела (Misc) и начать когитацию
                            pawn.jobs.TryTakeOrderedJob(cogitationJob, JobTag.Misc);
                        } else {
                            Log.Error("[LOTR Mod] Ошибка: Не найден JobDef с именем lotr_CogitationJob в XML!");
                        }
                    }
                };

                yield return cogitationButton;
            }
        }
    }

    // Специфичная логика Охотника
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

    // Harmony patch - отслеживает 'действие' охотника
    [HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
    public static class Patch_Pawn_JobTracker_EndCurrentJob {
        private static float factor { get; } = 0.1f;

        [HarmonyPrefix]
        public static void Prefix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob, bool canReturnToPool, ref float __state) {
            __state = 0.0f;

            if (__instance.curJob != null && __instance.curJob.def == JobDefOf.Hunt && condition == JobCondition.Succeeded) {
                __state = 1.0f;

                if (__instance.curJob.targetA.Thing is Pawn victim) {
                    __state = victim.RaceProps.baseBodySize; // в зависимости от размера добычи, усвоение меняется
                }
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob, bool canReturnToPool, ref float __state, Pawn ___pawn) {
            if (__state > 0.01f && ___pawn != null && ___pawn.IsColonist) {
                Hediff hediff = ___pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Hunter9_Hediff"));

                if (hediff != null) {
                    float victimBodySize = __state;
                    float severityIncrement = factor * victimBodySize;
                    severityIncrement = Mathf.Clamp(severityIncrement, 0.02f, 0.40f);

                    hediff.Severity += severityIncrement;

                    string messageText = $"После действия, {___pawn.LabelShortCap} усвоил свое зелье на {severityIncrement.ToStringPercent()}!";

                    Messages.Message(messageText, ___pawn, MessageTypeDefOf.SilentInput, historical: false);
                }
            }
        }
    }
}
