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
    }
}
