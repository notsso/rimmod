using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public class Hunter8_Hediff : Hunter9_Hediff {
        public override float SpiritualityFactor => 1.5f;
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
                if (caster.health?.hediffSet?.hediffs != null) {
                    foreach (var hediff in caster.health.hediffSet.hediffs) {
                        if (hediff is Beyonder_Hediff beyonderHediff) {
                            float severityIncrement = 0.05f;
                            float oldSeverity = beyonderHediff.Severity;
                            beyonderHediff.Severity += severityIncrement;

                            float diff = beyonderHediff.Severity - oldSeverity;
                            if (diff > 0.0f) {
                                string messageText = $"{caster.LabelShortCap} успешно спровоцировал врага! Зелье усвоено на {diff.ToStringPercent()}.";
                                Messages.Message(messageText, caster, MessageTypeDefOf.SilentInput, historical: false);
                            }
                            break;
                        }
                    }
                }
            }
        }

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
