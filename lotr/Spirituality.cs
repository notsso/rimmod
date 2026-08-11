using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // главный класс, который определяет нашу кастомную 'потребность'
    public class Need_Spirituality : Need {
        private const float RegenerationPerHour = 0.04f;
        public const float MaxInternalValue = 1f;
        public override float MaxLevel => GetFinalSpirituality();
        public Need_Spirituality(Pawn pawn) : base(pawn) {
            this.threshPercents = new System.Collections.Generic.List<float> { 0.01f, 0.1f, 0.4f };
        }

        public override void SetInitialLevel() {
            this.CurLevel = 0.8f;
        }

        // Этот метод игра вызывает автоматически каждые 150 тиков для всех потребностей
        public override void NeedInterval() {
            if (IsFrozen) return;

            // Пассивная регенерация духовности со временем до максимума
            if (this.CurLevel < this.MaxLevel) {
                // Потребности обновляются интервалами по 150 тиков (это 1/16 игрового часа)
                this.CurLevel += (RegenerationPerHour / 16f) * (this.MaxLevel);
            }
        }

        private float GetFinalSpirituality() {
            float result = MaxInternalValue;

            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs != null) {
                for (int i = 0; i < hediffs.Count; i++) {
                    if (hediffs[i] is Beyonder_Hediff beyonderHediff) {
                        result *= beyonderHediff.SpiritualityFactor;
                    }
                }
            }

            return result;
        }
    }

    // кастомный класс под потерю контроля - добавляет проценты к описанию
    public class Hediff_SanityLoss : HediffWithComps {
        private const float SeverityChangePerTick = 0.00006f;

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

        public override void Tick() {
            base.Tick();

            if (pawn == null || !pawn.Spawned) return;

            // Проверяем, активно ли событие Кровавой Луны на карте
            bool bloodMoonActive = false;
            if (pawn.Map != null) {
                bloodMoonActive = pawn.Map.gameConditionManager
                    .ConditionIsActive(LotrDefOf.BloodMoon); // наш кастомный деф
            }

            if (bloodMoonActive) {
                // Во время луны безумие растёт
                Severity += SeverityChangePerTick;
            } else {
                // В обычное время медленно спадает (восстановление рассудка)
                Severity -= SeverityChangePerTick;
            }

            // Ограничиваем 0..maxSeverity (автоматически clamped)
        }
    }

    // определяет гаджет показывающий духовность
    public class SpiritualityNeedGizmo : Gizmo {
        private readonly Need need;

        // Фиксированная ширина шкалы духовности на панели управления пешкой
        public override float GetWidth(float maxWidth) => 140f;

        // Сортировка: сдвигаем гизмо в крайнее левое положение панели (левее Draft)
        public override float Order => -10005f;

        public SpiritualityNeedGizmo(Need need) {
            this.need = need;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, Verse.GizmoRenderParms parms) {
            // Формируем базовые границы элемента (высота стандартного гизмо — 75f)
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);

            GUI.DrawTexture(rect, TexUI.GrayBg);

            // Если игрок навел мышь на этот гизмо, рисуем стандартную рамку подсветки
            if (Mouse.IsOver(rect)) {
                Widgets.DrawHighlight(rect);
            }

            // Создаем внутренний контейнер с отступами
            Rect innerRect = rect.ContractedBy(6f);

            // 1. Отрисовка названия потребности мелким шрифтом
            Text.Font = GameFont.Tiny;
            Rect labelRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 18f);
            Widgets.Label(labelRect, need.LabelCap);

            // 2. Отрисовка полосы прогресса (Progress Bar)
            Rect barRect = new Rect(innerRect.x, innerRect.y + 22f, innerRect.width, 24f);

            // Генерируем сплошные текстуры для наполнения шкалы духовности
            Texture2D barTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.3f, 0.5f, 0.7f, 0.6f)); // Цвет шкалы
            Texture2D bgTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f, 0.5f));  // Цвет подложки

            // Заполняем прогресс-бар текущим процентным соотношением
            Widgets.FillableBar(barRect, need.CurLevelPercentage, barTex, bgTex, doBorder: true);

            // 3. Выравнивание и отрисовка текста поверх шкалы ("Текущее / Максимальное") в масштабе 100
            Text.Anchor = (UnityEngine.TextAnchor)TextAnchor.MiddleCenter;

            // Умножаем на 100f, чтобы перевести внутренний диапазон (0..1) в игровой (0..100)
            int currentDisplayVal = Mathf.RoundToInt(need.CurLevel * 100f);
            int maxDisplayVal = Mathf.RoundToInt(need.MaxLevel * 100f);

            string valueText = $"{currentDisplayVal} / {maxDisplayVal}";
            Widgets.Label(barRect, valueText);
            Text.Anchor = (UnityEngine.TextAnchor)TextAnchor.UpperLeft;

            // Добавление всплывающей подсказки (Tooltip) при наведении курсора
            TooltipHandler.TipRegion(rect, () => $"{need.def.description}\n\nТекущее значение: {need.CurLevelPercentage:P0}", need.def.GetHashCode());

            // Обработка клика по шкале — открывает у пешки вкладку "Потребности" (Needs)
            if (Widgets.ButtonInvisible(rect)) {
                MainTabWindow_Inspect mainTabWindow = (MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow;
                Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Inspect);
                InspectPaneUtility.OpenTab(typeof(ITab_Pawn_Needs));

                return new GizmoResult(GizmoState.Interacted);
            }

            return new GizmoResult(GizmoState.Clear);
        }
    }

    // при поглощении зелья дает пешку Hediff с каким то Severity (прописано в xml)
    public class IngestionOutcomeDoer_GiveHediffRange : IngestionOutcomeDoer {
        public HediffDef hediffDef; // xml

        public FloatRange severityRange; // xml

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount) {
            if (pawn == null || hediffDef == null) return;

            float randomSeverity = severityRange.RandomInRange;

            Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);

            if (existingHediff != null) {
                existingHediff.Severity += randomSeverity;
            } else {
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = randomSeverity;
                pawn.health.AddHediff(hediff);
            }
        }
    }

    public class SpiritualityUtility {
        public static float ConsumeSpirituality(Pawn pawn, float amount, bool message = true) {
            float result = -1;
            try {
                if (pawn?.health != null) {
                    Need_Spirituality spirituality = pawn.needs.TryGetNeed(LotrDefOf.lotr_SpiritualityNeed) as Need_Spirituality;

                    if (spirituality != null) {
                        float oldLevel = spirituality.CurLevel;
                        spirituality.CurLevel -= amount * 0.01f;
                        result = (oldLevel - spirituality.CurLevel) * 100;

                        if (message) {
                            string textPct = $"-{(amount).ToString("F0")} Духовности";
                            if (pawn.Spawned && pawn.Map != null) {
                                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, textPct, 3f);
                            }
                        }
                    }
                }
            } catch (Exception) {
                Log.Message("[SpiritualityUtility] unknown error");
            }
            return result;
        }
    }
}
