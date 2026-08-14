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
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("MysticalMarsh_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            float threatPoints = sitePart.parms.threatPoints;

            var spawner = new MapComponent_PawnSpawner(map);
            spawner.Initialize(threatPoints);

            map.components.Add(spawner);
            map.weatherManager.TransitionTo(WeatherDefOf.FoggyRain);
        }
    }

    public class IncidentWorker_MysticalMarsh : IncidentWorker {
        protected override bool CanFireNowSub(IncidentParms parms) {
            if (!(parms.target is World)) return false;
            return base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            World world = parms.target as World;
            if (world == null) return false;

            SitePartDef sitePart = DefDatabase<SitePartDef>.GetNamed("MysticalMarsh_Site");
            if (sitePart == null) return false;

            int tile = Find.WorldSelector.SelectedTile;
            if (tile < 0 || !Find.WorldGrid.InBounds(tile)) {
                Log.Error("Select a tile on the world map before running this incident.");
                return false;
            }

            float threatPoints = parms.points > 0f ? parms.points : 100f;

            WorldObjectDef siteObjectDef = DefDatabase<WorldObjectDef>.GetNamed("MysticalMarsh_World");
            Site site = (Site)WorldObjectMaker.MakeWorldObject(siteObjectDef);
            site.Tile = tile;

            site.SetFaction(null);

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

    public class MapComponent_PawnSpawner : MapComponent {
        private float threatPoints;
        private int ticksUntilNextSpawn;
        private bool initialized;
        private PawnKindDef pawnKindDef;
        public void SetPawnKind(PawnKindDef kind) => pawnKindDef = kind;

        private int SpawnInterval =>
            Mathf.RoundToInt(Mathf.Lerp(2500, 600, Mathf.Clamp01(threatPoints / 500f)));

        private int SpawnCount =>
            Mathf.RoundToInt(Mathf.Lerp(1, 4, Mathf.Clamp01(threatPoints / 500f)));

        public MapComponent_PawnSpawner(Map map) : base(map) {
            ticksUntilNextSpawn = 2500;
            pawnKindDef = LotrDefOf.lotr_Spirit;
        }

        public void Initialize(float points) {
            threatPoints = points;
            ticksUntilNextSpawn = SpawnInterval;
            initialized = true;
        }

        public override void MapComponentTick() {
            if (!initialized) return;

            ticksUntilNextSpawn--;
            if (ticksUntilNextSpawn <= 0) {
                ticksUntilNextSpawn = SpawnInterval;
                SpawnPawns();
            }
        }

        private void SpawnPawns() {
            if (pawnKindDef == null) return;

            IntVec3 center = map.Center;
            for (int i = 0; i < SpawnCount; i++) {
                IntVec3 spawnPos;
                int attempts = 10;
                do {
                    spawnPos = center + new IntVec3(Rand.Range(-20, 20), 0, Rand.Range(-20, 20));
                    attempts--;
                }
                while (!spawnPos.InBounds(map) && attempts > 0);

                if (!spawnPos.InBounds(map)) continue;

                Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDef, null);

                // Делаем духа агрессивным (манхантер)
                if (pawn.mindState?.mentalStateHandler != null) {
                    MentalStateDef manhunter = DefDatabase<MentalStateDef>.GetNamed("Manhunter");
                    if (manhunter != null) {
                        pawn.mindState.mentalStateHandler.TryStartMentalState(manhunter);
                    }
                }

                GenSpawn.Spawn(pawn, spawnPos, map);
            }
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref threatPoints, "threatPoints", 0f);
            Scribe_Values.Look(ref ticksUntilNextSpawn, "ticksUntilNextSpawn", 2500);
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Defs.Look(ref pawnKindDef, "pawnKindDef");
        }
    }
}