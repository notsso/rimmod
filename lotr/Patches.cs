using System.Collections.Generic;
using System.Linq;
using System;

using HarmonyLib;

using Verse;
using Verse.AI;

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

    // Harmony patch - отслеживает 'действие' hunter9 (охотника): охота
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
                var hediff = ___pawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter9_Hediff) as Hunter9_Hediff;

                if (hediff != null) {
                    float victimBodySize = __state;
                    float severityIncrement = factor * victimBodySize;
                    severityIncrement = Mathf.Clamp(severityIncrement, 0.02f, 0.40f);

                    hediff.AddActingProgress(1, severityIncrement, ___pawn);
                }
            }
        }
    }

    // Harmony patch - отслеживает 'действие' hunter7 (пиромант): контроль огня
    [HarmonyPatch(typeof(Bill_Production), "Notify_IterationCompleted")]
    public static class Patch_Pyromancer_Creation {
        [HarmonyPostfix]
        public static void Postfix(Bill_Production __instance, Pawn billDoer) {
            if (billDoer != null && billDoer.IsColonist) {
                if (__instance.billStack?.billGiver is Building_WorkTable table && table.def.defName == "Campfire") {
                    var hediff = billDoer.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter7_Hediff) as Hunter7_Hediff;

                    if (hediff != null) {
                        hediff.AddActingProgress(1, 0.01f, billDoer);
                    }
                }
            }
        }
    }

    // Harmony patch - отслеживает получения потусторонними нервных срывов
    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
    public static class Patch_Beyonder_SanityBreak {
        [HarmonyPostfix]
        public static void Postfix(MentalStateHandler __instance, MentalStateDef stateDef, bool __result, Pawn ___pawn) {
            if (__result && ___pawn != null && ___pawn.IsColonist) {
                bool isBeyonder = BeyonderUtility.IsBeyonder(___pawn);

                if (isBeyonder) {
                    float sanityDamage = 0.05f;
                    if (stateDef.category == MentalStateCategory.Aggro || stateDef.category == MentalStateCategory.Malicious) {
                        sanityDamage = 0.25f;
                    } else if (stateDef == MentalStateDefOf.Berserk) {
                        sanityDamage = 0.40f;
                    }

                    BeyonderUtility.AddSanityLoss(___pawn, sanityDamage);
                }
            }
        }
    }

    // Harmony patch - отслеживает 'анти-действие' hunter9 (охотника): быть добычей
    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Patch_Beyonder_PanicFlee_SanityLoss {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance) {
            if (__instance != null && __instance.IsColonist && __instance.IsHashIntervalTick(250)) {

                bool isFleeing = (__instance.InMentalState && __instance.MentalStateDef == MentalStateDefOf.PanicFlee) ||
                                 (__instance.CurJob != null && (__instance.CurJob.def == JobDefOf.Flee || __instance.CurJob.def == JobDefOf.FleeAndCower));

                if (isFleeing) {
                    var hediff = __instance.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter9_Hediff) as Hunter9_Hediff;

                    if (hediff == null) return;

                    float sanityPenalty = 0.05f;

                    BeyonderUtility.AddSanityLoss(__instance, sanityPenalty, "Охотник стал жертвой!");
                }
            }
        }
    }

    // Harmony patch - отслеживает 'анти-действие' hunter8 (провокатора): быть оскорбленным
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

                    BeyonderUtility.AddSanityLoss(pawn, sanityPenalty, "Провокатор был оскорблен!");
                }
            }
        }
    }

    // Harmony patch - отслеживает 'анти-действие' hunter7 (пироманта): получение урона от огня
    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Patch_Pyromancer_FireDamage_SanityLoss {
        [HarmonyPostfix]
        public static void Postfix(Thing __instance, DamageInfo dinfo) {
            if (__instance is Pawn pawn && pawn.IsColonist && !pawn.Destroyed) {
                if (dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.Burn) {
                    var hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(LotrDefOf.Hunter7_Hediff) as Hunter7_Hediff;

                    if (hediff == null) return;

                    float sanityPenalty = 0.05f;

                    BeyonderUtility.AddSanityLoss(pawn, sanityPenalty, "Пламя ранило пироманта!");
                }
            }
        }
    }

    // Harmony patch - отслеживает lifespan призываемого оружия
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "EquipmentTrackerTick")]
    public static class Patch_SummonedWeapon_LifespanInHands {
        [HarmonyPostfix]
        public static void Postfix(Pawn_EquipmentTracker __instance) {
            if (__instance.Primary != null && __instance.Primary is SummonedWeapon summonedWeapon) {
                CompLifespan lifespanComp = summonedWeapon.GetComp<CompLifespan>();

                if (lifespanComp != null) {
                    lifespanComp.age += 1;

                    if (lifespanComp.age >= lifespanComp.Props.lifespanTicks) {
                        Pawn pawn = __instance.pawn;

                        summonedWeapon.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }
    }

    // Harmony patch - свет + lifespan у огненного меча
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "EquipmentTrackerTick")]
    public static class Patch_SummonedFireWeapon_GlowerController {
        // Словарь для отслеживания лампочек у разных пешек (чтобы не было конфликтов, если мечи призовут сразу 3 мага)
        private static Dictionary<Pawn, Thing> activeWeaponLights { get; } = new Dictionary<Pawn, Thing>();

        [HarmonyPostfix]
        public static void Postfix(Pawn_EquipmentTracker __instance) {
            Pawn pawn = __instance.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null) return;

            // 1. ПРОВЕРКА: Держит ли пешка наш Огненный меч прямо сейчас?
            if (__instance.Primary != null && __instance.Primary is SummonedFireWeapon) {
                // Принудительно крутим ванильный таймер Lifespan меча, пока он в руках
                CompLifespan lifespan = __instance.Primary.GetComp<CompLifespan>();
                if (lifespan != null) {
                    lifespan.age += 1;
                    if (lifespan.age >= lifespan.Props.lifespanTicks) {
                        // Время вышло — уничтожаем меч
                        __instance.Primary.Destroy(DestroyMode.Vanish);

                        // Удаляем свет
                        if (activeWeaponLights.TryGetValue(pawn, out Thing oldLight) && oldLight.Spawned) {
                            oldLight.Destroy(DestroyMode.Vanish);
                        }
                        activeWeaponLights.Remove(pawn);
                        return;
                    }
                }

                // 2. УПРАВЛЕНИЕ СВЕТОМ МЕЧА
                if (!activeWeaponLights.TryGetValue(pawn, out Thing light) || light == null || !light.Spawned) {
                    // Спавним свет, если его еще нет
                    ThingDef lightDef = LotrDefOf.lotr_FireLightSpawner;
                    if (lightDef != null) {
                        activeWeaponLights[pawn] = GenSpawn.Spawn(lightDef, pawn.Position, pawn.Map);
                    }
                } else if (light.Position != pawn.Position) {
                    // Если пешка сделала шаг — пересоздаем свет в новой точке для обновления графики Unity
                    light.Destroy(DestroyMode.Vanish);
                    ThingDef lightDef = LotrDefOf.lotr_FireLightSpawner;
                    activeWeaponLights[pawn] = GenSpawn.Spawn(lightDef, pawn.Position, pawn.Map);
                }
            } else {
                // Если меча в руках больше нет (бросил, убрал или он испарился) — гасим свет
                if (activeWeaponLights.TryGetValue(pawn, out Thing light) && light != null && light.Spawned) {
                    light.Destroy(DestroyMode.Vanish);
                }
                activeWeaponLights.Remove(pawn);
            }
        }
    }

    // Harmony patch - добавляет шкалу духовности каждому колонисту в строку гаджетов
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
}