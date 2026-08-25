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
        Seer = 0, Apprentice, Savant, Assassin, Lawyer,
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
            new[] { Pathway.Assassin, Pathway.Hunter },                                                    // Бедствие разрушения
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
            Pathway.Assassin, Pathway.Hunter,
            Pathway.Pryer, Pathway.Savant,
            Pathway.Planter, Pathway.Apothecary,
            Pathway.Monster,
            Pathway.Lawyer, Pathway.Arbiter,
            Pathway.Criminal, Pathway.Prisoner
        };

        public static readonly string[] FactionDefNames = new string[] {
            "lotr_IronAndBloodCrossOrder",
            "lotr_ChurchOfTheGodOfCombat"
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

        // Потеря контроля
        public static void ControlLoss(Pawn pawn) {
            if (pawn.Faction == Faction.OfPlayer) {
                Find.LetterStack.ReceiveLetter(
                    "Потеря контроля",
                    $"{pawn.LabelShort} полностью потерял контроль над потусторонними силами. Разум пешки окончательно разрушился, превратив его в ужасающее чудовище.",
                    LetterDefOf.Death,
                    pawn
                );
            }

            // TransformToMonster(pawn);
            pawn.ChangeKind(DefDatabase<PawnKindDef>.GetNamed("lotr_LostControlMonster"));
            pawn.SetFaction(null);
            PawnHelper.MakePermanentManhunter(pawn);
            pawn.Drawer.renderer.SetAllGraphicsDirty();
            pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.lotr_SanityLoss));
        }

        public static void ExtractBeyonderEssence(Pawn pawn, Hediff hediff) {
            if (hediff.def is BeyonderHediffDef beyonderDef) {
                string essenceName = $"lotr_{beyonderDef.defName.Split('_')[0]}_Essence";
                ThingDef essenceDef = ThingDef.Named(essenceName);

                if (essenceDef == null) {
                    Log.Message($"essenceNamed: {essenceName} not found. {beyonderDef.defName}");
                    return;
                }
                Thing essence = ThingMaker.MakeThing(essenceDef);
                essence.stackCount = 1;
                GenPlace.TryPlaceThing(essence, pawn.Position, pawn.Map, ThingPlaceMode.Near);

                // fancy way of deleting hediff without CheckForStateChange
                hediff.PreRemoved();
                pawn.health.hediffSet.hediffs.Remove(hediff);
                pawn.health.hediffSet.DirtyCache();
                hediff.PostRemoved();
                UpdateAbilities(pawn);
            }
        }

        public static bool CanAdvance(Pawn pawn, BeyonderHediffDef newHediffDef, out string reason) {
            // Log.Message("checking pawn");
            if (pawn == null) {
                reason = "pawn is null";
                return false;
            }

            // проверка на нужную последовательность
            // Log.Message("checking sequence");
            int pawn_sequence = GetBeyonderSequence(pawn);
            int hediff_sequence = newHediffDef.sequence;
            // Log.Message($"pawn: {pawn_sequence}, hediff: {hediff_sequence}");
            if (pawn_sequence - 1 > hediff_sequence) {
                reason = "sequence is too small";
                return false;
            }

            // проверка на соответствующий путь
            // Log.Message("checking pathway");
            Pathway pawn_pathway = GetBeyonderPathway(pawn);
            Pathway new_hediff_pathway = GetPathwayFromString(newHediffDef.pathway);
            if (!GetCorrespondingPathways(pawn_pathway).Contains<Pathway>(new_hediff_pathway)) {
                reason = "isnt corresponding pathway";
                return false;
            }

            // проверка на усвоение зелья (все черты)
            // Log.Message("checking potion");
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs) {
                if (hediff is Beyonder_Hediff beyonderHediff) {
                    if (beyonderHediff.Severity != 1f) {
                        reason = "didnt digested the potion";
                        return false;
                    }
                }
            }

            reason = "";
            return true;
        }
    }
}
