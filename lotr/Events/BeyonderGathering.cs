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

        public bool hasTraded = false;      // было ли открыто окно торговли
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
            Scribe_Values.Look(ref hasTraded, "hasTraded", false);
            Scribe_Values.Look(ref expirationTick, "expirationTick", -1);
        }

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan) {
            if (!caravan.IsPlayerControlled)
                yield break;

            // Караван должен стоять на том же тайле и не двигаться
            if (caravan.Tile != parent.Tile || caravan.pather.Moving)
                yield break;

            // Проверка таймаута
            if (!hasTraded && expirationTick > 0 && Find.TickManager.TicksGame >= expirationTick) {
                parent.Destroy();
                yield break;
            }

            yield return new Command_Action {
                defaultLabel = "Trade".Translate(),
                defaultDesc = "Trade with the beyonder gathering.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Trade", true),
                action = delegate {
                    Pawn negotiator = BestNegotiator(caravan);
                    if (negotiator == null) {
                        Messages.Message("No negotiator available.", MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    EnsureGoodsGenerated();
                    hasTraded = true;
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

            // Редкие потусторонние ингредиенты с рандомным шансом
            AddRandomIngredient("lotr_MarshCrystal", 0.6f);
            AddRandomIngredient("lotr_BloodRedChestnut", 0.6f);
            AddRandomIngredient("lotr_CuspidsParrotTongue", 0.5f);
            AddRandomIngredient("lotr_CorpseLilyRootstock", 0.5f);
            AddRandomIngredient("lotr_FireSalamanderGland", 0.4f);
            AddRandomIngredient("lotr_MagmaElfCore", 0.4f);
            AddRandomIngredient("lotr_BlackHuntingSpiderEyes", 0.3f);
            AddRandomIngredient("lotr_SphinxBrain", 0.3f);
            AddRandomIngredient("lotr_DemonicWolfClaws", 0.2f);
            AddRandomIngredient("lotr_ForestHunterTongue", 0.2f);
            AddRandomIngredient("lotr_MagmaGiantCore", 0.1f);
            AddRandomIngredient("lotr_StoneofCatastrophe", 0.1f);
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

            // Если торговали и окно торговли закрылось – удаляем объект
            if (hasTraded && Find.WindowStack.WindowOfType<Dialog_Trade>() == null) {
                parent.Destroy();
                return;
            }

            // Если не торговали и истёк таймаут – удаляем объект
            if (!hasTraded && expirationTick > 0 && Find.TickManager.TicksGame >= expirationTick) {
                parent.Destroy();
            }
        }
    }
}