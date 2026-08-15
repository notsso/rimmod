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
        private int ticksUntilCheck = 0;
        private const int CheckIntervalTicks = 60000;
        private const float ChancePerCheck = 0.2f;

        public GameComponent_MysteryEvent(Game game) {
            ResetTimer();
        }

        private void ResetTimer() {
            ticksUntilCheck = CheckIntervalTicks;
        }

        public override void GameComponentTick() {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame < ticksUntilCheck)
                return;

            ResetTimer();
            TryTriggerEvent();
        }

        private void TryTriggerEvent() {
            if (Find.AnyPlayerHomeMap == null) return;
            if (!Rand.Chance(ChancePerCheck)) return;

            // Проверяем, есть ли наша фракция в мире
            Faction mysteryFaction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "lotr_IronAndBloodCrossOrder");
            if (mysteryFaction == null) return;

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.AnyPlayerHomeMap);
            parms.faction = mysteryFaction;
            parms.forced = true; // чтобы сработал даже с низким шансом
            Find.Storyteller.TryFire(new FiringIncident(IncidentDef.Named("lotr_MysteryVisit"), null, parms));
        }
    }

    public class IncidentWorker_MysteryVisit : IncidentWorker_VisitorGroup {
        private const string FactionDefName = "lotr_IronAndBloodCrossOrder";
        private const string TraderKindDefName = "lotr_MysteryTrader";
        private const float RareIngredientChance = 0.3f;

        protected override bool TryResolveParmsGeneral(IncidentParms parms) {
            Map map = (Map)parms.target;
            if (!parms.spawnCenter.IsValid &&
                !RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Neutral, false, null)) {
                return false;
            }

            Faction mysteryFaction = Find.FactionManager.AllFactions
                .FirstOrDefault(f => f.def.defName == FactionDefName);

            if (mysteryFaction == null)
                return false;

            parms.faction = mysteryFaction;
            // Гарантируем размер группы 3-5 человек
            parms.points = 150f + Rand.Range(0, 100); // 150-250 points
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            Map map = (Map)parms.target;
            if (!base.TryResolveParms(parms))
                return false;

            List<Pawn> list = base.SpawnPawns(parms);
            if (list.Count == 0)
                return false;

            // Добавляем еду каждому визитёру
            foreach (Pawn p in list) {
                GiveFood(p);
            }

            LordMaker.MakeNewLord(parms.faction, CreateLordJob(parms, list), map, list);

            // Выбираем торговца
            Pawn trader = list.Where(p => p.DevelopmentalStage.Adult()).RandomElementWithFallback();
            if (trader != null) {
                MakePawnTrader(trader, parms.faction);
                GenerateTraderStock(trader, parms.faction, map);
            }

            Pawn leader = list.FirstOrDefault(p => p.Faction.leader == p);
            SendLetter(parms, list, leader, true);

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
            pawn.trader.traderKind = DefDatabase<TraderKindDef>.GetNamed(TraderKindDefName, true);
            pawn.inventory.DestroyAll(DestroyMode.Vanish);
        }

        private void GenerateTraderStock(Pawn trader, Faction faction, Map map) {
            // Генерируем запасы на основе TraderKindDef
            ThingSetMakerParams parms = default(ThingSetMakerParams);
            parms.traderDef = trader.trader.traderKind;
            parms.tile = new PlanetTile?(map.Tile);
            parms.makingFaction = faction;
            foreach (Thing thing in ThingSetMakerDefOf.TraderStock.root.Generate(parms)) {
                if (!trader.inventory.innerContainer.TryAdd(thing, true)) {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            // Добавляем редкий ингредиент с шансом
            if (Rand.Chance(RareIngredientChance)) {
                ThingDef rareIngredient = Rand.Value < 0.5f
                    ? ThingDef.Named("lotr_MarshCrystal")
                    : ThingDef.Named("lotr_BloodRedChestnut");
                Thing item = ThingMaker.MakeThing(rareIngredient);
                item.stackCount = 1;
                if (!trader.inventory.innerContainer.TryAdd(item, true)) {
                    item.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
