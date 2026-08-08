using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    // Harmony patch - отслеживает призываемое в руках оружие
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "EquipmentTrackerTick")]
    public static class Patch_SummonedFireWeapon_Controller {
        private static Dictionary<Pawn, Thing> activeWeaponLights { get; } = new Dictionary<Pawn, Thing>();

        [HarmonyPostfix]
        public static void Postfix(Pawn_EquipmentTracker __instance) {
            Pawn pawn = __instance.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead) return;

            if (__instance.Primary != null && __instance.Primary is SummonedWeapon summonedWeapon) {
                if (summonedWeapon.ticksLeft > 0) {
                    summonedWeapon.ticksLeft--;
                }

                if (summonedWeapon.ticksLeft == 0) {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Пламя угасло", 2f);
                    __instance.Primary.Destroy(DestroyMode.Vanish);

                    ClearLight(pawn);
                    return;
                }

                if (summonedWeapon is SummonedFireWeapon) {
                    if (!activeWeaponLights.TryGetValue(pawn, out Thing light) || light == null || !light.Spawned) {
                        ThingDef lightDef = LotrDefOf.lotr_FireLightSpawner;
                        if (lightDef != null) {
                            activeWeaponLights[pawn] = GenSpawn.Spawn(lightDef, pawn.Position, pawn.Map);
                        }
                    } else if (light.Position != pawn.Position) {
                        light.Destroy(DestroyMode.Vanish);
                        ThingDef lightDef = LotrDefOf.lotr_FireLightSpawner;
                        if (lightDef != null) {
                            activeWeaponLights[pawn] = GenSpawn.Spawn(lightDef, pawn.Position, pawn.Map);
                        }
                    }
                }
            } else {
                ClearLight(pawn);
            }
        }

        private static void ClearLight(Pawn pawn) {
            if (activeWeaponLights.TryGetValue(pawn, out Thing light) && light != null && light.Spawned) {
                light.Destroy(DestroyMode.Vanish);
            }
            activeWeaponLights.Remove(pawn);
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

    // Harmony patch - Запоминаем, кто наложил Берсерк
    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
    public static class Patch_RecordBerserkCaster {
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(MentalStateHandler), "pawn");

        [HarmonyPrefix]
        public static void Prefix(MentalStateHandler __instance, MentalStateDef stateDef, string reason) {
            if (stateDef == MentalStateDefOf.Berserk && reason == "Подстрекательство") {
                if (PawnField == null) return;

                Pawn targetPawn = (Pawn)PawnField.GetValue(__instance);

                if (targetPawn == null) return;

                if (CurrentCaster != null) {
                    BerserkPuppeteerRegistry.Register(targetPawn, CurrentCaster);
                    CurrentCaster = null;
                }
            }
        }

        public static Pawn CurrentCaster = null;
    }

    // Harmony patch - Отслеживаем убийство и проверяем условия заговора
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_ConspiratorMurderTracking {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, DamageInfo? dinfo) {
            if (__instance == null) return;

            if (dinfo == null || !(dinfo.Value.Instigator is Pawn killer)) return;

            if (killer.InMentalState && killer.MentalStateDef == MentalStateDefOf.Berserk) {
                Pawn conspiratorCaster = BerserkPuppeteerRegistry.GetCaster(killer);
                if (conspiratorCaster == null) return;

                // --- ПРОВЕРКА УСЛОВИЙ ИЗ ЗАПРОСА ---

                if (killer.Faction == conspiratorCaster.Faction || killer.Faction == null) return;

                // if (conspiratorCaster.Faction != Faction.OfPlayer) return;

                if (__instance.Faction != killer.Faction) return;

                // Все условия соблюдены: Враг-берсерк убил своего же союзника по приказу нашего Заговорщика!

                Hunter6_Hediff hediff = conspiratorCaster.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter6_Hediff) as Hunter6_Hediff;

                if (hediff != null) {
                    float severityIncrement = 0.05f;

                    hediff.AddActingProgress(2, severityIncrement, conspiratorCaster);

                    MoteMaker.ThrowText(conspiratorCaster.DrawPos, conspiratorCaster.Map, "Заговор удался!", 4f);
                }

                BerserkPuppeteerRegistry.CleanUp(killer);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
    public static class Patch_AttackAmplifier {
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_HealthTracker), "pawn");

        [HarmonyPrefix]
        public static void Prefix(Pawn_HealthTracker __instance, ref DamageInfo dinfo) {
            if (PawnField == null) return;

            Pawn victim = (Pawn)PawnField.GetValue(__instance);
            if (victim?.health?.hediffSet == null) return;

            // Определяем атакующего
            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null && dinfo.Instigator is Projectile proj && proj.Launcher is Pawn projLauncher) {
                attacker = projLauncher;
            }

            // Уязвимость работает только если есть атакующий
            if (attacker != null && attacker.health?.hediffSet != null) {
                bool hasVulnerable = false;
                foreach (var h in victim.health.hediffSet.hediffs) {
                    if (h is Hediff_Vulnerable) {
                        hasVulnerable = true;
                        break;
                    }
                }

                if (hasVulnerable) {
                    float newAmount = dinfo.Amount * 1.5f;
                    dinfo.SetAmount(newAmount);
                }
            }
        }
    }

    /*
    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Patch_ReaperAbsoluteDestruction {
        public static bool IsCustomProjectileDamage = false;
        private static bool isProcessingReaperDamage = false;
        private const float MinBodySizeForInstantKill = 2.0f; // порог размера цели

        // Структура для передачи данных между Prefix и Postfix
        public class StateData {
            public Pawn attacker;
            public bool wasAlive;
            public bool wasUnharmed;
            public bool bigEnough;
        }

        [HarmonyPrefix]
        public static void Prefix(Thing __instance, ref DamageInfo dinfo, out StateData __state) {
            __state = null;

            if (isProcessingReaperDamage)
                return;

            Pawn attacker = dinfo.Instigator as Pawn;
            bool isRangedAttack = false;

            // Определяем атакующего (игнорируем кастомные снаряды)
            if (attacker == null && dinfo.Instigator is Projectile proj) {
                if (proj is Projectile_Base)
                    return; // не мешаем своим снарядам

                attacker = proj.Launcher as Pawn;
                isRangedAttack = true;
            }

            // В ближнем бою без оружия не активируем (можно убрать, если нужно обратное)
            if (!isRangedAttack && dinfo.Weapon == null)
                return;

            if (attacker?.health?.hediffSet == null)
                return;

            // Фаза прогрева — пропускаем
            if (attacker.stances?.curStance is Stance_Warmup)
                return;

            // Ищем хедифф Жнеца
            Hediff_ReaperState reaperHediff = null;
            foreach (var h in attacker.health.hediffSet.hediffs) {
                if (h is Hediff_ReaperState hr) {
                    reaperHediff = hr;
                    break;
                }
            }

            if (reaperHediff == null || reaperHediff.isExpended)
                return;

            if (isRangedAttack && reaperHediff.isReserved)
                return; // заряд уже зарезервирован кастомным снарядом

            // --- Запоминаем состояние цели ДО модификаций ---
            Pawn victimPawn = __instance as Pawn;
            if (victimPawn != null) {
                __state = new StateData {
                    attacker = attacker,
                    wasAlive = !victimPawn.Dead,
                    wasUnharmed = victimPawn.health?.summaryHealth?.SummaryHealthPercent > 0.95f,
                    bigEnough = victimPawn.BodySize >= MinBodySizeForInstantKill
                };
            }

            // --- ЭТАП 1: УСИЛЕНИЕ ОСНОВНОГО УДАРА ---
            float baseAmount = dinfo.Amount;
            dinfo.SetIgnoreArmor(true);
            dinfo.SetAmount(baseAmount * 3f);

            // Блокируем рекурсию на время своих действий
            isProcessingReaperDamage = true;
            try {
                // Сбиваем энергощиты цели
                if (__instance is Pawn victim && victim.apparel != null) {
                    foreach (var apparel in victim.apparel.WornApparel) {
                        var shield = apparel?.GetComp<CompShield>();
                        if (shield?.parent != null) {
                            // Урон щиту, рекурсии не будет — флаг стоит
                            shield.parent.TakeDamage(new DamageInfo(DamageDefOf.Bomb, 9999f));
                        }
                    }
                }

                // Бонусный урон (половина от усиленного)
                float bonusAmount = dinfo.Amount * 0.5f;
                DamageDef bonusDef = isRangedAttack ? DamageDefOf.Blunt : DamageDefOf.ExecutionCut;
                DamageInfo bonusDinfo = new DamageInfo(
                    bonusDef,
                    bonusAmount,
                    dinfo.ArmorPenetrationInt,
                    dinfo.Angle,
                    attacker,
                    dinfo.HitPart,
                    dinfo.Weapon
                );
                bonusDinfo.SetIgnoreArmor(true);
                __instance.TakeDamage(bonusDinfo);
            } finally {
                isProcessingReaperDamage = false;
            }

            // Визуальный эффект
            Log.Message("reaper damage");
            if (__instance.Spawned && __instance.Map != null) {
                string text = isRangedAttack ? "ПОЖИНАНИЕ: РАЗРУШЕНИЕ (Blunt)" : "ПОЖИНАНИЕ: КАЗНЬ (Cut)";
                MoteMaker.ThrowText(__instance.Position.ToVector3Shifted(), __instance.Map, text, 4f);
            }

            reaperHediff.ExpendCharge();
        }

        [HarmonyPostfix]
        public static void Postfix(Thing __instance, StateData __state) {
            if (__state == null)
                return;

            Pawn victim = __instance as Pawn;
            // Цель должна была быть жива до удара и умереть после него
            if (victim == null || !__state.wasAlive)
                return;

            Pawn attacker = __state.attacker;
            if (attacker?.health?.hediffSet == null)
                return;

            var hunter5 = attacker.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter5_Hediff) as Hunter5_Hediff;
            if (hunter5 == null)
                return; // нет хедиффа — не начисляем ни прогресс, ни штраф

            if (__state.wasUnharmed && __state.bigEnough && victim.Dead) {
                hunter5.AddActingProgress(2, 0.01f, attacker);
            } else if (!victim.Dead) {
                BeyonderUtility.AddSanityLoss(attacker, 0.01f, "Пожинатель не казнил цель!");
            }
        }
    }*/
}
