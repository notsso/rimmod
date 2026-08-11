using System.Collections.Generic;
using System.Linq;
using System;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

namespace lotr {
    public class GameCondition_BloodMoon : GameCondition {
        public override void Init() {
            base.Init();
            // Можно сразу добавить всем потусторонним пешкам sanity loss, если его нет
        }

        public override void GameConditionTick() {
            base.GameConditionTick();
            // Периодически проверяем, что у всех Beyonder есть sanityLoss
            if (Find.TickManager.TicksGame % 2500 == 0) {
                EnsureSanityLossOnBeyonders();
            }
        }

        private void EnsureSanityLossOnBeyonders() {
            foreach (Pawn pawn in AffectedPawns()) {
                if (pawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.lotr_SanityLoss) == null) {
                    Hediff hediff = HediffMaker.MakeHediff(LotrDefOf.lotr_SanityLoss, pawn);
                    pawn.health.AddHediff(hediff);
                }
            }
        }

        private IEnumerable<Pawn> AffectedPawns() {
            Map map = this.SingleMap;
            if (map == null) yield break;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned) {
                if (pawn.health.hediffSet.hediffs.Any(h => h is Beyonder_Hediff))
                    yield return pawn;
            }
        }

        public override SkyTarget? SkyTarget(Map map) {
            Color skyColor = new Color(0.75f, 0.05f, 0.05f);
            Color shadowColor = new Color(0.3f, 0.02f, 0.02f);
            Color overlayColor = new Color(0.5f, 0.0f, 0.0f);
            SkyColorSet colorSet = new SkyColorSet(skyColor, shadowColor, overlayColor, 1f);

            // glow, colorSet, lightsourceShineSize, lightsourceShineIntensity
            return new SkyTarget(0.9f, colorSet, 0.4f, 0.6f);
        }

        public override float SkyTargetLerpFactor(Map map) => 1f;
    }

    public class IncidentWorker_BloodMoon : IncidentWorker {
        protected override bool CanFireNowSub(IncidentParms parms) {
            // Если цель не задана (ручной вызов), подставляем текущую карту
            if (parms.target == null) {
                parms.target = Find.CurrentMap;
            }


            Map map = parms.target as Map;
            if (map == null) {
                return false;
            }

            if (!base.CanFireNowSub(parms)) {
                return false;
            }

            // ночь с 21 до 6
            int hour = GenLocalDate.HourOfDay(map);
            if (!(hour >= 21 || hour < 6)) {
                return false;
            }

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            Map map = (parms.target as Map) ?? Find.CurrentMap;
            if (map == null)
                return false;

            int durationTicks = Mathf.RoundToInt(0.3f * 60000); // около 7 игровых часов
            GameCondition condition = GameConditionMaker.MakeCondition(
                DefDatabase<GameConditionDef>.GetNamed("BloodMoon"), durationTicks);
            map.gameConditionManager.RegisterCondition(condition);

            Messages.Message("Кровавая Луна взошла над поселением!", MessageTypeDefOf.NegativeEvent);
            return true;
        }
    }

    public class SitePartWorker_MysticalMarsh : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            Log.Message($"PostMapGenerate: mystical swamp on {map.uniqueID}");

            SitePart sitePart = map.ParentHolder as SitePart;
            float threatPoints = sitePart?.parms.threatPoints ?? 0f;

            var spawner = new MapComponent_MarshSpawner(map);
            var weather = new MapComponent_ForcedWeather(map);

            spawner.Initialize(threatPoints);

            map.components.Add(spawner);
            map.components.Add(weather);
        }
    }

    public class IncidentWorker_MysticalMarsh : IncidentWorker {
        protected override bool CanFireNowSub(IncidentParms parms) {
            // Должна быть указана цель – мир (World)
            if (!(parms.target is World))
                return false;

            // Дополнительные условия (например, минимальный день)
            if (GenDate.DaysPassedSinceSettle < 5)
                return false;

            return base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            World world = parms.target as World;
            if (world == null) return false;

            SitePartDef sitePart = DefDatabase<SitePartDef>.GetNamed("MysticalMarsh_Site");
            if (sitePart == null) return false;

            // Используем выбранную игроком клетку (удобно для тестов)
            int tile = Find.WorldSelector.SelectedTile;
            if (tile < 0 || !Find.WorldGrid.InBounds(tile)) {
                Log.Error("Select a tile on the world map before running this incident.");
                return false;
            }

            float threatPoints = StorytellerUtility.DefaultThreatPointsNow(Find.World);

            WorldObjectDef siteObjectDef = DefDatabase<WorldObjectDef>.GetNamed("MysticalMarsh_World");
            Site site = (Site)WorldObjectMaker.MakeWorldObject(siteObjectDef);
            site.Tile = tile;

            Faction faction = Faction.OfAncients ?? Find.FactionManager.RandomEnemyFaction();
            site.SetFaction(faction);

            SitePartParams partParams = new SitePartParams { threatPoints = threatPoints };
            SitePart part = new SitePart(site, sitePart, partParams);
            site.AddPart(part);

            Find.WorldObjects.Add(site);

            Log.Message($"Site created with {site.parts.Count} parts. Main part def: {site.MainSitePartDef?.defName}");

            var timedRaids = site.GetComponent<TimedDetectionRaids>();
            if (timedRaids != null)
                timedRaids.ResetCountdown();

            CameraJumper.TryJump(site);
            SendStandardLetter(parms, site);
            return true;
        }
    }

    public class GenStep_CentralSwampTerrain : GenStep {
        public override int SeedPart => 12345;

        public override void Generate(Map map, GenStepParams parms) {
            TerrainDef soil = TerrainDef.Named("Soil");
            TerrainDef marsh = TerrainDef.Named("Marsh");
            TerrainDef richSoil = TerrainDef.Named("SoilRich");
            TerrainDef mud = TerrainDef.Named("Mud");
            TerrainDef water = TerrainDef.Named("WaterShallow");

            // База – обычная почва (Soil)
            foreach (IntVec3 cell in map.AllCells) {
                if (cell.InBounds(map))
                    map.terrainGrid.SetTerrain(cell, soil);
            }

            // Круги Marshy soil (Marsh) – больше и чаще
            int marshPatches = Rand.Range(25, 40);          // было 12-18
            for (int i = 0; i < marshPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(6, 14);            // было 4-8
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && Rand.Value < 0.75f) // чуть выше шанс
                        map.terrainGrid.SetTerrain(cell, marsh);
                }
            }

            // Круги Rich soil – тоже больше
            int richPatches = Rand.Range(15, 25);           // было 8-14
            for (int i = 0; i < richPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(5, 10);            // было 3-6
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && Rand.Value < 0.75f)
                        map.terrainGrid.SetTerrain(cell, richSoil);
                }
            }

            // Круги Mud с водой – тоже массивнее
            int mudPatches = Rand.Range(20, 30);            // было 10-15
            for (int i = 0; i < mudPatches; i++) {
                IntVec3 center = RandomCell(map);
                int outerRadius = Rand.Range(8, 15);       // было 5-9
                int waterRadius = Rand.Range(2, 4);        // было фиксировано 2

                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, outerRadius, true)) {
                    if (!cell.InBounds(map)) continue;
                    float dist = cell.DistanceTo(center);
                    if (dist <= waterRadius && Rand.Value < 0.85f)
                        map.terrainGrid.SetTerrain(cell, water);
                    else if (Rand.Value < 0.75f)
                        map.terrainGrid.SetTerrain(cell, mud);
                }
            }
        }

        private IntVec3 RandomCell(Map map) {
            // Случайная клетка в пределах всей карты
            return new IntVec3(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
        }
    }

    public class GenStep_CentralSwampPlants : GenStep {
        public override int SeedPart => 12346;

        // Структура описания растения
        private struct PlantConfig {
            public ThingDef def;
            public float chance;        // вероятность появления на клетке
            public FloatRange growthRange; // диапазон роста
        }

        public override void Generate(Map map, GenStepParams parms) {
            // подлесок
            var undergrowth = new List<PlantConfig> {
                // Трава — очень часто
                new PlantConfig { def = ThingDef.Named("Plant_Grass"), chance = 0.8f, growthRange = new FloatRange(0.7f, 1.0f) },
                // Куст — редко
                new PlantConfig { def = ThingDef.Named("Plant_Bush"), chance = 0.10f, growthRange = new FloatRange(0.6f, 1.0f) },
                // Ягодный куст — очень редко
                new PlantConfig { def = ThingDef.Named("Plant_Berry"), chance = 0.04f, growthRange = new FloatRange(0.6f, 1.0f) },
                // Одуванчик (цветы) — умеренно
                new PlantConfig { def = ThingDef.Named("Plant_Dandelion"), chance = 0.15f, growthRange = new FloatRange(0.6f, 1.0f) },
            };

            // деревья
            var trees = new List<PlantConfig> {
                // Берёза — умеренно
                new PlantConfig { def = ThingDef.Named("Plant_TreeBirch"), chance = 0.15f, growthRange = new FloatRange(0.5f, 0.9f) },
                // Пальма — умеренно
                new PlantConfig { def = ThingDef.Named("Plant_TreePalm"), chance = 0.15f, growthRange = new FloatRange(0.5f, 0.9f) },
                // Алоказия — умеренно
                new PlantConfig { def = ThingDef.Named("Plant_Alocasia"), chance = 0.20f, growthRange = new FloatRange(0.5f, 0.9f) },
                // дуб
                new PlantConfig { def = ThingDef.Named("Plant_Alocasia"), chance = 0.15f, growthRange = new FloatRange(0.5f, 0.9f) },
                // хз
                new PlantConfig { def = ThingDef.Named("Plant_TreePoplar"), chance = 0.40f, growthRange = new FloatRange(0.5f, 0.9f) },
                // сосна - игнор
                new PlantConfig { def = ThingDef.Named("Plant_TreePine"), chance = 0f, growthRange = new FloatRange(0.5f, 0.9f) },
            };

            // Обрабатываем всю карту
            foreach (IntVec3 cell in map.AllCells) {
                TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
                if (terrain == TerrainDef.Named("WaterShallow")) continue;

                foreach (var config in undergrowth) {
                    if (Rand.Value < config.chance) {
                        if (PlantUtility.CanEverPlantAt(config.def, cell, map)) {
                            Plant plant = (Plant)GenSpawn.Spawn(config.def, cell, map);
                            plant.Growth = config.growthRange.RandomInRange;
                        }
                    }
                }

                foreach (var config in trees) {
                    if (Rand.Value < config.chance) {
                        if (PlantUtility.CanEverPlantAt(config.def, cell, map)) {
                            Plant plant = (Plant)GenSpawn.Spawn(config.def, cell, map);
                            plant.Growth = config.growthRange.RandomInRange;
                        }
                    }
                }
            }
        }
    }

    public class GenStep_CentralSwampAnimals : GenStep {
        public override int SeedPart => 12347;

        public override void Generate(Map map, GenStepParams parms) {
            PawnKindDef boarKind = PawnKindDef.Named("lotr_MarshBoar");
            if (boarKind == null) return;

            int count = Rand.Range(2, 5); // 2,3,4
            for (int i = 0; i < count; i++) {
                IntVec3 cell = map.Center + new IntVec3(Rand.Range(-25, 25), 0, Rand.Range(-25, 25));
                if (!cell.InBounds(map)) continue;
                Pawn boar = PawnGenerator.GeneratePawn(boarKind, Faction.OfAncients);
                GenSpawn.Spawn(boar, cell, map);
            }
        }
    }

    public class MapComponent_MarshSpawner : MapComponent {
        private float threatPoints;
        private int ticksUntilNextSpawn;
        private bool initialized;

        private int SpawnInterval =>
            Mathf.RoundToInt(Mathf.Lerp(2500, 600, Mathf.Clamp01(threatPoints / 500f)));

        private int SpawnCount =>
            Mathf.RoundToInt(Mathf.Lerp(1, 4, Mathf.Clamp01(threatPoints / 500f)));

        public MapComponent_MarshSpawner(Map map) : base(map) {
            ticksUntilNextSpawn = 2500;
        }

        public void Initialize(float points) {
            threatPoints = points;
            ticksUntilNextSpawn = SpawnInterval;
            initialized = true;
            Log.Message($"MarshSpawner initialized: threatPoints={points}, interval={SpawnInterval}");
        }

        public override void MapComponentTick() {
            if (!initialized) {
                // Initialize(200);
                return;
            }

            ticksUntilNextSpawn--;
            if (ticksUntilNextSpawn <= 0) {
                Log.Message($"MarshSpawner: Spawning boars. ThreatPoints={threatPoints}, Interval={SpawnInterval}, Count={SpawnCount}");
                ticksUntilNextSpawn = SpawnInterval;
                SpawnBoars();
            }
        }

        private void SpawnBoars() {

            PawnKindDef boarKind = PawnKindDef.Named("lotr_MarshBoar");
            if (boarKind == null) return;

            IntVec3 center = map.Center;
            Log.Message($"SpawnBoars on map: {map.uniqueID} ({map.Parent})");
            for (int i = 0; i < SpawnCount; i++) {
                IntVec3 spawnPos;
                int attempts = 10;
                do {
                    spawnPos = center + new IntVec3(Rand.Range(-20, 20), 0, Rand.Range(-20, 20));
                    attempts--;
                }
                while (!spawnPos.InBounds(map) && attempts > 0);

                if (!spawnPos.InBounds(map)) continue;

                Pawn boar = PawnGenerator.GeneratePawn(boarKind, Faction.OfAncients);
                GenSpawn.Spawn(boar, spawnPos, map);
            }
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref threatPoints, "threatPoints", 0f);
            Scribe_Values.Look(ref ticksUntilNextSpawn, "ticksUntilNextSpawn", 2500);
            Scribe_Values.Look(ref initialized, "initialized", false);
        }
    }

    public class MapComponent_ForcedWeather : MapComponent {
        public WeatherDef forcedWeather = WeatherDefOf.FoggyRain;

        public MapComponent_ForcedWeather(Map map) : base(map) { }

        public override void MapComponentTick() {
            if (Find.TickManager.TicksGame % 60 == 0) {
                // Дождь только на картах, созданных из нашего сайта
                if (map.Parent is Site site && site.MainSitePartDef == DefDatabase<SitePartDef>.GetNamed("MysticalMarsh_Site")) {
                    if (map.weatherManager.curWeather != forcedWeather)
                        map.weatherManager.TransitionTo(forcedWeather);
                }
            }
        }
    }
}
