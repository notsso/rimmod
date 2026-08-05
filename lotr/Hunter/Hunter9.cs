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

    public class Projectile_Fireball : Projectile_ExplosiveCustom {
        // Переменная для отслеживания последней клетки, где мы обновили свет
        private IntVec3 lastLightPosition = IntVec3.Invalid;

        protected override void Tick() {
            base.Tick();

            // Проверяем, что снаряд летит и находится на карте
            if (this.Spawned && !this.Destroyed) {
                // Если снаряд перелетел в новую клетку
                if (this.Position != lastLightPosition) {
                    lastLightPosition = this.Position;

                    // Получаем компонент свечения, который прикреплен к нашему снаряду
                    CompGlower glower = this.GetComp<CompGlower>();

                    if (glower != null) {
                        // Ванильный и безопасный способ заставить карту перерисовать свет:
                        // Мы принудительно выключаем и включаем свет обратно. 
                        // Игра сама сотрет старое световое пятно и нарисует его в текущей позиции снаряда.
                        this.Map.glowGrid.DeRegisterGlower(glower);
                        this.Map.glowGrid.RegisterGlower(glower);
                    }
                }
            }
        }
    }

    // Harmony patch - отслеживает 'действие' охотника, progress1 - охота
    [HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
    public static class Patch_Pawn_JobTracker_EndCurrentJob {
        private static float factor { get; } = 0.1f;

        [HarmonyPrefix]
        public static void Prefix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob, bool canReturnToPool, ref float __state) {
            __state = 0.0f;

            if (__instance == null || __instance.curJob == null || __instance.curJob.def == null) {
                return;
            }

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
                var hediff = ___pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Hunter9_Hediff")) as Beyonder_Hediff;

                if (hediff != null) {
                    float victimBodySize = __state;
                    float severityIncrement = factor * victimBodySize;
                    severityIncrement = Mathf.Clamp(severityIncrement, 0.02f, 0.40f);

                    hediff.AddActingProgress(1, severityIncrement, ___pawn);
                }
            }
        }
    }

    // Патчим метод регулярного обновления пешки для отслеживания 'анти-действия' охотника
    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Patch_Beyonder_PanicFlee_SanityLoss {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance) {
            if (__instance != null && __instance.IsColonist && __instance.IsHashIntervalTick(250)) {

                bool isFleeing = (__instance.InMentalState && __instance.MentalStateDef == MentalStateDefOf.PanicFlee) ||
                                 (__instance.CurJob != null && (__instance.CurJob.def == JobDefOf.Flee || __instance.CurJob.def == JobDefOf.FleeAndCower));

                if (isFleeing) {
                    var hediff = __instance.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Hunter9_Hediff")) as Hunter9_Hediff;

                    if (hediff == null) return;

                    float sanityPenalty = 0.05f;

                    HediffDef sanityLossDef = HediffDef.Named("lotr_SanityLoss");
                    Hediff sanityLoss = __instance.health.hediffSet.GetFirstHediffOfDef(sanityLossDef);

                    if (sanityLoss != null) {
                        sanityLoss.Severity += sanityPenalty;
                    } else {
                        __instance.health.AddHediff(sanityLossDef);
                        Hediff newSanity = __instance.health.hediffSet.GetFirstHediffOfDef(sanityLossDef);
                        if (newSanity != null) {
                            newSanity.Severity = sanityPenalty;
                        }
                    }

                    MoteMaker.ThrowText(__instance.DrawPos, __instance.Map, "Охотник стал жертвой!", 3.5f);
                }
            }
        }
    }
}
