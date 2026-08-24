using System.Collections.Generic;
using System.Linq;

using Verse;
using Verse.AI.Group;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

namespace lotr {
    public class GameComponent_FirstMeeting : GameComponent {
        private int ticksUntilCheck;
        private const int InitialDelayTicks = 7 * 60000;
        private const int BetweenEventsDelayTicks = 5 * 60000;
        private const int CheckIntervalTicks = 60000;
        private const float ChancePerCheck = 0.2f;

        public HashSet<int> talkedPawnsIds = new HashSet<int>();
        public HashSet<string> alliedFactionDefNames = new HashSet<string>();
        public Dictionary<string, int> alliedAtTicks = new Dictionary<string, int>();

        public GameComponent_FirstMeeting(Game game) { }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Collections.Look(ref talkedPawnsIds, "talkedPawnsIds", LookMode.Value);
            Scribe_Collections.Look(ref alliedFactionDefNames, "alliedFactionDefNames", LookMode.Value);
            Scribe_Collections.Look(ref alliedAtTicks, "alliedAtTicks", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                talkedPawnsIds = talkedPawnsIds ?? new HashSet<int>();
                alliedFactionDefNames = alliedFactionDefNames ?? new HashSet<string>();
                alliedAtTicks = alliedAtTicks ?? new Dictionary<string, int>();
            }
        }

        public bool IsFactionAllied(Faction f) => alliedFactionDefNames.Contains(f.def.defName);
        public int GetAlliedTick(Faction f) => alliedAtTicks.TryGetValue(f.def.defName, out int tick) ? tick : -1;
        public void SetFactionAllied(Faction f) {
            alliedFactionDefNames.Add(f.def.defName);
            alliedAtTicks[f.def.defName] = Find.TickManager.TicksGame;
        }

        public bool IsPawnTalked(Pawn p) => talkedPawnsIds.Contains(p.thingIDNumber);
        public void MarkPawnTalked(Pawn p) => talkedPawnsIds.Add(p.thingIDNumber);

        public override void GameComponentTick() {
            base.GameComponentTick();

            if (ticksUntilCheck == 0) {
                ticksUntilCheck = Find.TickManager.TicksGame + InitialDelayTicks;
                return;
            }

            if (Find.TickManager.TicksGame < ticksUntilCheck)
                return;

            if (Rand.Chance(ChancePerCheck)) {
                if (TryTriggerEvent())
                    ticksUntilCheck = Find.TickManager.TicksGame + BetweenEventsDelayTicks;
                else
                    ticksUntilCheck = Find.TickManager.TicksGame + CheckIntervalTicks;
            } else {
                ticksUntilCheck = Find.TickManager.TicksGame + CheckIntervalTicks;
            }
        }

