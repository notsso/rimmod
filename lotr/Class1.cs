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

                if (!__result.RaceProps.Humanlike) return;

                HediffDef spiritualityHediff = Lotr_DefOf.Spirituality;

                if (spiritualityHediff != null) {

                    Hediff hediff = HediffMaker.MakeHediff(spiritualityHediff, __result, null);

                    hediff.Severity = Rand.Range(0.3f, 0.7f);

                    __result.health.AddHediff(hediff, null, null);

                }

            }

        }

    }

}
