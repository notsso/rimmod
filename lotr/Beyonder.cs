using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public abstract class Beyonder_Hediff : HediffWithComps {
        // счетчик тиков, для пассивного восстановления безумия
        private int sanityTickCounter = 0;

        // как этот Hediff влияет на кол-во духовности
        public virtual float SpiritualityFactor => 1f;

        // флаг полного усвоения зелья
        private bool isFullyAbsorbed = false;

        // прогресс усвоения зелья
        public override float Severity {
            get => base.Severity;
            set {
                base.Severity = value;

                if (base.Severity >= 1f && !isFullyAbsorbed) {
                    isFullyAbsorbed = true;
                    OnPotionFullyAbsorbed();
                }
            }
        }

        // переменные для проверки усвоения зелья в разных аспектах
        public float progress1 = 0;
        public float progress2 = 0;
        public float progress3 = 0;

        // хранит максимальное значение усвоения, которое можно получить за один аспект
        public float maxProgressPerCategory = 0.30f;

        // Что делать, когда зелье усвоилось
        protected virtual void OnPotionFullyAbsorbed() {
            string messageText = $"{pawn.LabelShort} полностью усвоил зелье \"{def.LabelCap}\". Теперь он может продвинуться.";

            Messages.Message(messageText, pawn, MessageTypeDefOf.PositiveEvent);
        }

        // Нужно для сохранения кастомных данных
        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref isFullyAbsorbed, "isFullyAbsorbed", false);
            Scribe_Values.Look(ref progress1, "progress1", 0f);
            Scribe_Values.Look(ref progress2, "progress2", 0f);
            Scribe_Values.Look(ref progress3, "progress3", 0f);
        }

        // Общее для всех потусторонних отображение процентов усвоения зелья
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

        // Общая логика пассивной регенерации от безумия
        public override void Tick() {
            base.Tick();

            sanityTickCounter++;
            if (sanityTickCounter >= 60) {
                sanityTickCounter = 0;
                CheckSpirituality();
                CheckSanity();
            }
        }

        // Общий метод проверки рассудка
        protected virtual void CheckSanity() {
            if (this.pawn == null || this.pawn.health == null) return;

            Hediff sanityLoss = this.pawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.lotr_SanityLoss);

            if (sanityLoss == null) return;

            sanityLoss.Severity -= 0.001f;

            if (sanityLoss.Severity >= 0.90f && Rand.Chance(0.1f)) {
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

        // Общий метод проверки духовности
        protected virtual void CheckSpirituality() {
            Need_Spirituality spirituality = this.pawn.needs.TryGetNeed(LotrDefOf.lotr_SpiritualityNeed) as Need_Spirituality;
            if (spirituality == null) return;

            // пока что ничего не делает
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
                        // JobDef jobDef = DefDatabase<JobDef>.GetNamed("lotr_CogitationJob", false);
                        JobDef jobDef = LotrDefOf.lotr_CogitationJob;
                        Job cogitationJob = JobMaker.MakeJob(jobDef);

                        // Заставляем пешку немедленно бросить текущие дела (Misc) и начать когитацию
                        pawn.jobs.TryTakeOrderedJob(cogitationJob, JobTag.Misc);
                    }
                };

                yield return cogitationButton;
            }
        }

        // Универсальный метод добавления прогресса с проверкой лимита
        public void AddActingProgress(int category, float amount, Pawn pawn) {
            float oldProgress = 0f;
            float diff = 0f;

            if (category == 1) {
                if (progress1 >= maxProgressPerCategory) return;
                oldProgress = progress1;
                progress1 = Mathf.Clamp(progress1 + amount, 0f, maxProgressPerCategory);
                diff = progress1 - oldProgress;
            } else if (category == 2) {
                if (progress2 >= maxProgressPerCategory) return;
                oldProgress = progress2;
                progress2 = Mathf.Clamp(progress2 + amount, 0f, maxProgressPerCategory);
                diff = progress2 - oldProgress;
            } else if (category == 3) {
                if (progress3 >= maxProgressPerCategory) return;
                oldProgress = progress3;
                progress3 = Mathf.Clamp(progress3 + amount, 0f, maxProgressPerCategory);
                diff = progress3 - oldProgress;
            }

            this.Severity += diff;

            string messageText = $"После действия, {pawn.LabelShortCap} усвоил аспект зелья на {diff.ToStringPercent()}!"; ;
            if (diff < 0.01f) {
                messageText = $"{pawn.LabelShortCap} чуствует, что уже усвоил этот аспект зелья";
            }
            Messages.Message(messageText, pawn, MessageTypeDefOf.SilentInput, historical: false);

        }
    }
}