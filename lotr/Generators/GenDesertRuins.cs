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
    public class GenStep_DesertTerrain : GenStep {
        public override int SeedPart => 12354;
        public override void Generate(Map map, GenStepParams parms) {
            var sand = TerrainDef.Named("Sand");
            var softSand = TerrainDef.Named("SoftSand");
            var gravel = TerrainDef.Named("Gravel");

            // заливка песком
            foreach (var cell in map.AllCells)
                if (cell.InBounds(map)) map.terrainGrid.SetTerrain(cell, sand);

            // мягкий песок
            GenStepUtility.GenerateTerrainPatches(map, softSand, Rand.Range(10, 20), new IntRange(4, 10), 0.7f);
            // гравий
            GenStepUtility.GenerateTerrainPatches(map, gravel, Rand.Range(15, 25), new IntRange(3, 8), 0.6f);
        }
    }

    public class GenStep_DesertRuins : GenStep {
        public override int SeedPart => 12355;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;

            TerrainDef ancientTile = TerrainDef.Named("AncientTile");
            ThingDef wallDef = ThingDef.Named("Wall");
            ThingDef sandstoneBlock = ThingDef.Named("BlocksSandstone");
            if (sandstoneBlock == null) sandstoneBlock = ThingDef.Named("BlocksGranite");

            const int roomSize = 11;      // размер одной секции 
            const int roomsPerSide = 3;   // 3x3
            int totalSize = (roomSize + 1) * roomsPerSide + 1; // 37
            int half = totalSize / 2;     // 18

            CellRect ruinsRect = new CellRect(center.x - half, center.z - half, totalSize, totalSize);

            // 1. Пол из древней плитки
            foreach (IntVec3 cell in ruinsRect) {
                if (Rand.Value < 0.2f) continue; // Убираем часть клеток пола

                if (cell.InBounds(map))
                    map.terrainGrid.SetTerrain(cell, ancientTile);
            }

            // 2. Внешние и внутренние стены
            foreach (IntVec3 cell in ruinsRect) {
                bool isXInner = (cell.x - ruinsRect.minX) % (roomSize + 1) == 0;
                bool isZInner = (cell.z - ruinsRect.minX) % (roomSize + 1) == 0;
                if (!isXInner && !isZInner) continue;

                if (Rand.Value < 0.2f) continue; // Убираем часть стен

                if (cell.InBounds(map) && wallDef != null) {
                    Thing wall = ThingMaker.MakeThing(wallDef, sandstoneBlock);
                    GenSpawn.Spawn(wall, cell, map);
                }
            }
        }
    }
}