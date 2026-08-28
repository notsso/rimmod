using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

namespace lotr {
    public class GenStep_VolcanicTerrain : GenStep {
        public override int SeedPart => 12349;
        public override void Generate(Map map, GenStepParams parms) {
            var granite = TerrainDef.Named("FlagstoneGranite");
            foreach (var cell in map.AllCells)
                if (cell.InBounds(map)) map.terrainGrid.SetTerrain(cell, granite);

            GenStepUtility.GenerateTerrainPatches(map, TerrainDef.Named("WaterShallow"), Rand.Range(10, 20), new IntRange(4, 10), 0.8f);
            GenStepUtility.GenerateTerrainPatches(map, TerrainDef.Named("Gravel"), Rand.Range(15, 25), new IntRange(3, 8), 0.7f);
        }
    }
}
