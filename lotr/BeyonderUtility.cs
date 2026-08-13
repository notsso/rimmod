using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;
using Verse.Noise;

namespace lotr {

    public enum Pathway {
        No_pathway = -1,
        Seer = 0, Apprentice, Savant, Assasin, Lawyer,
        Sailor, Marauder, Hunter, Warrior, Pryer,
        Monster, Spectator, Suppliant, Collector, Prisoner,
        Criminal, Reader, Sleepless, Apothecary, Bard,
        Arbiter, Planter
    }

    public static class BeyonderUtility {

        public static Pathway[][] PathwayGroups = new Pathway[][] {
            new[] { Pathway.Seer, Pathway.Marauder, Pathway.Apprentice },                                 // Повелитель тайн
            new[] { Pathway.Spectator, Pathway.Bard, Pathway.Sailor, Pathway.Reader, Pathway.Suppliant }, // Бог Всемогущий
            new[] { Pathway.Sleepless, Pathway.Collector, Pathway.Warrior},                               // Вечная Тьма
            new[] { Pathway.Assasin, Pathway.Hunter },                                                    // Бедствие разрушения
            new[] { Pathway.Pryer, Pathway.Savant },                                                      // Демон знаний
            new[] { Pathway.Planter, Pathway.Apothecary },                                                // Богиня создания
            new[] { Pathway.Monster },                                                                    // Ключ света
            new[] { Pathway.Lawyer, Pathway.Arbiter },                                                    // Анархия
            new[] { Pathway.Criminal, Pathway.Prisoner }                                                  // Отец дьяволов
        };

        public static Pathway[] AllPathways = new Pathway[] {
            Pathway.Seer, Pathway.Marauder, Pathway.Apprentice,
            Pathway.Spectator, Pathway.Bard, Pathway.Sailor, Pathway.Reader, Pathway.Suppliant,
            Pathway.Sleepless, Pathway.Collector, Pathway.Warrior,
            Pathway.Assasin, Pathway.Hunter,
            Pathway.Pryer, Pathway.Savant,
            Pathway.Planter, Pathway.Apothecary,
            Pathway.Monster,
            Pathway.Lawyer, Pathway.Arbiter,
            Pathway.Criminal, Pathway.Prisoner
        };

        // Метод для нанесения урона рассудку
        public static void AdjustSanityLoss(Pawn pawn, float amount, string reasonMote = null) {
            if (pawn == null || pawn.health == null) return;

            HediffDef sanityDef = LotrDefOf.lotr_SanityLoss;
            Hediff sanityHediff = pawn.health.hediffSet.GetFirstHediffOfDef(sanityDef);

            if (sanityHediff != null) {
                sanityHediff.Severity += amount;
            } else if (amount >= 0) {
                sanityHediff = HediffMaker.MakeHediff(sanityDef, pawn);
                sanityHediff.Severity = amount;
                pawn.health.AddHediff(sanityHediff);
            }

            // Показываем всплывающую надпись, если передана
            if (!reasonMote.NullOrEmpty() && pawn.Spawned) {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, reasonMote, 3.5f);
            }
        }

        public static Pathway GetPathwayFromString(string pathwayName) {

            return Pathway.TryParse<Pathway>(pathwayName, true, out Pathway result) ? result : Pathway.No_pathway;

        }


        // Проверяет, является ли пешка "Потусторонним" (любого уровня)
        public static bool IsBeyonder(Pawn pawn) {
            if (pawn?.health?.hediffSet?.hediffs == null) return false;
            foreach (var hediff in pawn.health.hediffSet.hediffs) {
                if (hediff is Beyonder_Hediff) return true;
            }
            return false;
        }

        public static int GetBeyonderSequence(Pawn pawn) {

            if (pawn?.health?.hediffSet?.hediffs == null) return 10;
            int sequence = 10;
            foreach (var hediff in pawn.health.hediffSet.hediffs) {
                // Log.Message($"{pawn.Name} aboab: {hediff.def.defName}");
                if (hediff.def is BeyonderHediffDef beyonderDef) {
                    sequence = Mathf.Min(beyonderDef.sequence, sequence);
                }
            }

            return sequence;

        }

        public static Pathway GetBeyonderPathway(Pawn pawn) {

            if (pawn?.health?.hediffSet?.hediffs == null) return Pathway.No_pathway;
            int pawn_sequence = GetBeyonderSequence(pawn);
            Pathway pawn_pathway = Pathway.No_pathway;
            foreach (var hediff in pawn.health.hediffSet.hediffs) {
                if (hediff.def is BeyonderHediffDef beyonderDef) {
                    if (beyonderDef.sequence == pawn_sequence) {
                        pawn_pathway = GetPathwayFromString(beyonderDef.pathway);
                    }
                }
            }

            Log.Message($"{pawn.Name} aboba: {pawn_sequence.ToString()} {((int)pawn_pathway).ToString()}");

            return pawn_pathway;

        }

        public static Pathway[] GetCorrespondingPathways(Pathway path) {

            if (path == Pathway.No_pathway) {
                return AllPathways;
            }

            var group = PathwayGroups.FirstOrDefault(g => g.Contains(path));

            return group;

        }

        public static void UpdateAbilities(Pawn pawn) {

            if (pawn.abilities == null) {
                pawn.abilities = new Pawn_AbilityTracker(pawn);
            }
            if (pawn?.health?.hediffSet?.hediffs == null) {
                return;
            }

            if (pawn.abilities.abilities != null) {
                List<Ability> abilitiesToRemove = pawn.abilities.abilities
                    .Where(a => a.def is BeyonderAbilityDef)
                    .ToList();

                foreach (var ability in abilitiesToRemove) {
                    pawn.abilities.RemoveAbility(ability.def);
                }
            }

            int sequence = BeyonderUtility.GetBeyonderSequence(pawn);
            List<AbilityDef> new_abilities = new List<AbilityDef> { };
            foreach (var hediff in pawn.health.hediffSet.hediffs) {
                if (hediff.def is BeyonderHediffDef beyonder_hediff) {
                    foreach (string ability_name in beyonder_hediff.newAbilities) {
                        new_abilities.Add(DefDatabase<AbilityDef>.GetNamed(ability_name + "_" + sequence.ToString() + "S"));
                    }
                }
            }

            foreach (AbilityDef ability_def in new_abilities) {
                pawn.abilities.GainAbility(ability_def);
            }

        }

    }
}
