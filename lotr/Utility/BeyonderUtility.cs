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

            TransformToMonster(pawn);
        }

        private static void TransformToMonster(Pawn originalPawn) {
            if (originalPawn == null || originalPawn.Destroyed || originalPawn.Map == null) return;

            Map map = originalPawn.Map;
            IntVec3 position = originalPawn.Position;
            Rot4 rotation = originalPawn.Rotation;

            PawnKindDef monsterKind = PawnKindDef.Named("lotr_LostControlMonster");
            if (monsterKind == null) return;

            Pawn monster = PawnGenerator.GeneratePawn(monsterKind, null);
            if (monster == null) return;

            // Переносим все потусторонние Hediff'ы (копируем) монстру
            foreach (Hediff hediff in originalPawn.health.hediffSet.hediffs.ToList()) {
                if (hediff is Beyonder_Hediff) {
                    monster.health.AddHediff(HediffMaker.MakeHediff(hediff.def, monster, hediff.Part));
                }
            }

            // Выдаём способности
            UpdateAbilities(monster);

            // Переносим снаряжение и инвентарь
            TransferEquipmentAndInventory(originalPawn, monster);

            // Спавним монстра
            GenSpawn.Spawn(monster, position, map, rotation);

            // Уничтожаем исходную пешку (без Kill)
            originalPawn.Destroy(DestroyMode.Vanish);
        }

        private static void TransferEquipmentAndInventory(Pawn from, Pawn to) {
            // оружие
            if (from.equipment?.Primary != null && to.equipment != null) {
                ThingWithComps weapon = from.equipment.Primary;
                from.equipment.Remove(weapon);
                to.equipment.AddEquipment(weapon);
            }

            // снаряжение
            if (from.apparel != null && to.apparel != null) {
                var apparelList = from.apparel.WornApparel.ToList();
                foreach (Apparel apparel in apparelList) {
                    from.apparel.Remove(apparel);
                    to.apparel.Wear(apparel, false);
                }
            }

            // инвентарь
            if (from.inventory != null && to.inventory != null) {
                var items = from.inventory.innerContainer.ToList();
                foreach (Thing item in items) {
                    from.inventory.innerContainer.Remove(item);
                    if (!to.inventory.innerContainer.TryAdd(item, true))
                        item.Destroy(DestroyMode.Vanish);
                }
            }
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
            }
        }
    }
}
