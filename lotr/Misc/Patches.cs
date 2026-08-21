using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

using System.Reflection.Emit;

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
                int sequence = BeyonderUtility.GetBeyonderSequence(___pawn);
                bool isHunter9 = (hediff != null && sequence == 9);

                if (isHunter9) {
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
                    int sequence = BeyonderUtility.GetBeyonderSequence(billDoer);
                    bool isHunter7 = (hediff != null && sequence == 7);

                    if (isHunter7) {
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

                    BeyonderUtility.AdjustSanityLoss(___pawn, sanityDamage);
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
                    int sequence = BeyonderUtility.GetBeyonderSequence(__instance);
                    bool isHunter9 = (hediff != null && sequence == 9);

                    if (isHunter9) {
                        float sanityPenalty = 0.05f;
                        BeyonderUtility.AdjustSanityLoss(__instance, sanityPenalty, "Охотник стал жертвой!");
                    }
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
                    int sequence = BeyonderUtility.GetBeyonderSequence(pawn);
                    bool isHunter8 = (hediff != null && sequence == 8);

                    if (isHunter8) {
                        float sanityPenalty = 0.05f;
                        BeyonderUtility.AdjustSanityLoss(pawn, sanityPenalty, "Провокатор был оскорблен!");
                    }
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
                    int sequence = BeyonderUtility.GetBeyonderSequence(pawn);
                    bool isHunter7 = (hediff != null && sequence == 7);

                    if (isHunter7) {
                        float sanityPenalty = 0.05f;
                        BeyonderUtility.AdjustSanityLoss(pawn, sanityPenalty, "Пламя ранило пироманта!");
                    }
                }
            }
        }
    }

    // Harmony patch - отслеживает призываемое в руках оружие
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "EquipmentTrackerTick")]
    public static class Patch_SummonedWeapon_Controller {
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
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Summoned weapon disappeared", 2f);
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

    // Harmony patch - Отслеживаем действие 'Заговрщика' - враги убивают сами себя
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
                int sequence = BeyonderUtility.GetBeyonderSequence(conspiratorCaster);
                bool isHunter6 = (hediff != null && sequence == 6);

                if (isHunter6) {
                    float severityIncrement = 0.05f;
                    hediff.AddActingProgress(2, severityIncrement, conspiratorCaster);
                    MoteMaker.ThrowText(conspiratorCaster.DrawPos, conspiratorCaster.Map, "Заговор удался!", 4f);
                }

                BerserkPuppeteerRegistry.CleanUp(killer);
            }
        }
    }

    // Патчим метод, который собирает текущие цвета неба (для погоды, времени суток и т.д.)
    [HarmonyPatch(typeof(SkyManager), "CurrentSkyTarget")]
    public static class Patch_RedNightSky {
        [HarmonyPostfix]
        public static void Postfix(Map ___map, ref SkyTarget __result) {
            if (___map == null) return;

            // Получаем текущую ванильную освещенность неба (от 0.0 — глухая ночь, до 1.0 — ясный день)
            float curGlow = ___map.skyManager.CurSkyGlow;

            // Если освещенность падает ниже 35% (это сумерки и ночь)
            if (curGlow < 0.35f) {
                // Высчитываем силу ночи: чем темнее на улице, тем ближе значение к 1.0f
                float nightFactor = 1f - (curGlow / 0.35f);

                // Создаем каноничный мистический багровый свет (RGB: красный, зеленый, синий)
                // Можете изменить цифры (от 0f до 1f), чтобы сделать ночь светлее или темнее
                // Color bloodNightColor = new Color(0.7f, 0.25f, 0.1f, 1f);
                // Color bloodNightColor = new Color(0.4f, 0.15f, 0.12f, 1f);
                Color bloodNightColor = new Color(0.62f, 0.11f, 0.06f, 1f);

                Color lightShadowColor = new Color(0.38f, 0.08f, 0.08f, 1f);

                // Плавно смешиваем ванильный цвет ночи с нашим красным
                Color newSkyColor = Color.Lerp(__result.colors.sky, bloodNightColor, nightFactor);
                Color newShadowColor = Color.Lerp(__result.colors.shadow, lightShadowColor, nightFactor);

                // Перезаписываем цвета в финальном результате, который уходит на отрисовку видеокарте
                __result.colors.sky = newSkyColor;
                __result.colors.shadow = newShadowColor; // Тени от зданий и деревьев тоже станут красноватыми
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetFloatMenuOptions))]
    public static class Patch_Pawn_GetFloatMenuOptions {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> values, Pawn __instance, Pawn selPawn) {
            foreach (FloatMenuOption option in values)
                yield return option;

            // Первая встреча
            if (__instance.kindDef.HasModExtension<DefModExtension_FirstMeeting>()) {
                GameComponent_FirstMeeting comp = Current.Game.GetComponent<GameComponent_FirstMeeting>();
                if (comp == null || comp.IsPawnTalked(__instance))
                    yield break;

                yield return new FloatMenuOption(
                    "Поговорить с представителем",
                    delegate { Find.WindowStack.Add(new Dialog_FirstMeeting(__instance)); },
                    MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
            }

            // Мирный дипломат
            if (__instance.kindDef.HasModExtension<DefModExtension_PeaceOffer>()) {
                yield return new FloatMenuOption(
                    "Поговорить с дипломатом",
                    delegate { Find.WindowStack.Add(new Dialog_PeaceOffer(__instance)); },
                    MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
            }
        }
    }

    [HarmonyPatch(typeof(Command_Ability), "GizmoOnGUI")]
    public static class Patch_Command_Ability_GizmoOnGUI {
        // Достаем приватное поле ability
        private static readonly AccessTools.FieldRef<Command_Ability, Ability> AbilityField =
            AccessTools.FieldRefAccess<Command_Ability, Ability>("ability");

        public static void Postfix(Command_Ability __instance, Vector2 topLeft, float maxWidth) {
            // Проверяем наведение мыши
            Rect rect = new Rect(topLeft.x, topLeft.y, __instance.GetWidth(maxWidth), 75f);
            if (!Mouse.IsOver(rect)) return;

            Ability abilityInstance = AbilityField(__instance);
            if (abilityInstance == null || abilityInstance.comps == null) return;

            CompDrawRadiusOnHover comp = abilityInstance.comps.OfType<CompDrawRadiusOnHover>().FirstOrDefault();

            if (comp == null) return;

            // Считаем радиус
            float radius = comp.Props.radius;
            if (radius <= 0) {
                radius = abilityInstance.def.EffectRadius;
            }

            if (radius <= 0) radius = 5f;

            IntVec3 center = abilityInstance.pawn.Position;
            if (center.InBounds(Find.CurrentMap)) {
                GenDraw.DrawRadiusRing(center, radius);
            }
        }
    }

    // Harmony patch - выдаём способности потусторонним после генерации
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    public static class Patch_PawnGenerator_GeneratePawn {
        [HarmonyPostfix]
        public static void Postfix(ref Pawn __result) {
            if (__result != null && BeyonderUtility.IsBeyonder(__result)) {
                BeyonderUtility.UpdateAbilities(__result);
                Need spiritualityNeed = __result.needs?.AllNeeds.FirstOrDefault(n => n.def.defName == "lotr_SpiritualityNeed");
                if (spiritualityNeed != null) {
                    spiritualityNeed.CurLevel = spiritualityNeed.MaxLevel;
                }
            }
        }
    }

    // Harmony patch - при смерти пешки с нее выпадают потусторонние черты
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_BeyonderEssence {
        [HarmonyPrefix]
        public static void Prefix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit) {
            if (__instance == null || __instance.Destroyed) return;

            var beyonderHediffs = __instance.health.hediffSet.hediffs
                .Where(h => h is Beyonder_Hediff)
                .ToList();

            foreach (var hediff in beyonderHediffs) {
                BeyonderUtility.ExtractBeyonderEssence(__instance, hediff);
            }
        }
    }
}
