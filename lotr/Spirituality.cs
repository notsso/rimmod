using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;

using RimWorld;

using UnityEngine;

namespace lotr {
    [StaticConstructorOnStartup]
    public static class ModInitializer {
        static ModInitializer() {
            var harmony = new Harmony("nar.lotr");
            harmony.PatchAll();
        }
    }

    public class Need_Spirituality : Need {
        // Настройка скорости регенерации духовности (в час)
        private const float RegenerationPerHour = 0.04f;

        public const float MaxInternalValue = 1f;

        public override float MaxLevel => GetFinalSpirituality();

        public Need_Spirituality(Pawn pawn) : base(pawn) {
            this.threshPercents = new System.Collections.Generic.List<float> { 0.2f, 0.4f, 0.8f };
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

        // Метод для расчета бонусов/штрафов к МАКСИМАЛЬНОМУ объему духовности
        private float GetFinalSpirituality() {
            float result = 1f;

            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs != null) {
                for (int i = 0; i < hediffs.Count; i++) {
                    if (hediffs[i] is Beyonder_Hediff beyonderHediff) {
                        result *= beyonderHediff.SpiritualityFactor;
                    }
                }
            }

            return result * 1.0f;
        }
    }

    public class SpiritualityCostExtension : DefModExtension {
        // Переменная, которую мы будем настраивать в XML для каждой способности отдельно
        public float cost = 0f;
    }

    public class Ability_SpendSpirituality : Ability {
        public Ability_SpendSpirituality() : base() { }

        public Ability_SpendSpirituality(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public float AbilityCost() {
            float finalCost = 10f;

            SpiritualityCostExtension extension = this.def.GetModExtension<SpiritualityCostExtension>();

            if (extension != null) {
                finalCost = extension.cost;
            }

            // различные баффы/дебаффы к цене, но пока я сомневаюсь

            return finalCost;
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest) {
            // Сначала вызываем базовую логику (чтобы сработали все прикомпонованные comps, например, запуск снаряда)
            bool result = base.Activate(target, dest);

            // Если способность успешно активировалась
            if (result) {
                Pawn caster = this.pawn;
                if (caster?.health != null) {
                    Need_Spirituality spirituality = this.pawn.needs?.TryGetNeed<Need_Spirituality>();

                    if (spirituality != null) {
                        float cost = AbilityCost();

                        spirituality.CurLevel -= cost * 0.01f;

                        string textPct = $"-{(cost).ToString("F0")} Духовности";
                        MoteMaker.ThrowText(caster.DrawPos, caster.Map, textPct, 3f);
                    }
                }
            }

            return result;
        }

        public override bool GizmoDisabled(out string reason) {
            if (base.GizmoDisabled(out reason)) {
                return true;
            }

            Need_Spirituality spirituality = this.pawn.needs?.TryGetNeed<Need_Spirituality>();

            if (spirituality == null) {
                reason = "Нет духовной энергии.";
                return true;
            }

            float cost = AbilityCost();

            if (spirituality.CurLevel < cost * 0.01f) {
                reason = $"Недостаточно духовности (Нужно {(cost).ToString("F0")}).";
                return true;
            }

            reason = null;
            return false;
        }
    }

    public class SanityLoss : HediffWithComps {
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
    }

    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Patch_Pawn_GetGizmos {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance) {
            // Возвращаем оригинальные гизмо, но если это наш колонист — добавляем шкалу в самый ТОР (начало)
            if (__instance.IsColonistPlayerControlled) {
                Need spiritualityNeed = __instance.needs?.AllNeeds.FirstOrDefault(n => n.def.defName == "lotr_SpiritualityNeed");
                if (spiritualityNeed != null) {
                    yield return new SpiritualityNeedGizmo(spiritualityNeed);
                }
            }

            foreach (var gizmo in __result) {
                yield return gizmo;
            }
        }
    }

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

    // при поглощении зелья у пешки высасывается часть духовности
    public class IngestionOutcomeDoer_DrainSpirituality : IngestionOutcomeDoer {
        public float drainPercent; // xml

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount) {
            if (pawn == null || pawn.needs == null) return;

            NeedDef spiritualityNeedDef = DefDatabase<NeedDef>.GetNamed("lotr_SpiritualityNeed", false);

            if (spiritualityNeedDef != null) {
                Need spiritualityNeed = pawn.needs.TryGetNeed(spiritualityNeedDef);

                if (spiritualityNeed != null) {
                    spiritualityNeed.CurLevelPercentage -= drainPercent * spiritualityNeed.MaxLevel;
                }
            }
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
}
