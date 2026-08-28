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
    public class GenStep_CentralSwampTerrain : GenStep {
        public override int SeedPart => 12345;

        public override void Generate(Map map, GenStepParams parms) {
            TerrainDef soil = TerrainDef.Named("Soil");
            TerrainDef marsh = TerrainDef.Named("Marsh");
            TerrainDef richSoil = TerrainDef.Named("SoilRich");
            TerrainDef mud = TerrainDef.Named("Mud");
            TerrainDef water = TerrainDef.Named("WaterShallow");

            foreach (IntVec3 cell in map.AllCells) {
                if (cell.InBounds(map))
                    map.terrainGrid.SetTerrain(cell, soil);
            }

            float standardArea = 50f * 50f;
            float currentArea = map.Size.x * map.Size.z;
            float scaleFactor = currentArea / standardArea;

            // Ограничиваем коэффициент разумными пределами (0.5x - 3x)
            scaleFactor = Mathf.Clamp(scaleFactor, 0.5f, 3f);

            GenStepUtility.GenerateTerrainPatches(map, marsh, Mathf.RoundToInt(Rand.Range(25, 40) * scaleFactor), new IntRange(6, 14), 0.75f);
            GenStepUtility.GenerateTerrainPatches(map, richSoil, Mathf.RoundToInt(Rand.Range(15, 25) * scaleFactor), new IntRange(5, 10), 0.75f);

            // 5. Генерация грязевых луж с водой (Mud + Water)
            int mudPatches = Mathf.RoundToInt(Rand.Range(20, 30) * scaleFactor);
            for (int i = 0; i < mudPatches; i++) {
                IntVec3 center = RandomCell(map);
                int outerRadius = Rand.Range(8, 15);
                int waterRadius = Rand.Range(2, 4);

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
            return new IntVec3(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
        }
    }

    public class GenStep_CentralSwampPlants : GenStep {
        public override int SeedPart => 12346;

        public override void Generate(Map map, GenStepParams parms) {
            // подлесок
            var undergrowth = new List<GenStepUtility.PlantWeightConfig> {
                // Трава — очень часто
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Grass"), weight = 0.8f, growthRange = new FloatRange(0.7f, 1.0f) },
                // Куст — редко
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Bush"), weight = 0.10f, growthRange = new FloatRange(0.6f, 1.0f) },
                // Ягодный куст — очень редко
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Berry"), weight = 0.04f, growthRange = new FloatRange(0.6f, 1.0f) },
                // Одуванчик (цветы) — умеренно
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Dandelion"), weight = 0.15f, growthRange = new FloatRange(0.6f, 1.0f) },
            };

            // деревья
            var trees = new List<GenStepUtility.PlantWeightConfig> {
                // Берёза — умеренно
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreeBirch"), weight = 0.15f, growthRange = new FloatRange(0.5f, 0.9f) },
                // Пальма — умеренно
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreePalm"), weight = 0.15f, growthRange = new FloatRange(0.5f, 0.9f) },
                // Алоказия — умеренно
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Alocasia"), weight = 0.20f, growthRange = new FloatRange(0.5f, 0.9f) },
                // дуб
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_Alocasia"), weight = 0.15f, growthRange = new FloatRange(0.5f, 0.9f) },
                // хз
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreePoplar"), weight = 0.40f, growthRange = new FloatRange(0.5f, 0.9f) },
                // сосна - игнор
                new GenStepUtility.PlantWeightConfig { def = ThingDef.Named("Plant_TreePine"), weight = 0f, growthRange = new FloatRange(0.5f, 0.9f) },
            };

            GenStepUtility.SpawnWeightedPlants(map, undergrowth, 0.5f);
            GenStepUtility.SpawnWeightedPlants(map, trees, 0.5f);
        }
    }
}
