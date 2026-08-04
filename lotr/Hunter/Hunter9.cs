using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Специфичная логика Охотника
    public class Hunter9_Hediff : Beyonder_Hediff {
        public override float SpiritualityFactor => 1.2f;
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
