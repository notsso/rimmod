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

    public class GenStep_JungleCabin : GenStep {
        public override int SeedPart => 12353;

        private const int CabinSize = 6; // размер внешней стены (6x6)

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            int half = CabinSize / 2; // 3

            // Пол внутри домика (WoodPlankFloor)
            TerrainDef woodFloor = TerrainDef.Named("WoodPlankFloor");
            ThingDef wallDef = ThingDef.Named("Wall");
            ThingDef woodLog = ThingDef.Named("WoodLog");

            // Мебель
            ThingDef bedDef = ThingDef.Named("Bed");
            ThingDef tableDef = ThingDef.Named("Table2x2c");
            ThingDef stoolDef = ThingDef.Named("Stool");
            ThingDef torchDef = ThingDef.Named("TorchLamp");

            // Прямоугольник домика: от center - half до center + half - 1
            CellRect cabinRect = new CellRect(center.x - half, center.z - half, CabinSize, CabinSize);

            // Укладываем пол
            foreach (IntVec3 cell in cabinRect) {
                if (cell.InBounds(map))
                    map.terrainGrid.SetTerrain(cell, woodFloor);
            }

            // Стены по периметру с пропусками (полуразрушенность)
            foreach (IntVec3 cell in cabinRect) {
                // Определяем, является ли клетка границей
                bool isXEdge = cell.x == cabinRect.minX || cell.x == cabinRect.maxX;
                bool isZEdge = cell.z == cabinRect.minZ || cell.z == cabinRect.maxZ;
                if (!isXEdge && !isZEdge) continue; // не граница

                // Случайно пропускаем 30% стен
                if (Rand.Value < 0.3f) continue;

                if (cell.InBounds(map) && wallDef != null) {
                    Thing wall = ThingMaker.MakeThing(wallDef, woodLog);
                    GenSpawn.Spawn(wall, cell, map);
                }
            }

            // Мебель внутри (например, в углу кровать, стол в центре, стул, факел)
            IntVec3 bedPos = new IntVec3(cabinRect.minX + 1, 0, cabinRect.minZ + 1);
            if (bedPos.InBounds(map) && bedDef != null) {
                Thing bed = ThingMaker.MakeThing(bedDef, woodLog);
                GenSpawn.Spawn(bed, bedPos, map);
            }

            IntVec3 tablePos = new IntVec3(center.x, 0, center.z);
            if (tablePos.InBounds(map) && tableDef != null) {
                Thing table = ThingMaker.MakeThing(tableDef, woodLog);
                GenSpawn.Spawn(table, tablePos, map);
            }

            IntVec3 stoolPos = new IntVec3(tablePos.x + 1, 0, tablePos.z);
            if (stoolPos.InBounds(map) && stoolDef != null) {
                Thing stool = ThingMaker.MakeThing(stoolDef, woodLog);
                GenSpawn.Spawn(stool, stoolPos, map);
            }

            IntVec3 torchPos = new IntVec3(cabinRect.minX + 2, 0, cabinRect.maxZ - 1);
            if (torchPos.InBounds(map) && torchDef != null) {
                Thing torch = ThingMaker.MakeThing(torchDef);
                GenSpawn.Spawn(torch, torchPos, map);
            }
        }
    }
}
