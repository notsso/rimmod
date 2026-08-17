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

    public class GenStep_CentralVolcanicLair : GenStep {
        public override int SeedPart => 12350;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            int lairRadius = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(map.Size.x, map.Size.z) * 0.15f), 12, 30);

            TerrainDef stone = TerrainDef.Named("FlagstoneGranite");
            TerrainDef water = TerrainDef.Named("WaterShallow");
            TerrainDef slate = TerrainDef.Named("FlagstoneSlate");

            // --- 1. Убираем все горы, постройки и потолки внутри логова ---
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, lairRadius, true)) {
                if (!cell.InBounds(map)) continue;

                // Удаляем все вещи (здания и природные скалы)
                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--) {
                    Thing t = things[i];
                    if (t.def.category == ThingCategory.Building || t.def.building?.isNaturalRock == true) {
                        t.Destroy(DestroyMode.Vanish);
                    }
                }

                // Убираем крышу
                map.roofGrid.SetRoof(cell, null);
            }

            // --- 2. Формируем внешнее кольцо воды и внутреннюю каменную площадку ---
            int outerWaterRadius = lairRadius;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, outerWaterRadius, true)) {
                if (!cell.InBounds(map)) continue;
                float dist = cell.DistanceTo(center);
                if (dist > lairRadius * 0.7f)
                    map.terrainGrid.SetTerrain(cell, water);
            }

            int stoneRadius = Mathf.RoundToInt(lairRadius * 0.7f);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, stoneRadius, true)) {
                if (cell.InBounds(map))
                    map.terrainGrid.SetTerrain(cell, stone);
            }

            int gravelRadius = Mathf.Clamp(Mathf.RoundToInt(lairRadius * 0.3f), 3, 8);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, gravelRadius, true)) {
                if (cell.InBounds(map) && Rand.Value < 0.5f)
                    map.terrainGrid.SetTerrain(cell, slate);
            }

            // 3. Строим гору-кольцо из природного камня с крышей над всей областью
            int outerRing = 12;
            int innerRing = 8;
            RoofDef thickRoof = RoofDefOf.RoofRockThick;

            for (int dx = -outerRing; dx <= outerRing; dx++) {
                for (int dz = -outerRing; dz <= outerRing; dz++) {
                    IntVec3 pos = center + new IntVec3(dx, 0, dz);
                    if (!pos.InBounds(map)) continue;

                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist > outerRing) continue;

                    // Ставим крышу над всей горой, включая проходы и внутренний круг
                    map.roofGrid.SetRoof(pos, thickRoof);

                    // Проверяем проходы (шириной 3 клетки по осям)
                    bool isPassage = false;
                    if (Mathf.Abs(dx) <= 1 && dz > 0) isPassage = true;
                    else if (Mathf.Abs(dx) <= 1 && dz < 0) isPassage = true;
                    else if (Mathf.Abs(dz) <= 1 && dx > 0) isPassage = true;
                    else if (Mathf.Abs(dz) <= 1 && dx < 0) isPassage = true;

                    // Камень ставим только в кольце, не в проходах и не во внутреннем круге
                    if (dist >= innerRing && dist <= outerRing && !isPassage) {
                        ThingDef rockDef = GenStep_RocksFromGrid.RockDefAt(pos);
                        if (rockDef != null) {
                            Thing rock = ThingMaker.MakeThing(rockDef);
                            GenSpawn.Spawn(rock, pos, map);
                        }
                    }
                }
            }
        }
    }
}
