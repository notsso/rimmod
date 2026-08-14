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
    // ===== Генерация террейна джунглей =====
    public class GenStep_JungleTerrain : GenStep {
        public override int SeedPart => 12351;

        public override void Generate(Map map, GenStepParams parms) {
            TerrainDef soil = TerrainDef.Named("Soil");
            TerrainDef mud = TerrainDef.Named("Mud");
            TerrainDef marsh = TerrainDef.Named("Marsh");
            TerrainDef water = TerrainDef.Named("WaterShallow");

            // 1. Заполняем всю карту обычной почвой
            foreach (IntVec3 cell in map.AllCells) {
                if (cell.InBounds(map))
                    map.terrainGrid.SetTerrain(cell, soil);
            }

            // 2. Масштабируем количество пятен от площади карты
            float standardArea = 250f * 250f;
            float currentArea = map.Size.x * map.Size.z;
            float scaleFactor = Mathf.Clamp(currentArea / standardArea, 0.5f, 3f);

            // Болотистые участки (Marsh)
            int marshPatches = Mathf.RoundToInt(Rand.Range(15, 25) * scaleFactor);
            for (int i = 0; i < marshPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(4, 9);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && Rand.Value < 0.7f)
                        map.terrainGrid.SetTerrain(cell, marsh);
                }
            }

            // Грязевые пятна (Mud)
            int mudPatches = Mathf.RoundToInt(Rand.Range(10, 18) * scaleFactor);
            for (int i = 0; i < mudPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(3, 7);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && Rand.Value < 0.65f)
                        map.terrainGrid.SetTerrain(cell, mud);
                }
            }

            // Небольшие лужи (WaterShallow)
            int waterPatches = Mathf.RoundToInt(Rand.Range(8, 15) * scaleFactor);
            for (int i = 0; i < waterPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(2, 5);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && Rand.Value < 0.8f)
                        map.terrainGrid.SetTerrain(cell, water);
                }
            }
        }

        private IntVec3 RandomCell(Map map) {
            return new IntVec3(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
        }
    }

    // ===== Генерация растительности =====
    public class GenStep_JunglePlants : GenStep {
        public override int SeedPart => 12352;

        private struct PlantConfig {
            public ThingDef def;
            public float chance;
            public FloatRange growthRange;
            public int weight;
        }

        public override void Generate(Map map, GenStepParams parms) {
            // Деревья с относительными весами (не шансами, а весами для выбора)
            var trees = new List<PlantConfig>{
                new PlantConfig { def = ThingDef.Named("Plant_TreePalm"), weight = 30, growthRange = new FloatRange(0.7f, 1.0f) },
                new PlantConfig { def = ThingDef.Named("Plant_TreeBamboo"), weight = 25, growthRange = new FloatRange(0.7f, 1.0f) },
                new PlantConfig { def = ThingDef.Named("Plant_TreeCecropia"), weight = 20, growthRange = new FloatRange(0.6f, 0.9f) },
                new PlantConfig { def = ThingDef.Named("Plant_TreeTeak"), weight = 15, growthRange = new FloatRange(0.6f, 0.9f) },
                new PlantConfig { def = ThingDef.Named("Plant_TreeWillow"), weight = 10, growthRange = new FloatRange(0.5f, 0.8f) },
            };

            // Кусты и наземные растения (можно оставить прежними, но также с весами)
            var bushes = new List<PlantConfig>{
                new PlantConfig { def = ThingDef.Named("Plant_Alocasia"), weight = 30, growthRange = new FloatRange(0.6f, 1.0f) },
                new PlantConfig { def = ThingDef.Named("Plant_Clivia"), weight = 15, growthRange = new FloatRange(0.6f, 1.0f) },
                new PlantConfig { def = ThingDef.Named("Plant_Rafflesia"), weight = 5, growthRange = new FloatRange(0.5f, 0.8f) },
                new PlantConfig { def = ThingDef.Named("Plant_Bush"), weight = 30, growthRange = new FloatRange(0.5f, 0.9f) },
                new PlantConfig { def = ThingDef.Named("Plant_Chokevine"), weight = 10, growthRange = new FloatRange(0.5f, 0.8f) },
            };

            var ground = new List<PlantConfig>{
                new PlantConfig { def = ThingDef.Named("Plant_Grass"), weight = 60, growthRange = new FloatRange(0.7f, 1.0f) },
                new PlantConfig { def = ThingDef.Named("Plant_TallGrass"), weight = 40, growthRange = new FloatRange(0.7f, 1.0f) },
            };

            // Общая вероятность появления дерева на клетке (можно настроить, например 0.25 = 25% клеток с деревьями)
            float treeDensity = 0.25f;
            // Общая вероятность появления куста на клетке (если дерева нет)
            float bushDensity = 0.30f;
            // Общая вероятность появления наземного растения (если дерева и куста нет)
            float groundDensity = 0.45f;

            foreach (IntVec3 cell in map.AllCells) {
                TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
                if (terrain == TerrainDef.Named("WaterShallow")) continue;
                if (terrain == TerrainDef.Named("Mud")) continue; // на грязи не растёт

                PlantConfig? selected = null;

                // 1. Пытаемся посадить дерево
                if (Rand.Value < treeDensity) {
                    selected = PickWeighted(trees);
                }
                // 2. Если дерева нет, пробуем куст
                else if (Rand.Value < bushDensity) {
                    selected = PickWeighted(bushes);
                }
                // 3. Иначе наземное растение
                else if (Rand.Value < groundDensity) {
                    selected = PickWeighted(ground);
                }

                if (selected.HasValue && PlantUtility.CanEverPlantAt(selected.Value.def, cell, map)) {
                    Plant plant = (Plant)GenSpawn.Spawn(selected.Value.def, cell, map);
                    plant.Growth = selected.Value.growthRange.RandomInRange;
                }
            }
        }

        // Вспомогательный метод для выбора с весами
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

    // ===== Генерация домика и охотника =====
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

            // Спавним охотника в центре
            PawnKindDef hunterKind = PawnKindDef.Named("lotr_ForestHunter");
            if (hunterKind != null) {
                IntVec3 hunterPos = center;
                if (!hunterPos.InBounds(map)) hunterPos = cabinRect.CenterCell;
                Pawn hunter = PawnGenerator.GeneratePawn(hunterKind, null);
                StartPermanentManhunter(hunter);
                GenSpawn.Spawn(hunter, hunterPos, map);
            }
        }

        private void StartPermanentManhunter(Pawn pawn) {
            if (pawn?.mindState?.mentalStateHandler == null) return;
            MentalStateDef manhunter = DefDatabase<MentalStateDef>.GetNamed("Manhunter");
            if (manhunter == null) return;
            pawn.mindState.mentalStateHandler.TryStartMentalState(manhunter, null, true);
        }
    }
}
