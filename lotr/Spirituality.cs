using HarmonyLib;

using Verse;

using RimWorld;

namespace lotr {

    [DefOf]
    public static class Lotr_DefOf {
        public static HediffDef Spirituality;

        static Lotr_DefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(Lotr_DefOf));
        }
    }


    public class first_class {

        [StaticConstructorOnStartup]
        public static class Main {
            static Main() {
                var harmony = new Harmony("nar.lotr");
                harmony.PatchAll();
            }
        }

        [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnGenerationRequest) })]
        public static class Patch_PawnGenerator_GeneratePawn {
            public static void Postfix(Pawn __result) {

                if (__result == null || __result.health == null) return;

                if (__result.RaceProps.IsMechanoid) return;

                HediffDef spiritualityHediff = Lotr_DefOf.Spirituality;

                if (spiritualityHediff != null) {

                    Hediff hediff = HediffMaker.MakeHediff(spiritualityHediff, __result, null);

                    hediff.Severity = Rand.Range(0.3f, 0.7f);

                    __result.health.AddHediff(hediff, null, null);

                }

            }

        }

    }

    public class Spirituality : HediffWithComps {
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

    public class Ability_SpendSpirituality : Ability {
        public Ability_SpendSpirituality() : base() { }

        public Ability_SpendSpirituality(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest) {
            // Сначала вызываем базовую логику (чтобы сработали все прикомпонованные comps, например, запуск снаряда)
            bool result = base.Activate(target, dest);

            // Если способность успешно активировалась
            if (result) {
                Pawn caster = this.pawn;
                if (caster?.health != null) {
                    // Находим хедифф духовности по его DefName
                    Hediff spirituality = caster.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("Spirituality"));
                    if (spirituality != null) {
                        // Базовая стоимость (15%)
                        float finalCost = 0.15f;

                        // Проверяем модификатор от Охотника
                        if (caster.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Hunter9_Hediff"))) {
                            finalCost *= 0.80f; // Скидка 20%
                        }

                        // Отнимаем духовность
                        spirituality.Severity -= finalCost;

                        // Текст над головой пешки
                        string textPct = $"-{(finalCost * 100f).ToString("F0")}% Духовности";
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

            Hediff spirituality = this.pawn?.health?.hediffSet?.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("Spirituality"));
            if (spirituality == null) {
                reason = "Нет духовной энергии.";
                return true;
            }

            float finalCost = 0.15f; // Та же стоимость для проверки
            if (this.pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Hunter9_Hediff"))) {
                finalCost *= 0.80f;
            }

            if (spirituality.Severity < finalCost) {
                reason = $"Недостаточно духовности (Нужно {(finalCost * 100f).ToString("F0")}%).";
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
}
