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
        public static ThingDef lotr_MarshCrystal;
        public static ThingDef lotr_FireLightSpawner;
        public static ThingDef Hunter9_Potion;
        public static ThingDef Hunter8_Potion;
        public static ThingDef Hunter7_Potion;
        public static ThingDef Hunter6_Potion;
        public static ThingDef Hunter5_Potion;
        public static ThingDef Melee_BlazingSword;
        public static ThingDef Melee_BlazingSword_7S;
        public static ThingDef Melee_BlazingSword_6S;
        public static ThingDef Melee_BlazingSword_5S;

        public static AbilityDef Cast_BlazingSpear;
        public static AbilityDef Cast_BlazingSword;
        public static AbilityDef Cast_FireArmor;
        public static AbilityDef Cast_Fireball;
        public static AbilityDef Cast_FireRavens;
        public static AbilityDef Cast_Taunt;
        public static AbilityDef Cast_ExtinguishFire;
        public static AbilityDef Cast_FireTeleport;
        public static AbilityDef Cast_Incite;
        public static AbilityDef Cast_Confusion;
        public static AbilityDef Cast_ReaperState;
        public static AbilityDef Cast_Vulnerability;
        public static AbilityDef Cast_Execution;

        public static AbilityDef Cast_Fireball_9S;
        public static AbilityDef Cast_Fireball_8S;
        public static AbilityDef Cast_Fireball_7S;
        public static AbilityDef Cast_Fireball_6S;
        public static AbilityDef Cast_Fireball_5S;
        public static AbilityDef Cast_BlazingSpear_7S;
        public static AbilityDef Cast_BlazingSpear_6S;
        public static AbilityDef Cast_BlazingSword_7S;
        public static AbilityDef Cast_BlazingSword_6S;
        public static AbilityDef Cast_BlazingSword_5S;
        public static AbilityDef Cast_FireArmor_7S;
        public static AbilityDef Cast_FireArmor_6S;
        public static AbilityDef Cast_FireArmor_5S;
        public static AbilityDef Cast_FireRavens_7S;
        public static AbilityDef Cast_FireRavens_6S;
        public static AbilityDef Cast_FireRavens_5S;
        public static AbilityDef Cast_Taunt_8S;
        public static AbilityDef Cast_Taunt_7S;
        public static AbilityDef Cast_Taunt_6S;
        public static AbilityDef Cast_Taunt_5S;
        public static AbilityDef Cast_ExtinguishFire_7S;
        public static AbilityDef Cast_ExtinguishFire_6S;
        public static AbilityDef Cast_ExtinguishFire_5S;
        public static AbilityDef Cast_Incite_6S;
        public static AbilityDef Cast_Incite_5S;
        public static AbilityDef Cast_Confusion_6S;
        public static AbilityDef Cast_Confusion_5S;
        public static AbilityDef Cast_ReaperState_5S;
        public static AbilityDef Cast_Vulnerability_5S;
        public static AbilityDef Cast_Execution_5S;

        public static ThingDef Proj_Fireball_9S;
        public static ThingDef Proj_Fireball_8S;
        public static ThingDef Proj_Fireball_7S;
        public static ThingDef Proj_Fireball_6S;
        public static ThingDef Proj_Fireball_5S;
        public static ThingDef Proj_BlazingSpear_7S;
        public static ThingDef Proj_BlazingSpear_6S;
        public static ThingDef Proj_BlazingSpear_5S;

        public static HediffDef lotr_SanityLoss;
        public static lotr.BeyonderHediffDef Hunter9_Hediff;
        public static lotr.BeyonderHediffDef Hunter8_Hediff;
        public static lotr.BeyonderHediffDef Hunter7_Hediff;
        public static lotr.BeyonderHediffDef Hunter6_Hediff;
        public static lotr.BeyonderHediffDef Hunter5_Hediff;
        public static lotr.BeyonderHediffDef Hunter4_Hediff;
        public static HediffDef Hediff_FireArmor;
        public static HediffDef Hediff_FireArmor_7S;
        public static HediffDef Hediff_FireArmor_6S;
        public static HediffDef Hediff_FireArmor_5S;
        public static HediffDef Hediff_Confusion;
        public static HediffDef Hediff_Confusion_6S;
        public static HediffDef Hediff_Confusion_5S;
        public static HediffDef Hediff_ReaperState;
        public static HediffDef Hediff_ReaperState_5S;
        public static HediffDef Hediff_Vulnerable;
        public static HediffDef Hediff_Vulnerable_5S;

        public static JobDef lotr_CogitationJob;

        public static NeedDef lotr_SpiritualityNeed;

        public static PawnKindDef lotr_FireRaven;
        public static PawnKindDef lotr_MarshBoar;
        public static PawnKindDef lotr_Spirit;

        public static RecipeDef Hunter9_PotionRecipe;
        public static RecipeDef Hunter8_PotionRecipe;
        public static RecipeDef Hunter7_PotionRecipe;
        public static RecipeDef Hunter6_PotionRecipe;
        public static RecipeDef Hunter5_PotionRecipe;

        public static ThingCategoryDef BeyonderPotions;

        public static ThinkTreeDef lotr_MarshBoarTree;

        public static ThoughtDef lotr_SanityLossThought;

        public static FleckDef InstantFlame; // OBSOLETE
        public static GameConditionDef BloodMoon;
    }

    // для получения hediff какого то
    public class IngestionOutcomeDoer_GiveHediffRange : IngestionOutcomeDoer {
        public HediffDef hediffDef; // xml
        public FloatRange severityRange; // xml

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount) {
            if (pawn == null || hediffDef == null) return;

            float randomSeverity = severityRange.RandomInRange;

            Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);

            if (existingHediff != null) {
                existingHediff.Severity += randomSeverity;
            } else {
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = randomSeverity;
                pawn.health.AddHediff(hediff);
            }
        }
    }

    // класс, для зелий потусторонних, которые продвигают
    public class IngestionOutcomeDoer_SequenceAdvance : IngestionOutcomeDoer {
        // Поля будут настраиваться через XML
        public HediffDef hediffToGive; // На что меняем
        public float severity;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount) {
            if (pawn == null) return;

            Pathway pawn_pathway = BeyonderUtility.GetBeyonderPathway(pawn);
            int pawn_sequence = BeyonderUtility.GetBeyonderSequence(pawn);

            BeyonderHediffDef new_hediff = (BeyonderHediffDef)hediffToGive;
            Pathway new_hediff_pathway = BeyonderUtility.GetPathwayFromString(new_hediff.pathway);

            bool flag1 = false;
            foreach (Pathway path in BeyonderUtility.GetCorrespondingPathways(pawn_pathway)) {
                if (path == new_hediff_pathway) {
                    flag1 = true;
                }
            }

            if (flag1 && new_hediff.sequence >= pawn_sequence - 1) {

                // И добавляем новый
                Hediff newHediff = HediffMaker.MakeHediff(hediffToGive, pawn);
                newHediff.Severity = severity;
                pawn.health.AddHediff(newHediff);

                BeyonderUtility.UpdateAbilities(pawn);

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
        public int lifespan;
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

    // Хранилище связей: Жертва Безумия -> Кто её заколдовал
    // Используем WeakReference или регулярную очистку, чтобы избежать утечек памяти
    public static class BerserkPuppeteerRegistry {
        public static Dictionary<Pawn, Pawn> puppeteers = new Dictionary<Pawn, Pawn>();

        public static void Register(Pawn victimOfMentalState, Pawn caster) {
            if (victimOfMentalState == null || caster == null) return;
            puppeteers[victimOfMentalState] = caster;
        }

        public static Pawn GetCaster(Pawn victimOfMentalState) {
            if (victimOfMentalState != null && puppeteers.TryGetValue(victimOfMentalState, out Pawn caster)) {
                // Если кастер умер или исчез, связь невалидна
                if (caster.Dead || !caster.Spawned) {
                    puppeteers.Remove(victimOfMentalState);
                    return null;
                }
                return caster;
            }
            return null;
        }

        public static void CleanUp(Pawn pawn) {
            puppeteers.Remove(pawn);
        }
    }
}
