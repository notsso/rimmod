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
    // ===== Генерация террейна умеренного леса =====
    public class GenStep_TemperateForestTerrain : GenStep {
        public override int SeedPart => 12356;

        public override void Generate(Map map, GenStepParams parms) {
            TerrainDef soil = TerrainDef.Named("Soil");
            TerrainDef richSoil = TerrainDef.Named("SoilRich");
            TerrainDef gravel = TerrainDef.Named("Gravel");

            // 1. Заполняем почвой только клетки без природного камня и воды
            foreach (IntVec3 cell in map.AllCells) {
                if (!cell.InBounds(map)) continue;
                TerrainDef cur = map.terrainGrid.TerrainAt(cell);
                if (cur.defName.StartsWith("Rough") || cur.defName.Contains("Water") || cur == gravel)
                    continue; // оставляем горы, воду и гравий
                map.terrainGrid.SetTerrain(cell, soil);
            }

            // 2. Пятна плодородной почвы
            int richPatches = Rand.Range(15, 25);
            for (int i = 0; i < richPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(4, 8);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && map.terrainGrid.TerrainAt(cell) == soil && Rand.Value < 0.7f)
                        map.terrainGrid.SetTerrain(cell, richSoil);
                }
            }

            // 3. Каменистые участки
            int gravelPatches = Rand.Range(10, 18);
            for (int i = 0; i < gravelPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(3, 6);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && map.terrainGrid.TerrainAt(cell) == soil && Rand.Value < 0.6f)
                        map.terrainGrid.SetTerrain(cell, gravel);
                }
            }
        }

        private IntVec3 RandomCell(Map map) {
            return new IntVec3(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
        }
    }

    // ===== Генерация растительности =====
    public class GenStep_TemperateForestPlants : GenStep {
        public override int SeedPart => 12357;

        private struct PlantConfig {
            public ThingDef def;
            public float weight; // для выбора с весами
            public FloatRange growthRange;
        }

        public override void Generate(Map map, GenStepParams parms) {
            // Деревья умеренного леса
            var trees = new List<PlantConfig>
            {
                new PlantConfig { def = ThingDef.Named("Plant_TreeOak"), weight = 30, growthRange = new FloatRange(0.6f, 1.0f) },
                new PlantConfig { def = ThingDef.Named("Plant_TreeBirch"), weight = 25, growthRange = new FloatRange(0.6f, 0.9f) },
                new PlantConfig { def = ThingDef.Named("Plant_TreePoplar"), weight = 25, growthRange = new FloatRange(0.6f, 0.9f) },
                new PlantConfig { def = ThingDef.Named("Plant_TreePine"), weight = 20, growthRange = new FloatRange(0.6f, 0.9f) },
            };

            // Кустарники
            var bushes = new List<PlantConfig>
            {
                new PlantConfig { def = ThingDef.Named("Plant_Bush"), weight = 20, growthRange = new FloatRange(0.5f, 0.9f) },
                new PlantConfig { def = ThingDef.Named("Plant_Berry"), weight = 5, growthRange = new FloatRange(0.5f, 0.8f) },
                new PlantConfig { def = ThingDef.Named("Plant_Dandelion"), weight = 30, growthRange = new FloatRange(0.5f, 0.9f) },
            };

            // Наземные растения
            var ground = new List<PlantConfig>
            {
                new PlantConfig { def = ThingDef.Named("Plant_Grass"), weight = 70, growthRange = new FloatRange(0.7f, 1.0f) },
                new PlantConfig { def = ThingDef.Named("Plant_TallGrass"), weight = 30, growthRange = new FloatRange(0.7f, 1.0f) },
            };

            // Плотность (вероятность появления на клетке)
            float treeDensity = 0.15f;
            float bushDensity = 0.10f;
            float groundDensity = 0.30f;

            foreach (IntVec3 cell in map.AllCells) {
                TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
                if (terrain == TerrainDef.Named("WaterShallow")) continue;
                if (terrain == TerrainDef.Named("Gravel")) continue; // на гравии меньше растительности

                PlantConfig? selected = null;

                if (Rand.Value < treeDensity) {
                    selected = PickWeighted(trees);
                } else if (Rand.Value < bushDensity) {
                    selected = PickWeighted(bushes);
                } else if (Rand.Value < groundDensity) {
                    selected = PickWeighted(ground);
                }

                if (selected.HasValue && PlantUtility.CanEverPlantAt(selected.Value.def, cell, map)) {
                    Plant plant = (Plant)GenSpawn.Spawn(selected.Value.def, cell, map);
                    plant.Growth = selected.Value.growthRange.RandomInRange;
                }
            }
        }

        private PlantConfig PickWeighted(List<PlantConfig> list) {
            float totalWeight = list.Sum(c => c.weight);
            float rand = Rand.Value * totalWeight;
            foreach (var config in list) {
                if (rand < config.weight)
                    return config;
                rand -= config.weight;
            }
            return list.Last();
        }
    }

    // ===== Генерация волка в центре =====
    public class GenStep_TemperateForestAnimals : GenStep {
        public override int SeedPart => 12358;

        public override void Generate(Map map, GenStepParams parms) {
            PawnKindDef wolfKind = PawnKindDef.Named("lotr_DemonicWolf");
            if (wolfKind == null) return;

            IntVec3 center = map.Center;
            Pawn wolf = PawnGenerator.GeneratePawn(wolfKind, null);
            StartPermanentManhunter(wolf);
            GenSpawn.Spawn(wolf, center, map);
        }

        private void StartPermanentManhunter(Pawn pawn) {
            if (pawn?.mindState?.mentalStateHandler == null) return;
            MentalStateDef manhunter = DefDatabase<MentalStateDef>.GetNamed("Manhunter");
            if (manhunter == null) return;
            pawn.mindState.mentalStateHandler.TryStartMentalState(manhunter, null, true);
        }
    }
}
