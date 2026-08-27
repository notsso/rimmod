using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

namespace lotr {
    public class GameComponent_LotrPathEvents : GameComponent {
        // События по путям и последовательностям (9..4)
        private static readonly Dictionary<Pathway, Dictionary<int, List<IncidentDef>>> PathEvents =
            new Dictionary<Pathway, Dictionary<int, List<IncidentDef>>>
            {
                {
                    Pathway.Warrior,
                    new Dictionary<int, List<IncidentDef>>
                    {
                        { 9, new List<IncidentDef> { IncidentDef.Named("GiantWarriorLair_Incident") } },
                        { 8, new List<IncidentDef> { IncidentDef.Named("GiantSquireLair_Incident") } },
                        { 7, new List<IncidentDef> { IncidentDef.Named("BlueGiantLair_Incident") } },
                        { 6, new List<IncidentDef> { IncidentDef.Named("DawnGiantLair_Incident") } },
                        { 5, new List<IncidentDef> { IncidentDef.Named("GreyGiantLair_Incident") } },
                        { 4, new List<IncidentDef> { IncidentDef.Named("DivineGiantLair_Incident") } }
                    }
                },
                {
                    Pathway.Hunter,
                    new Dictionary<int, List<IncidentDef>>
                    {
                        { 9, new List<IncidentDef> { IncidentDef.Named("MysticalMarsh_Incident") } },
                        { 8, new List<IncidentDef> { IncidentDef.Named("JungleParrot_Incident") } },
                        { 7, new List<IncidentDef> { IncidentDef.Named("FireSalamanderRuins_Incident"), IncidentDef.Named("StrangeVolcano_Incident") } },
                        { 6, new List<IncidentDef> { IncidentDef.Named("SpiderForest_Incident"), IncidentDef.Named("DesertRuins_Incident") } },
                        { 5, new List<IncidentDef> { IncidentDef.Named("FoggyForest_Incident"), IncidentDef.Named("JungleCabin_Incident") } },
                        { 4, new List<IncidentDef> { IncidentDef.Named("MagmaGiantVolcano_Incident") } }
                    }
                },
                {
                    Pathway.Assassin,
                    new Dictionary<int, List<IncidentDef>>
                    {
                        { 9, new List<IncidentDef> { IncidentDef.Named("SerpentBirdTower_Incident") } },
                        { 8, new List<IncidentDef> { IncidentDef.Named("DemonThroatHoneyguideLair_Incident") } },
                        { 7, new List<IncidentDef> { IncidentDef.Named("AgatePeacockMeadow_Incident") } },
                        { 6, new List<IncidentDef> { IncidentDef.Named("BlackWidowForest_Incident"), IncidentDef.Named("SuccubusTemple_Incident") } },
                        { 5, new List<IncidentDef> { IncidentDef.Named("FlowerFacedBatCave_Incident"), IncidentDef.Named("TwoTailedSnakeHollow_Incident") } },
                        { 4, new List<IncidentDef> { IncidentDef.Named("PlagueSerpentLair_Incident"), IncidentDef.Named("SilverHunterTerritory_Incident") } }
                    }
                },
                {
                    Pathway.Bard,
                    new Dictionary<int, List<IncidentDef>>
                    {
                        { 9, new List<IncidentDef> { IncidentDef.Named("FireBirdNest_Incident") } },
                        { 8, new List<IncidentDef> { IncidentDef.Named("MirrorHedgehogGlade_Incident") } },
                        { 7, new List<IncidentDef> { IncidentDef.Named("DawnRoosterMeadow_Incident") } },
                        { 6, new List<IncidentDef> { IncidentDef.Named("SpiritPactBirdGrove_Incident") } },
                        { 5, new List<IncidentDef> { IncidentDef.Named("DawnRoosterKingThrone_Incident") } },
                        { 4, new List<IncidentDef> { IncidentDef.Named("SunDivineBirdPeak_Incident") } }
                    }
                }
            };

        private int nextCheckTick = -1;
        private const int InitialDelayTicks = 60000;      // 1 день
        private const int CheckIntervalTicks = 60000;     // 1 день
        private const int CooldownAfterEventTicks = 3 * 60000; // 3 дня после успешного события
        private const float BaseTriggerChance = 0.3f;     // 30% каждый день

