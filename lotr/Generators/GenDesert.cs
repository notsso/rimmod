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
    public class GenStep_DesertTerrain : GenStep {
        public override int SeedPart => 12354;
        public override void Generate(Map map, GenStepParams parms) {
            var sand = TerrainDef.Named("Sand");
            var softSand = TerrainDef.Named("SoftSand");
            var gravel = TerrainDef.Named("Gravel");

            // заливка песком
            foreach (var cell in map.AllCells)
                if (cell.InBounds(map)) map.terrainGrid.SetTerrain(cell, sand);

            // мягкий песок
            GenStepUtility.GenerateTerrainPatches(map, softSand, Rand.Range(10, 20), new IntRange(4, 10), 0.7f);
            // гравий
            GenStepUtility.GenerateTerrainPatches(map, gravel, Rand.Range(15, 25), new IntRange(3, 8), 0.6f);
        }
    }
}