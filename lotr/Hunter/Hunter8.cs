using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public class Hunter8_Hediff : Hunter9_Hediff {
        public override float SpiritualityFactor => 1.5f;

        public Hunter8_Hediff() {
            maxProgressPerCategory = 0.8f;
        }
    }

    // абилка провокация
    public class CompAbilityEffect_Provoke : CompAbilityEffect {
        public new CompProperties_AbilityProvoke Props => (CompProperties_AbilityProvoke)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);

            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn == null || targetPawn.Dead || targetPawn.Downed) {
                return;
            }

            if (targetPawn.Faction == caster.Faction) {
                return;
            }

            float victimPsychicSensitivity = targetPawn.GetStatValue(StatDefOf.PsychicSensitivity, true);

            float finalSuccessChance = 0.75f * victimPsychicSensitivity;

            if (Rand.Value <= finalSuccessChance) {
                ProvokePawn(targetPawn, caster);

                if (targetPawn.RaceProps.ToolUser || targetPawn.RaceProps.IsMechanoid) {
                    var hediff = caster.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter8_Hediff) as Hunter8_Hediff;

                    if (hediff != null) {
                        float severityIncrement = 0.05f;

                        hediff.AddActingProgress(1, severityIncrement, caster);
                    }
                }
            } else {
                float sanityPenalty = 0.10f;

                SanityHelper.AddSanityLoss(caster, sanityPenalty, "Провокация провалена!");
            }
        }

        // провоцирует цель - дает ей задачу на ближний бой с провокатором на 10 секунд
        private void ProvokePawn(Pawn victim, Pawn aggressor) {
            if (victim == null || aggressor == null) return;

            victim.jobs.StopAll();

            victim.mindState.enemyTarget = aggressor;

            Job tauntJob = JobMaker.MakeJob(JobDefOf.AttackMelee, aggressor);

            tauntJob.expiryInterval = 600;
            tauntJob.checkOverrideOnExpire = true;
            tauntJob.playerForced = true;

            victim.jobs.StartJob(tauntJob, JobCondition.InterruptForced, null, false, true);

            MoteMaker.ThrowText(victim.DrawPos, victim.Map, "Provoked!", 3f);
        }
    }

    // Класс свойств для связи с XML
    public class CompProperties_AbilityProvoke : CompProperties_AbilityEffect {
        public CompProperties_AbilityProvoke() {
            compClass = typeof(CompAbilityEffect_Provoke);
        }
    }

    // Harmony patch - отслеживает 'анти-действие' провокатора: быть оскорбленным
    [HarmonyPatch(typeof(MemoryThoughtHandler), "TryGainMemory", new System.Type[] { typeof(Thought_Memory), typeof(Pawn) })]
    public static class Patch_Provoker_Insulted_SanityLoss {
        [HarmonyPostfix]
        public static void Postfix(MemoryThoughtHandler __instance, Thought_Memory newThought, Pawn otherPawn) {
            Pawn pawn = __instance.pawn;

            if (pawn != null && pawn.IsColonist && newThought != null) {
                if (newThought.def.defName == "Insulted" || newThought.def.defName == "InsultedMood") {
                    var hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(LotrDefOf.Hunter8_Hediff) as Hunter8_Hediff;

                    if (hediff == null) return;
                    float sanityPenalty = 0.05f;

                    SanityHelper.AddSanityLoss(pawn, sanityPenalty, "Провокатор был оскорблен!");
                }
            }
        }
    }
}
