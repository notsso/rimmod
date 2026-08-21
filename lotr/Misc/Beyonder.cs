using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;
using RimWorld.Planet;

namespace lotr {

    public abstract class Beyonder_Hediff : HediffWithComps {
        // счетчик тиков, для пассивного восстановления безумия
        private int sanityTickCounter = 0;

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
            if (!PawnUtility.ShouldSendNotificationAbout(pawn)) return;
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

            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

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

            // Проверяем, активна ли Кровавая Луна на карте
            bool bloodMoonActive = false;
            if (pawn.Map != null) {
                bloodMoonActive = pawn.Map.gameConditionManager.ConditionIsActive(LotrDefOf.BloodMoon);
            }

            if (bloodMoonActive) {
                BeyonderUtility.AdjustSanityLoss(this.pawn, 0.001f, null);
            } else {
                // Обычное восстановление рассудка
                BeyonderUtility.AdjustSanityLoss(this.pawn, -0.001f, null);
            }

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

            float currentPercentage = spirituality.CurLevelPercentage;

            HediffDef weaknessDef = DefDatabase<HediffDef>.GetNamed("SpiritualityExhaust", true);
            if (weaknessDef == null) return;

            Hediff weaknessHediff = pawn.health.hediffSet.GetFirstHediffOfDef(weaknessDef);

            if (currentPercentage > 0.4) {
                if (weaknessHediff != null) {
                    pawn.health.RemoveHediff(weaknessHediff);

                    if (PawnUtility.ShouldSendNotificationAbout(pawn)) {
                        Messages.Message(pawn.LabelShort + " has recovered from their weakness.", pawn, MessageTypeDefOf.PositiveEvent, false);
                    }
                }
            } else {
                if (weaknessHediff == null) {
                    pawn.health.AddHediff(weaknessDef);

                    if (PawnUtility.ShouldSendNotificationAbout(pawn)) {
                        Messages.Message(pawn.LabelShort + " is weakened due to low spirituality!", pawn, MessageTypeDefOf.NegativeEvent, true);
                    }
                } else {
                    float severity = (0.4f - currentPercentage) / 0.4f;

                    if (severity < 0.01f) severity = 0f;
                    if (severity > 1.0f) severity = 1f;
                    pawn.health.hediffSet.GetFirstHediffOfDef(weaknessDef).Severity = severity;
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

                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Cogitation") ?? TexCommand.GatherSpotActive,

                    action = delegate {
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
                oldProgress = progress1;
                progress1 = Mathf.Clamp(progress1 + amount, 0f, maxProgressPerCategory);
                diff = progress1 - oldProgress;
            } else if (category == 2) {
                oldProgress = progress2;
                progress2 = Mathf.Clamp(progress2 + amount, 0f, maxProgressPerCategory);
                diff = progress2 - oldProgress;
            } else if (category == 3) {
                oldProgress = progress3;
                progress3 = Mathf.Clamp(progress3 + amount, 0f, maxProgressPerCategory);
                diff = progress3 - oldProgress;
            }

            this.Severity += diff;

            if (!PawnUtility.ShouldSendNotificationAbout(pawn)) return;

            if (this.Severity < 1.0f) {
                string messageText;
                if (diff < 0.001f) {
                    messageText = $"{pawn.LabelShortCap} чуствует, что уже усвоил этот аспект зелья";
                } else {
                    messageText = $"После действия, {pawn.LabelShortCap} усвоил аспект зелья на {diff.ToStringPercent()}!";
                }
                Messages.Message(messageText, pawn, MessageTypeDefOf.SilentInput, historical: false);
            }
        }
    }
}