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

            // 1. Заливаем всю карту обычной почвой
            foreach (IntVec3 cell in map.AllCells) {
                if (cell.InBounds(map))
                    map.terrainGrid.SetTerrain(cell, soil);
            }

            // 2. Вычисляем коэффициент масштабирования площади
            // Стандартный размер карты: 250x250 = 62500 клеток
            float standardArea = 50f * 50f;
            float currentArea = map.Size.x * map.Size.z;
            float scaleFactor = currentArea / standardArea;

            // Ограничиваем коэффициент разумными пределами (0.5x - 3x)
            scaleFactor = Mathf.Clamp(scaleFactor, 0.5f, 3f);

            // 3. Генерация болотных пятен (Marsh)
            int marshPatches = Mathf.RoundToInt(Rand.Range(25, 40) * scaleFactor);
            for (int i = 0; i < marshPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(6, 14);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && Rand.Value < 0.75f)
                        map.terrainGrid.SetTerrain(cell, marsh);
                }
            }

            // 4. Генерация плодородной почвы (Rich soil)
            int richPatches = Mathf.RoundToInt(Rand.Range(15, 25) * scaleFactor);
            for (int i = 0; i < richPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(5, 10);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && Rand.Value < 0.75f)
                        map.terrainGrid.SetTerrain(cell, richSoil);
                }
            }

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
            // Случайная клетка в пределах всей карты
            return new IntVec3(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
        }
    }

    public class GenStep_CentralSwampPlants : GenStep {
        public override int SeedPart => 12346;

        // Структура описания растения
        private struct PlantConfig {
            public ThingDef def;
            public float chance;        // вероятность появления на клетке
            public FloatRange growthRange; // диапазон роста
        }

        public override void Generate(Map map, GenStepParams parms) {
            // подлесок
            var undergrowth = new List<PlantConfig> {
                // Трава — очень часто
                new PlantConfig { def = ThingDef.Named("Plant_Grass"), chance = 0.8f, growthRange = new FloatRange(0.7f, 1.0f) },
                // Куст — редко
                new PlantConfig { def = ThingDef.Named("Plant_Bush"), chance = 0.10f, growthRange = new FloatRange(0.6f, 1.0f) },
                // Ягодный куст — очень редко
                new PlantConfig { def = ThingDef.Named("Plant_Berry"), chance = 0.04f, growthRange = new FloatRange(0.6f, 1.0f) },
                // Одуванчик (цветы) — умеренно
                new PlantConfig { def = ThingDef.Named("Plant_Dandelion"), chance = 0.15f, growthRange = new FloatRange(0.6f, 1.0f) },
            };

            // деревья
            var trees = new List<PlantConfig> {
                // Берёза — умеренно
                new PlantConfig { def = ThingDef.Named("Plant_TreeBirch"), chance = 0.15f, growthRange = new FloatRange(0.5f, 0.9f) },
                // Пальма — умеренно
                new PlantConfig { def = ThingDef.Named("Plant_TreePalm"), chance = 0.15f, growthRange = new FloatRange(0.5f, 0.9f) },
                // Алоказия — умеренно
                new PlantConfig { def = ThingDef.Named("Plant_Alocasia"), chance = 0.20f, growthRange = new FloatRange(0.5f, 0.9f) },
                // дуб
                new PlantConfig { def = ThingDef.Named("Plant_Alocasia"), chance = 0.15f, growthRange = new FloatRange(0.5f, 0.9f) },
                // хз
                new PlantConfig { def = ThingDef.Named("Plant_TreePoplar"), chance = 0.40f, growthRange = new FloatRange(0.5f, 0.9f) },
                // сосна - игнор
                new PlantConfig { def = ThingDef.Named("Plant_TreePine"), chance = 0f, growthRange = new FloatRange(0.5f, 0.9f) },
            };

            // Обрабатываем всю карту
            foreach (IntVec3 cell in map.AllCells) {
                TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
                if (terrain == TerrainDef.Named("WaterShallow")) continue;

                // Проверяем, нет ли уже растения
                if (cell.GetFirstThing(map, ThingDef.Named("Plant_Grass")) != null) continue; // или более общая проверка

                foreach (var config in undergrowth) {
                    if (Rand.Value < config.chance) {
                        if (PlantUtility.CanEverPlantAt(config.def, cell, map)) {
                            Plant plant = (Plant)GenSpawn.Spawn(config.def, cell, map);
                            plant.Growth = config.growthRange.RandomInRange;
                        }
                    }
                }

                foreach (var config in trees) {
                    if (Rand.Value < config.chance) {
                        if (PlantUtility.CanEverPlantAt(config.def, cell, map)) {
                            Plant plant = (Plant)GenSpawn.Spawn(config.def, cell, map);
                            plant.Growth = config.growthRange.RandomInRange;
                        }
                    }
                }
            }
        }
    }

    public class GenStep_CentralSwampAnimals : GenStep {
        public override int SeedPart => 12347;

        public override void Generate(Map map, GenStepParams parms) {
            PawnKindDef pawnKind = PawnKindDef.Named("lotr_MarshBoar");
            if (pawnKind == null) return;

            int count = Rand.Range(2, 5); // 2,3,4
            for (int i = 0; i < count; i++) {
                IntVec3 cell = map.Center + new IntVec3(Rand.Range(-25, 25), 0, Rand.Range(-25, 25));
                if (!cell.InBounds(map)) continue;
                Pawn pawn = PawnGenerator.GeneratePawn(pawnKind, null);

                StartPermanentManhunter(pawn);

                GenSpawn.Spawn(pawn, cell, map);
            }
        }

        public static void StartPermanentManhunter(Pawn pawn) {
            if (pawn?.mindState?.mentalStateHandler == null) return;

            MentalStateDef manhunter = DefDatabase<MentalStateDef>.GetNamed("Manhunter");
            if (manhunter == null) return;

            pawn.mindState.mentalStateHandler.TryStartMentalState(manhunter, null, true);
        }
    }

    public class GenStep_CentralSwampLair : GenStep {
        public override int SeedPart => 12348;

        // Параметры логова (можно вынести в XML, если захотите)
        private const float LairSizeFraction = 0.15f; // доля от меньшей стороны карты
        private const int MinLairRadius = 12;
        private const int MaxLairRadius = 30;
        private const float MarshRingFraction = 0.8f; // внешнее кольцо болота
        private const float WaterPoolFraction = 0.3f; // центральная вода
        private const float WaterChance = 0.7f;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            int lairRadius = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Min(map.Size.x, map.Size.z) * LairSizeFraction),
                MinLairRadius,
                MaxLairRadius
            );

            // Внешнее кольцо – болото (Marsh)
            int marshRadius = lairRadius;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, marshRadius, true)) {
                if (!cell.InBounds(map)) continue;
                float dist = cell.DistanceTo(center);
                if (dist > lairRadius * MarshRingFraction)
                    map.terrainGrid.SetTerrain(cell, TerrainDef.Named("Marsh"));
            }

            // Основная часть – грязь (Mud)
            int mudRadius = Mathf.RoundToInt(lairRadius * MarshRingFraction);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, mudRadius, true)) {
                if (cell.InBounds(map))
                    map.terrainGrid.SetTerrain(cell, TerrainDef.Named("MarshyTerrain"));
            }

            // Центральные лужи воды (WaterShallow)
            int waterRadius = Mathf.Clamp(Mathf.RoundToInt(lairRadius * WaterPoolFraction), 2, 8);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, waterRadius, true)) {
                if (cell.InBounds(map) && Rand.Value < WaterChance)
                    map.terrainGrid.SetTerrain(cell, TerrainDef.Named("WaterShallow"));
            }
        }
    }
}