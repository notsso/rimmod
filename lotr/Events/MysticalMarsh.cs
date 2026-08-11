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
