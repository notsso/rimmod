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
    public class GenStep_JungleTerrain : GenStep {
        public override int SeedPart => 12351;
        public override void Generate(Map map, GenStepParams parms) {
            var soil = TerrainDef.Named("Soil");
            foreach (var cell in map.AllCells)
                if (cell.InBounds(map)) map.terrainGrid.SetTerrain(cell, soil);

            float scale = Mathf.Clamp((map.Size.x * map.Size.z) / 62500f, 0.5f, 3f);

            GenStepUtility.GenerateTerrainPatches(map, TerrainDef.Named("Marsh"), Mathf.RoundToInt(Rand.Range(15, 25) * scale), new IntRange(4, 9), 0.7f);
            GenStepUtility.GenerateTerrainPatches(map, TerrainDef.Named("Mud"), Mathf.RoundToInt(Rand.Range(10, 18) * scale), new IntRange(3, 7), 0.65f);
            GenStepUtility.GenerateTerrainPatches(map, TerrainDef.Named("WaterShallow"), Mathf.RoundToInt(Rand.Range(8, 15) * scale), new IntRange(2, 5), 0.8f);
        }
    }

    public class GenStep_JunglePlants : GenStep {
        public override int SeedPart => 12352;

        public override void Generate(Map map, GenStepParams parms) {
            var trees = new List<GenStepUtility.PlantWeightConfig>{
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreePalm"), weight = 30, growthRange = new FloatRange(0.7f, 1.0f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreeBamboo"), weight = 25, growthRange = new FloatRange(0.7f, 1.0f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreeCecropia"), weight = 20, growthRange = new FloatRange(0.6f, 0.9f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreeTeak"), weight = 15, growthRange = new FloatRange(0.6f, 0.9f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreeWillow"), weight = 10, growthRange = new FloatRange(0.5f, 0.8f) },
            };

            var bushes = new List<GenStepUtility.PlantWeightConfig>{
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Alocasia"), weight = 30, growthRange = new FloatRange(0.6f, 1.0f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Clivia"), weight = 15, growthRange = new FloatRange(0.6f, 1.0f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Rafflesia"), weight = 5, growthRange = new FloatRange(0.5f, 0.8f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Bush"), weight = 30, growthRange = new FloatRange(0.5f, 0.9f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Chokevine"), weight = 10, growthRange = new FloatRange(0.5f, 0.8f) },
            };

            var ground = new List<GenStepUtility.PlantWeightConfig>{
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Grass"), weight = 60, growthRange = new FloatRange(0.7f, 1.0f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TallGrass"), weight = 40, growthRange = new FloatRange(0.7f, 1.0f) },
            };

            GenStepUtility.SpawnWeightedPlants(map, trees, 0.25f);
            GenStepUtility.SpawnWeightedPlants(map, bushes, 0.30f);
            GenStepUtility.SpawnWeightedPlants(map, ground, 0.45f);
        }
    }
}
