using System.Collections.Generic;
using System.Linq;
using System;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // полезные def'ы
    [DefOf]
    public static class LotrDefOf {
        public static ThingDef Proj_BlazingSpear;
        public static ThingDef Proj_Fireball;
        public static ThingDef lotr_FireRavenRace;
        public static ThingDef lotr_MarshBoarRace;
        public static ThingDef lotr_MarshCrystal;
        public static ThingDef lotr_FireLightSpawner;
        public static HediffDef lotr_SanityLoss;

        public static BeyonderHediffDef Hunter9_Hediff;
        public static BeyonderHediffDef Hunter8_Hediff;
        public static BeyonderHediffDef Hunter7_Hediff;
        public static BeyonderHediffDef Hunter6_Hediff;
        public static BeyonderHediffDef Hunter5_Hediff;
        public static BeyonderHediffDef Hunter4_Hediff;

        public static JobDef lotr_CogitationJob; // TODO: когитацию надо скрывать, если пешка не умеет её делать

        public static NeedDef lotr_SpiritualityNeed;
        public static PawnKindDef lotr_Spirit;
        public static GameConditionDef BloodMoon;

        public static StatDef SpiritualityOffset;
    }

    [StaticConstructorOnStartup]
    public static class ModStartup {
        static ModStartup() {
            // событие - первая встреча с какой то тайной организацией
            LongEventHandler.QueueLongEvent(() => {
                if (Current.Game != null && !Current.Game.components.OfType<GameComponent_FirstMeeting>().Any()) {
                    Current.Game.components.Add(new GameComponent_FirstMeeting(Current.Game));
                }
            }, "lotr_AddFirstMeeting", false, null);

            // событие - торговцы из дружественной тайной организации
            LongEventHandler.QueueLongEvent(() => {
                if (Current.Game != null && !Current.Game.components.OfType<GameComponent_MysteryEvent>().Any()) {
                    Current.Game.components.Add(new GameComponent_MysteryEvent(Current.Game));
                }
            }, "lotr_AddMysteryEvent", false, null);

            // событие - возможность заново подружиться при вражде с тайной организацией
            LongEventHandler.QueueLongEvent(() => {
                if (Current.Game != null && !Current.Game.components.OfType<GameComponent_PeaceOffer>().Any())
                    Current.Game.components.Add(new GameComponent_PeaceOffer(Current.Game));
            }, "lotr_AddPeaceOffer", false, null);

            // обработчик событий появления на глобальной карте потусторонних существ
            LongEventHandler.QueueLongEvent(() => {
                if (Current.Game != null && !Current.Game.components.OfType<GameComponent_LotrPathEvents>().Any())
                    Current.Game.components.Add(new GameComponent_LotrPathEvents(Current.Game));
            }, "lotr_AddPathEvents", false, null);
        }
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
        public HediffDef hediffToGive; // hediff новой последовательности
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

            if (flag1 && new_hediff.sequence >= pawn_sequence - 1) { // TODO: проверить уровень усвоения

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

    // Лечит одну случайную болезнь и добавляет регенерацию
    public class IngestionOutcomeDoer_HealingPotion : IngestionOutcomeDoer {
        public HediffDef regenerationHediff;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount) {
            if (pawn == null || pawn.health == null)
                return;

            // Добавляем ускоренную регенерацию
            if (regenerationHediff != null)
                pawn.health.AddHediff(regenerationHediff);

            // Лечим одну случайную болезнь
            CureRandomDisease(pawn);
        }

        private void CureRandomDisease(Pawn pawn) {
            List<Hediff> diseases = new List<Hediff>();
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs) {
                if (IsDisease(hediff))
                    diseases.Add(hediff);
            }

            if (diseases.Count > 0) {
                Hediff toCure = diseases.RandomElement();
                pawn.health.RemoveHediff(toCure);
                Messages.Message("DiseaseCured".Translate(pawn.LabelShort, toCure.LabelCap),
                    pawn, MessageTypeDefOf.PositiveEvent, true);
            }
        }

        private bool IsDisease(Hediff hediff) {
            return hediff.def.HasComp(typeof(HediffCompProperties_Immunizable))
                || hediff.def.HasComp(typeof(HediffCompProperties_TendDuration));
        }
    }

    // Удаляет все негативные мысли и снижает SanityLoss
    public class IngestionOutcomeDoer_CalmingPotion : IngestionOutcomeDoer {
        public float sanityLossReduction = 0.5f;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount) {
            if (pawn == null)
                return;

            // Снимаем 0.5 потери контроля (SanityLoss)
            if (BeyonderUtility.IsBeyonder(pawn))
                BeyonderUtility.AdjustSanityLoss(pawn, -sanityLossReduction, "Calming");

            // Убираем все плохие мысли
            RemoveNegativeThoughts(pawn);
        }

        private void RemoveNegativeThoughts(Pawn pawn) {
            var memories = pawn.needs?.mood?.thoughts?.memories?.Memories;
            if (memories == null)
                return;

            // Проходим с конца, чтобы безопасно удалять
            for (int i = memories.Count - 1; i >= 0; i--) {
                if (memories[i].MoodOffset() < 0) {
                    pawn.needs.mood.thoughts.memories.RemoveMemory(memories[i]);
                }
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
