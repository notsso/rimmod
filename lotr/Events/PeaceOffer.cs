using System.Collections.Generic;
using System.Linq;

using Verse;
using Verse.AI.Group;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

namespace lotr {
    public class GameComponent_PeaceOffer : GameComponent {
        private int ticksUntilCheck;
        private int lastPeaceOfferTick;
        private const int CheckIntervalTicks = 60000;      // раз в день
        private const float ChancePerCheck = 0.1f;         // 10% в день
        private const int MinIntervalTicks = 60 * 60000;   // год (60 дней)
        private const string FactionDefName = "lotr_IronAndBloodCrossOrder";

        public GameComponent_PeaceOffer(Game game) { }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilCheck, "ticksUntilCheck", 0);
            Scribe_Values.Look(ref lastPeaceOfferTick, "lastPeaceOfferTick", 0);
        }

        public override void GameComponentTick() {
            base.GameComponentTick();

            if (ticksUntilCheck == 0) {
                ticksUntilCheck = Find.TickManager.TicksGame + CheckIntervalTicks;
                return;
            }

            if (Find.TickManager.TicksGame < ticksUntilCheck)
                return;

            ticksUntilCheck = Find.TickManager.TicksGame + CheckIntervalTicks;

            Faction faction = Find.FactionManager.AllFactions
                .FirstOrDefault(f => f.def.defName == FactionDefName);
            if (faction == null) return;

            // Только если враждебны
            if (!faction.HostileTo(Faction.OfPlayer)) return;

            // Только если ранее был союз
            GameComponent_FirstMeeting firstMeetingComp = Current.Game.GetComponent<GameComponent_FirstMeeting>();
            if (firstMeetingComp == null || !firstMeetingComp.IsFactionAllied(faction)) return;

            // Если первый раз заметили вражду — начинаем отсчёт года
            if (lastPeaceOfferTick == 0) {
                lastPeaceOfferTick = Find.TickManager.TicksGame;
                return;
            }

            // Не чаще раза в год
            if (Find.TickManager.TicksGame - lastPeaceOfferTick < MinIntervalTicks)
                return;

            // Шанс 10% в день
            if (!Rand.Chance(ChancePerCheck))
                return;

            // Запускаем событие
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.AnyPlayerHomeMap);
            parms.faction = faction;
            parms.forced = true;
            Find.Storyteller.TryFire(new FiringIncident(IncidentDef.Named("lotr_PeaceOffer"), null, parms));

            lastPeaceOfferTick = Find.TickManager.TicksGame;
        }
    }

    public class IncidentWorker_PeaceOffer : IncidentWorker_VisitorGroup {
        private const string FactionDefName = "lotr_IronAndBloodCrossOrder";
        private const string PawnKindDefName = "lotr_PeaceEnvoy";

        protected override bool TryResolveParmsGeneral(IncidentParms parms) {
            Map map = (Map)parms.target;
            if (!parms.spawnCenter.IsValid &&
                !RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Neutral, false, null))
                return false;

            Faction faction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == FactionDefName);
            if (faction == null) return false;

            parms.faction = faction;
            parms.points = 100f;
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            Map map = (Map)parms.target;
            if (!base.TryResolveParms(parms)) return false;

            Faction faction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == FactionDefName);
            if (faction == null) return false;

            parms.faction = faction;

            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamed(PawnKindDefName);
            Pawn envoy = PawnGenerator.GeneratePawn(kindDef, faction, map.Tile);
            if (envoy == null) return false;

            if (envoy.inventory != null)
                PawnInventoryGenerator.GiveRandomFood(envoy);
            if (envoy.apparel != null)
                PawnApparelGenerator.GenerateStartingApparelFor(envoy, new PawnGenerationRequest(kindDef, faction, PawnGenerationContext.NonPlayer));

            GenSpawn.Spawn(envoy, parms.spawnCenter, map, Rot4.Random);

            List<Pawn> lordPawns = new List<Pawn> { envoy };
            LordMaker.MakeNewLord(faction, CreateLordJob(parms, lordPawns), map, lordPawns);

            SendStandardLetter(this.def.letterLabel, this.def.letterText, LetterDefOf.NeutralEvent, parms, envoy, new NamedArgument[0]);
            return true;
        }
    }

    public class DefModExtension_PeaceOffer : DefModExtension { }

    public class Dialog_PeaceOffer : Window {
        public Pawn representative;
        public Faction faction;

        public Dialog_PeaceOffer(Pawn representative) {
            this.representative = representative;
            this.faction = representative.Faction;
            forcePause = true;
            closeOnAccept = false;
            closeOnCancel = false;
            doCloseButton = false;
            doCloseX = false;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(500f, 300f);

        public override void DoWindowContents(Rect inRect) {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "Дипломат Ордена");
            Text.Font = GameFont.Small;
            string message = $"Дипломат фракции {faction.Name} предлагает забыть обиды и восстановить нейтралитет.";
            Widgets.Label(new Rect(0f, 40f, inRect.width, 100f), message);

            float buttonWidth = 150f;
            float buttonHeight = 40f;
            float y = inRect.height - buttonHeight - 10f;

            if (Widgets.ButtonText(new Rect(inRect.width / 2f - buttonWidth - 20f, y, buttonWidth, buttonHeight), "Забыть обиды")) {
                int goodwillChange = -faction.GoodwillWith(Faction.OfPlayer);
                faction.TryAffectGoodwillWith(Faction.OfPlayer, goodwillChange, false, true, null, null);
                Messages.Message($"Вы заключили мир с {faction.Name}.", new LookTargets(representative), MessageTypeDefOf.NeutralEvent, true);
                CloseAndExit();
            }

            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 20f, y, buttonWidth, buttonHeight), "Продолжить войну")) {
                CloseAndExit();
            }
        }

        private void CloseAndExit() {
            Find.WindowStack.TryRemove(this, true);
            if (representative.Spawned)
                representative.ExitMap(true, Rot4.Random);
        }
    }
}
