using System.Collections.Generic;
using System.Linq;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

using Verse;

namespace lotr {
    public class BeyonderGatheringExtension : DefModExtension {
        public TraderKindDef traderKindDef;
    }

    public class IncidentWorker_BeyonderGathering : IncidentWorker {
        private const int MinTileDistance = 3;
        private const int MaxTileDistance = 6;

        protected override bool CanFireNowSub(IncidentParms parms) {
            return parms.target is World && base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            World world = parms.target as World;
            if (world == null)
                return false;

            int baseTile = GetPlayerHomeTile();
            if (baseTile < 0) {
                Log.Error("[BeyonderGathering] No player home tile found.");
                return false;
            }

            // Исправлено: out PlanetTile, а не out int
            PlanetTile foundTile;
            if (!TileFinder.TryFindPassableTileWithTraversalDistance(
                baseTile, MinTileDistance, MaxTileDistance, out foundTile)) {
                Log.Warning("[BeyonderGathering] Could not find nearby tile.");
                return false;
            }

            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamed("BeyonderGatheringSite");
            if (def == null) {
                Log.Error("[BeyonderGathering] Missing WorldObjectDef BeyonderGatheringSite.");
                return false;
            }

            WorldObject site = WorldObjectMaker.MakeWorldObject(def);
            site.Tile = foundTile;
            site.SetFaction(null);
            Find.WorldObjects.Add(site);

            SendStandardLetter(parms, site);
            return true;
        }

        private int GetPlayerHomeTile() {
            Settlement playerSettlement = Find.WorldObjects.AllWorldObjects
                .OfType<Settlement>()
                .FirstOrDefault(s => s.Faction == Faction.OfPlayer);

            if (playerSettlement != null)
                return playerSettlement.Tile;

            Map map = Find.AnyPlayerHomeMap;
            if (map != null && map.Parent is Settlement mapSettlement)
                return mapSettlement.Tile;

            return -1;
        }
    }

    public class WorldObjectCompProperties_BeyonderGatheringTrader : WorldObjectCompProperties {
        public WorldObjectCompProperties_BeyonderGatheringTrader() {
            compClass = typeof(BeyonderGatheringTraderComp);
        }
    }

    public class BeyonderGatheringTraderComp : WorldObjectComp, ITrader {
        private TraderKindDef traderKindDef;
        private List<Thing> goods = new List<Thing>();
        private bool goodsGenerated;
        private int randomPriceFactorSeed;

        public int expirationTick = -1;     // таймаут (3 дня)

        public const int TimeoutTicks = 3 * 60000; // 3 игровых дня

        public TraderKindDef TraderKind => traderKindDef;
        public IEnumerable<Thing> Goods => goods;
        public int RandomPriceFactorSeed => randomPriceFactorSeed;
        public string TraderName => parent.LabelCap;
        public bool CanTradeNow => true;
        public TradeCurrency TradeCurrency => traderKindDef?.tradeCurrency ?? TradeCurrency.Silver;
        public Faction Faction => parent.Faction;
        public float TradePriceImprovementOffsetForPlayer => 0f; // добавлено

