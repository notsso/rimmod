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
        // Получаем доступ к настройкам из XML (если нужно)
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

            ProvokePawn(targetPawn, caster);

            if (targetPawn.RaceProps.ToolUser || targetPawn.RaceProps.IsMechanoid) {
                var hediff = caster.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Hunter8_Hediff")) as Beyonder_Hediff;

                if (hediff != null) {
                    float severityIncrement = 0.05f;

                    hediff.AddActingProgress(1, severityIncrement, caster);
                }
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
}
