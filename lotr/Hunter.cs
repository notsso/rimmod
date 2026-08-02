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
        // private int ticksCounter = 0;

        public override void Tick() {
            base.Tick();

            // Специфичная логика Охотника: регенерация ран
            /*
            ticksCounter++;
            if (ticksCounter >= 180) {
                ticksCounter = 0;
                TryHealWounds();
            }*/
        }

        // disabled, for now
        private void TryHealWounds() {
            if (this.pawn == null || this.pawn.health == null) return;

            float healAmount = 0.1f;
            if (this.CurStageIndex == 1) healAmount = 0.2f;
            if (this.CurStageIndex == 2) healAmount = 0.3f;

            if (healAmount <= 0f) return;

            List<Hediff_Injury> injuries = this.pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(x => x.Severity > 0f)
                .ToList();

            if (injuries.Any()) {
                Hediff_Injury worstInjury = injuries.OrderByDescending(x => x.Severity).First();
                worstInjury.Severity -= healAmount;
            }
        }
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

    public class Hunter8_Hediff : Hunter9_Hediff { }

    public class CompAbilityEffect_Provoke : CompAbilityEffect {
        // Получаем доступ к настройкам из XML (если нужно)
        public new CompProperties_AbilityProvoke Props => (CompProperties_AbilityProvoke)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);

            // Проверяем, что цель — это живая пешка
            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn == null || targetPawn.Dead || targetPawn.Downed) {
                return;
            }

            // Игнорируем союзников (опционально, если хотите провоцировать только врагов)
            if (targetPawn.Faction == caster.Faction) {
                return;
            }

            // Механика провокации: заставляем цель атаковать кастера
            ProvokePawn(targetPawn, caster);
        }

        private void ProvokePawn(Pawn victim, Pawn aggressor) {
            // 1. Сбрасываем текущее действие жертвы
            victim.jobs.StopAll();

            // 2. Создаем новую задачу атаки в ближнем бою (или дальнем, если нужно)
            // JobDefOf.AttackMelee заставит пешку бежать к агрессору и бить его
            Job tauntJob = JobMaker.MakeJob(JobDefOf.AttackMelee, aggressor);

            // Устанавливаем высокий приоритет, чтобы задача не сбросилась сразу
            tauntJob.expiryInterval = 600; // Провокация длится 10 секунд (600 тиков)
            tauntJob.checkOverrideOnExpire = true;
            tauntJob.playerForced = true;

            // 3. Отдаем приказ пешке
            victim.jobs.StartJob(tauntJob, JobCondition.InterruptForced, null, false, true);

            // Визуальный эффект (текст над головой)
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
