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
    public class GenStep_TemperateForestTerrain : GenStep {
        public override int SeedPart => 12356;
        public override void Generate(Map map, GenStepParams parms) {
            var soil = TerrainDef.Named("Soil");
            var richSoil = TerrainDef.Named("SoilRich");
            var gravel = TerrainDef.Named("Gravel");

            foreach (var cell in map.AllCells) {
                if (!cell.InBounds(map)) continue;
                var cur = map.terrainGrid.TerrainAt(cell);
                if (cur.defName.StartsWith("Rough") || cur.defName.Contains("Water") || cur == gravel)
                    continue;
                map.terrainGrid.SetTerrain(cell, soil);
            }

            GenStepUtility.GenerateTerrainPatches(map, richSoil, Rand.Range(15, 25), new IntRange(4, 8), 0.7f);
            GenStepUtility.GenerateTerrainPatches(map, gravel, Rand.Range(10, 18), new IntRange(3, 6), 0.6f);
        }
    }

    public class GenStep_TemperateForestPlants : GenStep {
        public override int SeedPart => 12357;

        public override void Generate(Map map, GenStepParams parms) {
            // Деревья умеренного леса
            var trees = new List<GenStepUtility.PlantWeightConfig>{
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreeOak"), weight = 30, growthRange = new FloatRange(0.6f, 1.0f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreeBirch"), weight = 25, growthRange = new FloatRange(0.6f, 0.9f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreePoplar"), weight = 25, growthRange = new FloatRange(0.6f, 0.9f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreePine"), weight = 20, growthRange = new FloatRange(0.6f, 0.9f) },
            };

            // Кустарники
            var bushes = new List<GenStepUtility.PlantWeightConfig>{
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Bush"), weight = 20, growthRange = new FloatRange(0.5f, 0.9f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Berry"), weight = 5, growthRange = new FloatRange(0.5f, 0.8f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Dandelion"), weight = 30, growthRange = new FloatRange(0.5f, 0.9f) },
            };

            // Наземные растения
            var ground = new List<GenStepUtility.PlantWeightConfig>{
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Grass"), weight = 70, growthRange = new FloatRange(0.7f, 1.0f) },
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TallGrass"), weight = 30, growthRange = new FloatRange(0.7f, 1.0f) },
            };

            GenStepUtility.SpawnWeightedPlants(map, trees, 0.15f);
            GenStepUtility.SpawnWeightedPlants(map, bushes, 0.10f);
            GenStepUtility.SpawnWeightedPlants(map, ground, 0.30f);
        }
    }
}
