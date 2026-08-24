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
    public static class GenStepUtility {
        private static IntVec3 RandomCell(Map map) {
            return new IntVec3(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
        }

        /// <summary>
        /// Создаёт на карте случайные пятна заданного террейна.
        /// </summary>
        public static void GenerateTerrainPatches(Map map, TerrainDef terrain, int patchCount, IntRange radiusRange, float fillChance = 0.7f) {
            for (int i = 0; i < patchCount; i++) {
                var center = RandomCell(map);
                var radius = Rand.Range(radiusRange.min, radiusRange.max);
                foreach (var cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && Rand.Value < fillChance)
                        map.terrainGrid.SetTerrain(cell, terrain);
                }
            }
        }

        /// <summary>
        /// Структура для настройки растения с весом.
        /// </summary>
        public struct PlantWeightConfig {
            public ThingDef def;
            public float weight;
            public FloatRange growthRange;
        }

        /// <summary>
        /// Спавнит растения на всей карте с заданной плотностью, выбирая вид по весам.
        /// </summary>
        public static void SpawnWeightedPlants(Map map, List<PlantWeightConfig> configs, float density) {
            if (configs == null || configs.Count == 0) return;

            foreach (var cell in map.AllCells) {
                var terrain = map.terrainGrid.TerrainAt(cell);
                if (terrain == TerrainDefOf.WaterShallow || terrain == TerrainDefOf.Mud) continue;

                if (Rand.Value < density) {
                    var selected = PickWeighted(configs);
                    if (selected.def != null && PlantUtility.CanEverPlantAt(selected.def, cell, map)) {
                        var plant = (Plant)GenSpawn.Spawn(selected.def, cell, map);
                        plant.Growth = selected.growthRange.RandomInRange;
                    }
                }
            }
        }

        private static PlantWeightConfig PickWeighted(List<PlantWeightConfig> list) {
            float total = list.Sum(c => c.weight);
            float rand = Rand.Value * total;
            foreach (var c in list) {
                if (rand < c.weight) return c;
                rand -= c.weight;
            }
            return list.Last();
        }

        /// <summary>
        /// Спавнит группу существ одного вида в случайных позициях вокруг центра.
        /// </summary>
        /// <param name="map">Карта.</param>
        /// <param name="kind">Вид существа.</param>
        /// <param name="count">Количество существ.</param>
        /// <param name="radius">Максимальное смещение по X и Z от центра (квадратная область).</param>
        /// <param name="center">Центр области (по умолчанию map.Center).</param>
        /// <param name="makeManhunter">Делать ли существо перманентным манхантером.</param>
        public static void SpawnPawns(Map map, PawnKindDef kind, int count, float radius, IntVec3? center = null, bool makeManhunter = true) {
            if (kind == null) return;
            if (count <= 0) return;

            var centerPos = center ?? map.Center;

            for (int i = 0; i < count; i++) {
                IntVec3 cell = centerPos + new IntVec3(
                    Rand.Range(-Mathf.RoundToInt(radius), Mathf.RoundToInt(radius)),
                    0,
                    Rand.Range(-Mathf.RoundToInt(radius), Mathf.RoundToInt(radius))
                );
                if (!cell.InBounds(map)) continue;

                var pawn = PawnGenerator.GeneratePawn(kind, null);
                if (makeManhunter)
                    PawnHelper.MakePermanentManhunter(pawn);
                GenSpawn.Spawn(pawn, cell, map);
            }
        }

        // Перегрузка с диапазоном количества
        public static void SpawnPawns(Map map, PawnKindDef kind, IntRange countRange, float radius, IntVec3? center = null, bool makeManhunter = true) {
            int count = Rand.Range(countRange.min, countRange.max + 1);
            SpawnPawns(map, kind, count, radius, center, makeManhunter);
        }

        public static void ClearRect(Map map, CellRect rect) {
            foreach (IntVec3 c in rect) {
                if (!c.InBounds(map)) continue;

                // Удаляем вещи, кроме пешек
                List<Thing> things = map.thingGrid.ThingsListAt(c).ToList();
                for (int i = things.Count - 1; i >= 0; i--) {
                    Thing thing = things[i];
                    if (thing.def.category != ThingCategory.Pawn)
                        thing.Destroy(DestroyMode.Vanish);
                }

                // Убираем горы и неподходящий терраин
                TerrainDef terrain = map.terrainGrid.TerrainAt(c);
                if (terrain != null) {
                    bool canBuild = terrain.affordances.Contains(TerrainAffordanceDefOf.Heavy);
                    if (!canBuild || terrain.defName.Contains("Rough") || terrain.defName.Contains("Rock") || terrain.defName.Contains("Mountain")) {
                        map.terrainGrid.SetTerrain(c, TerrainDefOf.Soil);
                    }
                }

                // Убираем крышу
                map.roofGrid.SetRoof(c, null);
            }

            CellRect fog = rect.ExpandedBy(1);
            Unfog(map, fog);
        }

        public static void Unfog(Map map, CellRect rect) {
            foreach (IntVec3 c in rect) {
                if (!c.InBounds(map)) continue;

                // Снимаем туман
                map.fogGrid.Unfog(c);
            }
        }

        /// <summary>
        /// Генерирует дорогу внутри заданного прямоугольника.
        /// Чем дальше клетка от центра, тем меньше шанс поставить дорожное покрытие.
        /// </summary>
        /// <param name="map">Карта.</param>
        /// <param name="roadRect">Прямоугольник, в котором создаётся дорога.</param>
        /// <param name="center">Центр, относительно которого рассчитывается целостность дороги.</param>
        /// <param name="roadTerrainDefName">DefName террайна дороги.</param>
        /// <param name="intensityFalloff">Скорость уменьшения целостности с расстоянием (0..1). Больше значение — быстрее затухание.</param>
        /// <param name="minChance">Минимальная вероятность установки дороги (даже на максимальном удалении).</param>
        public static void GenerateRoad(Map map, CellRect roadRect, IntVec3 center, string roadTerrainDefName, float intensityFalloff = 0.7f, float minChance = 0.2f) {
            TerrainDef road = TerrainDef.Named(roadTerrainDefName);
            if (road == null) return;

            ClearRect(map, roadRect);

            // Максимальное расстояние от центра до угла прямоугольника (для нормализации)
            float maxDist = Mathf.Max(
                center.DistanceTo(new IntVec3(roadRect.minX, 0, roadRect.minZ)),
                center.DistanceTo(new IntVec3(roadRect.maxX, 0, roadRect.maxZ))
            );
            if (maxDist <= 0f) maxDist = 1f;

            foreach (IntVec3 c in roadRect) {
                if (!c.InBounds(map)) continue;

                float dist = c.DistanceTo(center); // евклидово расстояние
                float normalizedDist = Mathf.Clamp01(dist / maxDist);
                float chance = Mathf.Lerp(1f, minChance, normalizedDist * intensityFalloff);

                if (Rand.Value < chance) {
                    // Проверяем, что поверхность пригодна для дороги (не вода и не глубокая вода)
                    TerrainDef curTerrain = map.terrainGrid.TerrainAt(c);
                    if (curTerrain != null && (curTerrain.defName.Contains("WaterDeep") || curTerrain.defName.Contains("WaterOceanDeep")))
                        continue;

                    map.terrainGrid.SetTerrain(c, road);
                }
            }
        }
    }
}
