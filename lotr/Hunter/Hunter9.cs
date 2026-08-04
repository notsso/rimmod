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

        public Hunter9_Hediff() {
            maxProgressPerCategory = 0.8f;
        }
    }

    // Harmony patch - отслеживает 'действие' охотника, progress1 - охота
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

                if (hediff is Beyonder_Hediff beyonder_Hediff) {
                    // Messages.Message("pawn has a beyonder hediff", ___pawn, MessageTypeDefOf.SilentInput, historical: false);

                    float victimBodySize = __state;
                    float severityIncrement = factor * victimBodySize;
                    severityIncrement = Mathf.Clamp(severityIncrement, 0.02f, 0.40f);

                    float oldProgress = beyonder_Hediff.progress1;
                    beyonder_Hediff.progress1 += severityIncrement;
                    if (beyonder_Hediff.progress1 > beyonder_Hediff.maxProgressPerCategory) {
                        beyonder_Hediff.progress1 = beyonder_Hediff.maxProgressPerCategory;
                    }
                    float diff = beyonder_Hediff.progress1 - oldProgress;

                    hediff.Severity += diff;

                    string messageText = $"После действия, {___pawn.LabelShortCap} усвоил аспект зелья на {diff.ToStringPercent()}!"; ;
                    if (diff < 0.01f) {
                        messageText = $"{___pawn.LabelShortCap} чуствует, что уже усвоил этот аспект зелья";
                    }

                    Messages.Message(messageText, ___pawn, MessageTypeDefOf.SilentInput, historical: false);
                }
            }
        }
    }
}
