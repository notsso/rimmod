using System.Collections.Generic;
using System.Linq;
using System;

using HarmonyLib;

using Verse;
using Verse.AI;
using Verse.AI.Group;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

namespace lotr {
    public class GameComponent_MysteryEvent : GameComponent {
        private int nextCheckTick;
        private const int CheckIntervalTicks = 7 * 60000;
        private const int MinAllianceDelayTicks = 3 * 60000;

        public GameComponent_MysteryEvent(Game game) { }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref nextCheckTick, "nextCheckTick", 0);
        }

        public override void GameComponentTick() {
            base.GameComponentTick();

            if (nextCheckTick == 0)
                nextCheckTick = Find.TickManager.TicksGame + CheckIntervalTicks;

            if (Find.TickManager.TicksGame < nextCheckTick)
                return;

            nextCheckTick += CheckIntervalTicks;

            if (Find.AnyPlayerHomeMap == null) return;

            GameComponent_FirstMeeting firstMeetingComp = Current.Game.GetComponent<GameComponent_FirstMeeting>();
            if (firstMeetingComp == null) return;

            // Собираем фракции, с которыми союз и прошло время
            List<Faction> availableFactions = new List<Faction>();
            foreach (string defName in BeyonderUtility.FactionDefNames) {
                Faction faction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == defName);
                if (faction == null || faction.HostileTo(Faction.OfPlayer)) continue;
                if (!firstMeetingComp.IsFactionAllied(faction)) continue;

                int alliedTick = firstMeetingComp.GetAlliedTick(faction);
                if (alliedTick < 0 || Find.TickManager.TicksGame - alliedTick < MinAllianceDelayTicks) continue;

                availableFactions.Add(faction);
            }

            if (availableFactions.Count == 0) return;

            Faction chosenFaction = availableFactions.RandomElement();
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.AnyPlayerHomeMap);
            parms.faction = chosenFaction;
            parms.forced = true;
            Find.Storyteller.TryFire(new FiringIncident(IncidentDef.Named("lotr_MysteryVisit"), null, parms));
        }
    }

    public class IncidentWorker_MysteryVisit : IncidentWorker_VisitorGroup {
        private const string TraderKindDefName = "lotr_BeyonderTrader";

        protected override bool TryResolveParmsGeneral(IncidentParms parms) {
            Map map = (Map)parms.target;
            if (!parms.spawnCenter.IsValid &&
                !RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Neutral, false, null))
                return false;

            if (parms.faction == null)
                return false;

            parms.points = 150f + Rand.Range(0, 100);
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            Map map = (Map)parms.target;
            Faction faction = parms.faction;
            if (faction == null) return false;

            if (!base.TryResolveParms(parms))
                return false;

            List<Pawn> list = base.SpawnPawns(parms);
            if (list.Count == 0)
                return false;

            foreach (Pawn p in list) {
                if (p.Faction != faction)
                    p.SetFaction(faction, null);
                GiveFood(p);
            }

            LordMaker.MakeNewLord(faction, CreateLordJob(parms, list), map, list);

            Pawn trader = list.Where(p => p.DevelopmentalStage.Adult()).RandomElementWithFallback();
            if (trader != null) {
                MakePawnTrader(trader, faction);
                GenerateTraderStock(trader, faction, map);
            }

            SendStandardLetter(this.def.letterLabel, this.def.letterText, LetterDefOf.NeutralEvent, parms, list[0], new NamedArgument[0]);
            return true;
        }

        private void GiveFood(Pawn pawn) {
            if (pawn?.inventory == null) return;
            ThingDef foodDef = ThingDef.Named("MealSurvivalPack");
            Thing food = ThingMaker.MakeThing(foodDef);
            food.stackCount = Rand.RangeInclusive(1, 3);
            pawn.inventory.innerContainer.TryAdd(food, true);
        }

        private void MakePawnTrader(Pawn pawn, Faction faction) {
            pawn.mindState.wantsToTradeWithColony = true;
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, true);

            pawn.trader.traderKind = DefDatabase<TraderKindDef>.GetNamed(TraderKindDefName);

            pawn.inventory.DestroyAll(DestroyMode.Vanish);
        }

        private void GenerateTraderStock(Pawn trader, Faction faction, Map map) {
            ThingSetMakerParams parms = default;
            parms.traderDef = trader.trader.traderKind;
            parms.tile = new PlanetTile?(map.Tile);
            parms.makingFaction = faction;

            foreach (Thing thing in ThingSetMakerDefOf.TraderStock.root.Generate(parms)) {
                if (!trader.inventory.innerContainer.TryAdd(thing, true))
                    thing.Destroy(DestroyMode.Vanish);
            }

            // Определяем путь и словарь ингредиентов в зависимости от фракции
            Pathway organizationPathway = Pathway.No_pathway;
            Dictionary<int, string[]> ingredientDefs = new Dictionary<int, string[]> { };

            if (faction.def.defName == "lotr_ChurchOfTheGodOfCombat") {
                organizationPathway = Pathway.Warrior;
                ingredientDefs = new Dictionary<int, string[]> {
                    { 9, new[] { "lotr_GiantWarriorCore" } },
                    { 8, new[] { "lotr_GiantSquireBone" } },
                    { 7, new[] { "lotr_BlueGiantSpine" } },
                    { 6, new[] { "lotr_DawnGiantCrystal" } },
                    { 5, new[] { "lotr_GrayGiantHeart" } },
                    { 4, new[] { "lotr_DivineGiantEye" } }
                };
            } else if (faction.def.defName == "lotr_IronAndBloodCrossOrder") {
                organizationPathway = Pathway.Hunter;
                ingredientDefs = new Dictionary<int, string[]> {
                    { 9, new[] { "lotr_MarshCrystal", "lotr_BloodRedChestnut"} },
                    { 8, new[] { "lotr_CuspidsParrotTongue", "lotr_CorpseLilyRootstock" } },
                    { 7, new[] { "lotr_FireSalamanderGland", "lotr_MagmaElfCore" } },
                    { 6, new[] { "lotr_BlackHuntingSpiderEyes", "lotr_SphinxBrain" } },
                    { 5, new[] { "lotr_DemonicWolfClaws", "lotr_ForestHunterTongue" } },
                    { 4, new[] { "lotr_MagmaGiantCore", "lotr_StoneofCatastrophe" } }
                };
            }

            int maxSequence = GetMaxSequenceForPathway(map, organizationPathway);

            for (int potionLevel = 9; potionLevel >= maxSequence - 1; potionLevel--) {
                if (!ingredientDefs.TryGetValue(potionLevel, out string[] defs))
                    continue;

                foreach (string defName in defs) {
                    if (Rand.Chance(0.7f)) continue;
                    ThingDef ingredientDef = ThingDef.Named(defName);
                    if (ingredientDef == null) continue;

                    Thing item = ThingMaker.MakeThing(ingredientDef);
                    item.stackCount = 1;

                    if (!trader.inventory.innerContainer.TryAdd(item, true))
                        item.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private int GetMaxSequenceForPathway(Map map, Pathway pathway) {
            int maxSequence = 10;

            foreach (Pawn pawn in map.mapPawns.FreeColonists) {
                if (pawn.Dead || pawn.Faction != Faction.OfPlayer)
                    continue;

                if (!BeyonderUtility.IsBeyonder(pawn))
                    continue;

                Pathway pawnPathway = BeyonderUtility.GetBeyonderPathway(pawn);
                if (pawnPathway != pathway)
                    continue;

                int sequence = BeyonderUtility.GetBeyonderSequence(pawn);
                if (sequence < maxSequence)
                    maxSequence = sequence;
            }

            return maxSequence;
        }
    }
}
