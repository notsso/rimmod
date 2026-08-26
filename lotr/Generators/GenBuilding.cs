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
    public class GenStep_SuccubusTemple : GenStep {
        public override int SeedPart => 12374;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            TerrainDef ancientTile = TerrainDef.Named("AncientTile");
            ThingDef wallDef = ThingDef.Named("Wall");
            ThingDef stoneBlock = ThingDef.Named("BlocksSandstone");
            if (stoneBlock == null) stoneBlock = ThingDef.Named("BlocksGranite");

            // Размеры храма (примерно 15x15)
            const int size = 15;
            CellRect templeRect = CellRect.CenteredOn(center, size, size);

            // Пол из древней плитки
            foreach (IntVec3 cell in templeRect) {
                if (cell.InBounds(map) && Rand.Value < 0.85f)
                    map.terrainGrid.SetTerrain(cell, ancientTile);
            }

            // Стены по периметру с пропусками
            foreach (IntVec3 cell in templeRect) {
                bool isEdge = cell.x == templeRect.minX || cell.x == templeRect.maxX ||
                              cell.z == templeRect.minZ || cell.z == templeRect.maxZ;
                if (!isEdge) continue;

                // Вход с юга
                if (cell.z == templeRect.maxZ && cell.x == center.x)
                    continue;

                if (Rand.Value < 0.25f) continue; // разрушены

                if (cell.InBounds(map) && wallDef != null) {
                    Thing wall = ThingMaker.MakeThing(wallDef, stoneBlock);
                    GenSpawn.Spawn(wall, cell, map);
                }
            }

            // Крыша
            foreach (IntVec3 cell in templeRect) {
                if (cell.InBounds(map) && Rand.Value < 0.6f)
                    map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
            }

            // Колонны внутри (4 штуки по углам центральной части)
            IntVec3[] columns = {
                center + new IntVec3(-2, 0, -2),
                center + new IntVec3(2, 0, -2),
                center + new IntVec3(-2, 0, 2),
                center + new IntVec3(2, 0, 2)
            };
            ThingDef columnDef = ThingDef.Named("Column");
            foreach (IntVec3 pos in columns) {
                if (pos.InBounds(map) && columnDef != null) {
                    Thing column = ThingMaker.MakeThing(columnDef, stoneBlock);
                    GenSpawn.Spawn(column, pos, map);
                }
            }

            // Алтарь в центре (можно использовать стул или стол)
            ThingDef altarDef = ThingDef.Named("Table2x2c");
            if (altarDef != null) {
                Thing altar = ThingMaker.MakeThing(altarDef, stoneBlock);
                GenSpawn.Spawn(altar, center, map);
            }
        }
    }

    public class GenStep_FlowerFacedBatCave : GenStep {
        public override int SeedPart => 12377;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            TerrainDef ancientTile = TerrainDef.Named("AncientTile");
            ThingDef wallDef = ThingDef.Named("Wall");
            ThingDef stoneBlock = ThingDef.Named("BlocksGranite");
            if (stoneBlock == null) stoneBlock = ThingDef.Named("BlocksSandstone");

            // Размер пещеры (примерно 11x11)
            const int size = 11;
            CellRect caveRect = CellRect.CenteredOn(center, size, size);

            // Пол из древней плитки (неровный)
            foreach (IntVec3 cell in caveRect) {
                if (cell.InBounds(map) && Rand.Value < 0.7f)
                    map.terrainGrid.SetTerrain(cell, ancientTile);
            }

            // Стены по периметру с пропусками
            foreach (IntVec3 cell in caveRect) {
                bool isEdge = cell.x == caveRect.minX || cell.x == caveRect.maxX ||
                              cell.z == caveRect.minZ || cell.z == caveRect.maxZ;
                if (!isEdge) continue;

                // Вход с севера
                if (cell.z == caveRect.minZ && cell.x == center.x)
                    continue;

                if (Rand.Value < 0.3f) continue; // частично обрушены

                if (cell.InBounds(map) && wallDef != null) {
                    Thing wall = ThingMaker.MakeThing(wallDef, stoneBlock);
                    GenSpawn.Spawn(wall, cell, map);
                }
            }

            // Крыша (в основном целая)
            foreach (IntVec3 cell in caveRect) {
                if (cell.InBounds(map) && Rand.Value < 0.8f)
                    map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
            }

            // Несколько сталактитоподобных колонн
            ThingDef columnDef = ThingDef.Named("Column");
            for (int i = 0; i < 3; i++) {
                IntVec3 pos = caveRect.RandomCell;
                if (pos.InBounds(map) && columnDef != null) {
                    Thing column = ThingMaker.MakeThing(columnDef, stoneBlock);
                    GenSpawn.Spawn(column, pos, map);
                }
            }
        }
    }

    public class GenStep_TwoTailedSnakeHollow : GenStep {
        public override int SeedPart => 12379;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            TerrainDef soil = TerrainDef.Named("Soil");

            // Создаём участок с высокой травой и рыхлой землёй
            int radius = 8;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                if (!cell.InBounds(map)) continue;

                // Убираем деревья и кусты, оставляем только траву
                List<Thing> things = map.thingGrid.ThingsListAt(cell).ToList();
                for (int i = things.Count - 1; i >= 0; i--) {
                    Thing t = things[i];
                    if (t.def.category == ThingCategory.Plant && t.def.defName != "Plant_TallGrass")
                        t.Destroy(DestroyMode.Vanish);
                }

                // Сажаем высокую траву с высокой вероятностью
                if (Rand.Value < 0.7f && PlantUtility.CanEverPlantAt(ThingDef.Named("Plant_TallGrass"), cell, map)) {
                    Plant plant = (Plant)GenSpawn.Spawn(ThingDef.Named("Plant_TallGrass"), cell, map);
                    plant.Growth = Rand.Range(0.7f, 1f);
                }

                // Иногда рыхлая земля (Soil) для нор
                if (Rand.Value < 0.3f)
                    map.terrainGrid.SetTerrain(cell, soil);
            }

            // Немного камней вокруг
            ThingDef stoneChunk = ThingDef.Named("ChunkGranite");
            if (stoneChunk == null) stoneChunk = ThingDef.Named("ChunkSandstone");
            for (int i = 0; i < 5; i++) {
                IntVec3 pos = center + new IntVec3(Rand.Range(-radius, radius), 0, Rand.Range(-radius, radius));
                if (pos.InBounds(map)) {
                    Thing chunk = ThingMaker.MakeThing(stoneChunk);
                    GenSpawn.Spawn(chunk, pos, map);
                }
            }
        }
    }
}