        private bool TryTriggerEvent() {
            if (Find.AnyPlayerHomeMap == null) return false;

            // Собираем фракции, с которыми ещё нет союза
            List<Faction> availableFactions = new List<Faction>();
            foreach (string defName in BeyonderUtility.FactionDefNames) {
                Faction faction = Find.FactionManager.AllFactions
                    .FirstOrDefault(f => f.def.defName == defName);
                if (faction != null && !IsFactionAllied(faction))
                    availableFactions.Add(faction);
            }

            if (availableFactions.Count == 0) return false;

            Faction chosenFaction = availableFactions.RandomElement();
            IncidentDef incident = IncidentDef.Named("lotr_FirstMeeting");
            if (incident == null) return false;

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.AnyPlayerHomeMap);
            parms.faction = chosenFaction;
            parms.forced = true;
            Find.Storyteller.TryFire(new FiringIncident(incident, null, parms));
            return true;
        }
    }

    public class IncidentWorker_FirstMeeting : IncidentWorker_VisitorGroup {
        private const string PawnKindDefName = "lotr_Envoy";

        protected override bool TryResolveParmsGeneral(IncidentParms parms) {
            Map map = (Map)parms.target;
            if (!parms.spawnCenter.IsValid &&
                !RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Neutral, false, null)) {
                return false;
            }

            if (parms.faction == null)
                return false;

            parms.points = 100f;
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            Map map = (Map)parms.target;
            if (!base.TryResolveParms(parms))
                return false;

            Faction faction = parms.faction;
            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamed(PawnKindDefName);
            Pawn representative = PawnGenerator.GeneratePawn(kindDef, faction, map.Tile);
            if (representative == null)
                return false;

            if (representative.inventory != null)
                PawnInventoryGenerator.GiveRandomFood(representative);
            if (representative.apparel != null)
                PawnApparelGenerator.GenerateStartingApparelFor(representative, new PawnGenerationRequest(kindDef, faction, PawnGenerationContext.NonPlayer));

            GenSpawn.Spawn(representative, parms.spawnCenter, map, Rot4.Random);

            List<Pawn> lordPawns = new List<Pawn> { representative };
            LordMaker.MakeNewLord(faction, CreateLordJob(parms, lordPawns), map, lordPawns);

            SendStandardLetter(this.def.letterLabel, this.def.letterText, LetterDefOf.NeutralEvent, parms, representative, new NamedArgument[0]);
            return true;
        }

        private void GiveBasicApparel(Pawn pawn) {
            if (pawn.apparel == null) return;

            // Простые предметы одежды (можно заменить на свои)
            // GiveApparel(pawn, ThingDefOf.Apparel_BasicShirt);
            // GiveApparel(pawn, ThingDefOf.Apparel_Pants);
        }

        private void GiveApparel(Pawn pawn, ThingDef apparelDef) {
            Thing apparel = ThingMaker.MakeThing(apparelDef);
            if (apparel != null) {
                apparel.stackCount = 1;
                pawn.apparel.Wear((Apparel)apparel, false);
            }
        }

        private void GiveFood(Pawn pawn) {
            if (pawn?.inventory == null) return;
            ThingDef foodDef = ThingDef.Named("MealSurvivalPack");
            Thing food = ThingMaker.MakeThing(foodDef);
            food.stackCount = Rand.RangeInclusive(1, 3);
            pawn.inventory.innerContainer.TryAdd(food, true);
        }
    }

    public class DefModExtension_FirstMeeting : DefModExtension { }

    public class Dialog_FirstMeeting : Window {
        public Pawn representative;
        public Faction faction;
        public GameComponent_FirstMeeting gameComp;

        public Dialog_FirstMeeting(Pawn representative) {
            this.representative = representative;
            this.faction = representative.Faction;
            this.gameComp = Current.Game.GetComponent<GameComponent_FirstMeeting>();
            this.forcePause = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.doCloseButton = false;
            this.doCloseX = false;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(500f, 300f);

        public override void DoWindowContents(Rect inRect) {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "Представитель Ордена");
            Text.Font = GameFont.Small;
            string message = $"Представитель фракции {faction.Name} предлагает вам присоединиться к тайному союзу.";
            Widgets.Label(new Rect(0f, 40f, inRect.width, 100f), message);

            float buttonWidth = 150f;
            float buttonHeight = 40f;
            float y = inRect.height - buttonHeight - 10f;

            if (Widgets.ButtonText(new Rect(inRect.width / 2f - buttonWidth - 20f, y, buttonWidth, buttonHeight), "Присоединиться")) {
                gameComp.SetFactionAllied(faction);
                CloseAndExit(true);
            }

            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 20f, y, buttonWidth, buttonHeight), "Отказаться")) {
                CloseAndExit(false);
            }
        }

        private void CloseAndExit(bool accepted) {
            gameComp.MarkPawnTalked(representative);

            if (accepted) {
                gameComp.SetFactionAllied(faction);
                Messages.Message($"Вы присоединились к {faction.Name}.", new LookTargets(representative), MessageTypeDefOf.NeutralEvent, true);
            } else {
                Messages.Message($"Вы отказались от предложения {faction.Name}.", new LookTargets(representative), MessageTypeDefOf.NeutralEvent, true);
            }

            Find.WindowStack.TryRemove(this, true);

            if (representative.Spawned)
                representative.ExitMap(true, Rot4.Random);
        }
    }
}