        public override void Initialize(WorldObjectCompProperties props) {
            base.Initialize(props);
            var ext = parent.def.GetModExtension<BeyonderGatheringExtension>();
            traderKindDef = ext?.traderKindDef;
            randomPriceFactorSeed = Rand.Int;
            expirationTick = Find.TickManager.TicksGame + TimeoutTicks;
        }

        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_Defs.Look(ref traderKindDef, "traderKindDef");
            Scribe_Collections.Look(ref goods, "goods", LookMode.Deep);
            Scribe_Values.Look(ref goodsGenerated, "goodsGenerated", false);
            Scribe_Values.Look(ref randomPriceFactorSeed, "randomPriceFactorSeed", 0);
            Scribe_Values.Look(ref expirationTick, "expirationTick", -1);
        }

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan) {
            if (!caravan.IsPlayerControlled)
                yield break;

            // Караван должен стоять на том же тайле и не двигаться
            if (caravan.Tile != parent.Tile || caravan.pather.Moving)
                yield break;

            // Проверка таймаута
            if (expirationTick > 0 && Find.TickManager.TicksGame >= expirationTick) {
                yield break;
            }

            yield return new Command_Action {
                defaultLabel = "Trade",
                defaultDesc = "Trade with the beyonder gathering.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Trade", true),
                action = delegate {
                    Pawn negotiator = BestNegotiator(caravan);
                    if (negotiator == null) {
                        Messages.Message("No negotiator available.", MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    EnsureGoodsGenerated();
                    Find.WindowStack.Add(new Dialog_Trade(negotiator, this));
                }
            };
        }

        private Pawn BestNegotiator(Caravan caravan) {
            return caravan.PawnsListForReading
                .Where(p => p.RaceProps.Humanlike && !p.Dead && !p.Downed)
                .OrderByDescending(p => p.GetStatValue(StatDefOf.TradePriceImprovement))
                .FirstOrDefault();
        }

        private void EnsureGoodsGenerated() {
            if (goodsGenerated)
                return;

            goodsGenerated = true;

            // Базовый ассортимент из TraderKindDef
            ThingSetMakerParams parms = default;
            parms.traderDef = traderKindDef;
            parms.tile = new PlanetTile?(parent.Tile);
            parms.makingFaction = parent.Faction;

            foreach (Thing thing in ThingSetMakerDefOf.TraderStock.root.Generate(parms)) {
                goods.Add(thing);
            }

            // Для каждого пути генерируем ингредиенты с шансом по последовательности
            foreach (Pathway pathway in BeyonderUtility.AllPathways) {
                if (!BeyonderUtility.TryGetPathwayIngredients(pathway, out var ingredientDefs))
                    continue;

                // Проходим по последовательностям 9..4
                for (int seq = 9; seq >= 4; seq--) {
                    if (!ingredientDefs.TryGetValue(seq, out string[] defs))
                        continue;

                    foreach (string defName in defs) {
                        // Шанс: 0.6 для 9, 0.5 для 8, ..., 0.1 для 4
                        float chance = 0.6f - (9 - seq) * 0.1f;
                        AddRandomIngredient(defName, chance);
                    }
                }
            }
        }

        private void AddRandomIngredient(string defName, float chance) {
            if (!Rand.Chance(chance))
                return;

            ThingDef def = ThingDef.Named(defName);
            if (def == null)
                return;

            Thing item = ThingMaker.MakeThing(def);
            item.stackCount = 1;
            goods.Add(item);
        }

        // =================== ITrader ===================

        public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator) {
            // Можно вернуть пустой список — это не влияет на стандартное окно торговли,
            // но для полной корректности можно использовать TradeUtility.ColonyThingsWillingToBuy
            return Enumerable.Empty<Thing>();
        }

        public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator) {
            Thing thing = toGive.SplitOff(countToGive);
            thing?.Destroy(DestroyMode.Vanish);
        }

        public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator) {
            Thing thing = toGive.SplitOff(countToGive);
            Caravan caravan = playerNegotiator.GetCaravan();
            if (caravan != null) {
                CaravanInventoryUtility.GiveThing(caravan, thing);
            } else {
                thing?.Destroy(DestroyMode.Vanish);
            }

            if (toGive.stackCount <= 0)
                goods.Remove(toGive);
        }

        public override void CompTick() {
            base.CompTick();

            // Если открыто окно торговли, не уничтожаем объект
            if (Find.WindowStack.WindowOfType<Dialog_Trade>() != null)
                return;

            // Если не торговали и истёк таймаут – удаляем объект
            if (expirationTick > 0 && Find.TickManager.TicksGame >= expirationTick) {
                Messages.Message("Beyonder gathering ended", MessageTypeDefOf.NeutralEvent);
                parent.Destroy();
            }
        }
    }

    public class GameComponent_BeyonderGatheringScheduler : GameComponent {
        private int nextGatheringTick = -1;
        private const int GatheringIntervalTicks = 28 * 60000; // 28 дней

        public GameComponent_BeyonderGatheringScheduler(Game game) { }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref nextGatheringTick, "nextGatheringTick", -1);
        }

        public override void GameComponentTick() {
            base.GameComponentTick();

            if (nextGatheringTick < 0)
                nextGatheringTick = Find.TickManager.TicksGame + GatheringIntervalTicks;

            if (Find.TickManager.TicksGame < nextGatheringTick)
                return;

            // Пора запускать событие
            IncidentDef incident = IncidentDef.Named("BeyonderGathering_Incident");
            if (incident != null) {
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, Find.World);
                parms.forced = true;
                Find.Storyteller.TryFire(new FiringIncident(incident, null, parms));
            }

            nextGatheringTick = Find.TickManager.TicksGame + GatheringIntervalTicks;
        }
    }
}