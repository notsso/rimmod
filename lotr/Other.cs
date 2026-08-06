using System.Collections.Generic;
using System.Linq;
using System;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // все используемые def'ы
    [DefOf]
    public static class LotrDefOf {
        public static ThingDef Proj_BlazingSpear;
        public static ThingDef Proj_Fireball;
        public static ThingDef lotr_FireRavenRace;
        public static ThingDef lotr_MarshBoarRace;
        public static ThingDef lotr_MarshBoarCrystallizedSpleen;
        public static ThingDef lotr_FireLightSpawner;
        public static ThingDef Hunter9_Potion;
        public static ThingDef Hunter8_Potion;
        public static ThingDef Hunter7_Potion;
        public static ThingDef Melee_BlazingSword;
        public static ThingDef Melee_BlazingSword_7S;

        public static AbilityDef Cast_BlazingSpear;
        public static AbilityDef Cast_BlazingSword;
        public static AbilityDef Cast_FireArmor;
        public static AbilityDef Cast_Fireball;
        public static AbilityDef Cast_FireRavens;
        public static AbilityDef Cast_Taunt;

        public static AbilityDef Cast_Fireball_7S;
        public static AbilityDef Cast_Fireball_8S;
        public static AbilityDef Cast_Fireball_9S;
        public static AbilityDef Cast_BlazingSpear_7S;
        public static AbilityDef Cast_BlazingSword_7S;
        public static AbilityDef Cast_FireArmor_7S;
        public static AbilityDef Cast_FireRavens_7S;
        public static AbilityDef Cast_Taunt_7S;
        public static AbilityDef Cast_Taunt_8S;

        public static ThingDef Proj_Fireball_7S;
        public static ThingDef Proj_Fireball_8S;
        public static ThingDef Proj_Fireball_9S;
        public static ThingDef Proj_BlazingSpear_7S;

        public static HediffDef lotr_SanityLoss;
        public static HediffDef Hunter9_Hediff;
        public static HediffDef Hunter8_Hediff;
        public static HediffDef Hunter7_Hediff;
        public static HediffDef Hediff_FireArmor;
        public static HediffDef Hediff_FireArmor_7S;

        public static JobDef lotr_CogitationJob;

        public static NeedDef lotr_SpiritualityNeed;

        public static PawnKindDef lotr_FireRavenKind;
        public static PawnKindDef lotr_MarshBoar;

        public static RecipeDef Hunter9_PotionRecipe;
        public static RecipeDef Hunter8_PotionRecipe;
        public static RecipeDef Hunter7_PotionRecipe;

        public static ThingCategoryDef BeyonderPotions;

        public static ThinkTreeDef lotr_FireRaven_ThinkTree;
        public static ThinkTreeDef lotr_MarshBoarTree;

        public static ThoughtDef lotr_SanityLossThought;
    }

    // класс, для зелий потусторонних, которые продвигают
    public class IngestionOutcomeDoer_SequenceAdvance : IngestionOutcomeDoer {
        // Поля будут настраиваться через XML
        public HediffDef hediffToRemove; // Что ищем
        public HediffDef hediffToGive; // На что меняем
        public float severity;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount) {
            if (pawn == null) return;

            // Ищем старый Hediff
            Hediff oldHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(hediffToRemove);

            if (oldHediff != null && oldHediff.Severity >= 1.0f) {
                // Если нашли: удаляем его
                pawn.health.RemoveHediff(oldHediff);

                // И добавляем новый
                Hediff newHediff = HediffMaker.MakeHediff(hediffToGive, pawn);
                newHediff.Severity = severity;
                pawn.health.AddHediff(newHediff);

                // Сообщение игроку
                Messages.Message($"{pawn.LabelShort} успешно продвинулся.", pawn, MessageTypeDefOf.PositiveEvent);
            } else {
                pawn.Kill(null);

                // Сообщение о смерти
                Messages.Message($"{pawn.LabelShort} погиб, выпив зелье без подготовки!", TargetInfo.Invalid, MessageTypeDefOf.NegativeEvent);
            }
        }
    }

    public class SummonedWeaponExtension : DefModExtension {
        public ThingDef weaponDef;
    }

    public class SpiritualityCostExtension : DefModExtension {
        public float cost = 10f;
    }

    public class Projectile_FireExtension : DefModExtension {
        // nothing yet
    }

    public class Projectile_LightningExtension : DefModExtension {
        public bool? canStun;
        public float? stunChance;
    }

    public class Projectile_PoisonExtension : DefModExtension {
        public HediffDef poisonHediff;
        public float? poisonSeverity;
    }

    public class Projectile_SunlightExtension : DefModExtension {
        public bool? isEffectiveVsUndead;
        public float? bonusDamageVsUndead;
    }

    public class Projectile_MarionetteExtension : DefModExtension {
        public int? controlDuration;
        public float? controlChance;
    }
}