        public GameComponent_LotrPathEvents(Game game) { }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref nextCheckTick, "nextCheckTick", -1);
        }

        public override void GameComponentTick() {
            base.GameComponentTick();

            if (nextCheckTick < 0)
                nextCheckTick = Find.TickManager.TicksGame + InitialDelayTicks;

            if (Find.TickManager.TicksGame < nextCheckTick)
                return;

            if (!Rand.Chance(BaseTriggerChance)) {
                nextCheckTick = Find.TickManager.TicksGame + CheckIntervalTicks;
                return;
            }

            if (TryTriggerRandomEvent())
                nextCheckTick = Find.TickManager.TicksGame + CooldownAfterEventTicks;
            else
                nextCheckTick = Find.TickManager.TicksGame + CheckIntervalTicks;
        }

        private bool TryTriggerRandomEvent() {
            // Log.Message($"[LotrPathEvents] Attempting to trigger event. Tick={Find.TickManager.TicksGame}");

            if (Find.AnyPlayerHomeMap == null)
                return false;

            // 1. Собираем потусторонних колонистов
            var colonists = PawnsFinder.AllMaps_FreeColonists;
            var beyonders = new List<(Pathway path, int sequence)>();
            foreach (var pawn in colonists) {
                if (BeyonderUtility.IsBeyonder(pawn)) {
                    Pathway path = BeyonderUtility.GetBeyonderPathway(pawn);
                    int seq = BeyonderUtility.GetBeyonderSequence(pawn);
                    if (path != Pathway.No_pathway && seq >= 4 && seq <= 9)
                        beyonders.Add((path, seq));
                }
            }

            // 2. Веса путей
            var pathWeights = new Dictionary<Pathway, float>();
            foreach (var path in PathEvents.Keys)
                pathWeights[path] = 1f; // базовый вес

            foreach (var (path, seq) in beyonders) {
                if (pathWeights.ContainsKey(path)) {
                    float bonus = 11f - seq; // seq 9 -> 2, 8 -> 3, ..., 4 -> 7
                    pathWeights[path] += bonus;
                }
            }

            // Выбираем путь
            Pathway selectedPath = WeightedRandomKey(pathWeights);
            if (selectedPath == Pathway.No_pathway || !PathEvents.ContainsKey(selectedPath))
                return false;

            var eventsBySequence = PathEvents[selectedPath];
            if (eventsBySequence == null || eventsBySequence.Count == 0)
                return false;

            // Log.Message($"[LotrPathEvents] Selected pathway: {selectedPath}");

            // 3. Веса последовательностей
            float[] sequenceWeights = new float[10]; // индексы 4..9
            // База: последовательность 9 = 1
            sequenceWeights[9] = 1f;

            // Получаем потусторонних этого пути
            var pathMembers = beyonders.Where(b => b.path == selectedPath).ToList();
            if (pathMembers.Any()) {
                // Определяем максимальную последовательность (самый низкий номер)
                int maxSeq = pathMembers.Max(b => b.sequence); // например, 7
                // Все последовательности от 9 до maxSeq получают базовый вес 1
                for (int s = 9; s >= maxSeq; s--) {
                    if (s >= 4 && s <= 9)
                        sequenceWeights[s] = Mathf.Max(1f, sequenceWeights[s]);
                }

                // Добавляем бонусы к последовательности s-1
                foreach (var (_, seq) in pathMembers) {
                    int targetSeq = seq - 1;
                    if (targetSeq >= 4 && targetSeq <= 9) {
                        float bonus = 11f - seq; // seq 9 -> 2, 8 -> 3, ..., 4 -> 7
                        sequenceWeights[targetSeq] += bonus;
                    }
                }
            }

            // Выбираем последовательность
            int selectedSeq = WeightedRandomIndex(sequenceWeights, 4, 9);
            if (selectedSeq < 0 || !eventsBySequence.ContainsKey(selectedSeq))
                return false;

            // Log.Message($"[LotrPathEvents] Selected sequence: {selectedSeq}");

            // 4. Выбираем случайное событие из списка
            var eventList = eventsBySequence[selectedSeq];
            if (eventList == null || eventList.Count == 0)
                return false;

            IncidentDef incident = eventList.RandomElement();
            if (incident == null)
                return false;

            // Log.Message($"[LotrPathEvents] Firing incident: {incident.defName}");

            // 5. Запускаем событие
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, Find.World);
            parms.forced = true;
            bool fired = Find.Storyteller.TryFire(new FiringIncident(incident, null, parms));

            // Log.Message($"[LotrPathEvents] TryFire result: {fired}");

            return fired;
        }

        // Вспомогательные методы
        private static Pathway WeightedRandomKey(Dictionary<Pathway, float> weights) {
            float total = weights.Values.Sum();
            if (total <= 0f) return Pathway.No_pathway;
            float rand = Rand.Value * total;
            foreach (var kvp in weights) {
                rand -= kvp.Value;
                if (rand <= 0f)
                    return kvp.Key;
            }
            return weights.Last().Key;
        }

        private static int WeightedRandomIndex(float[] weights, int minIndex, int maxIndex) {
            float total = 0f;
            for (int i = minIndex; i <= maxIndex; i++)
                total += weights[i];
            if (total <= 0f) return -1;
            float rand = Rand.Value * total;
            for (int i = minIndex; i <= maxIndex; i++) {
                rand -= weights[i];
                if (rand <= 0f)
                    return i;
            }
            return maxIndex;
        }
    }
}
