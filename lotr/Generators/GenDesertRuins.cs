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
    // ===== Генерация террейна пустыни =====
    public class GenStep_DesertTerrain : GenStep {
        public override int SeedPart => 12354;

        public override void Generate(Map map, GenStepParams parms) {
            TerrainDef sand = TerrainDef.Named("Sand");
            TerrainDef softSand = TerrainDef.Named("SoftSand");
            TerrainDef gravel = TerrainDef.Named("Gravel");

            // 1. Заполняем всю карту песком
            foreach (IntVec3 cell in map.AllCells) {
                if (cell.InBounds(map))
                    map.terrainGrid.SetTerrain(cell, sand);
            }

            // 2. Пятна мягкого песка (дюны)
            int softPatches = Rand.Range(10, 20);
            for (int i = 0; i < softPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(4, 10);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && Rand.Value < 0.7f)
                        map.terrainGrid.SetTerrain(cell, softSand);
                }
            }

            // 3. Каменистые участки (гравий)
            int gravelPatches = Rand.Range(15, 25);
            for (int i = 0; i < gravelPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(3, 8);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (cell.InBounds(map) && Rand.Value < 0.6f)
                        map.terrainGrid.SetTerrain(cell, gravel);
                }
            }
        }

        private IntVec3 RandomCell(Map map) {
            return new IntVec3(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
        }
    }

    // ===== Генерация руин и сфинкса =====
    public class GenStep_DesertRuins : GenStep {
        public override int SeedPart => 12355;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;

            TerrainDef ancientTile = TerrainDef.Named("AncientTile");
            ThingDef wallDef = ThingDef.Named("Wall");
            // ThingDef columnDef = ThingDef.Named("Column");
            ThingDef sandstoneBlock = ThingDef.Named("BlocksSandstone");
            if (sandstoneBlock == null) sandstoneBlock = ThingDef.Named("BlocksGranite");

            const int roomSize = 11;      // размер одной секции (как было)
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

            // 2. Внешние и внутренние стены с пропусками (полуразрушенность)
            foreach (IntVec3 cell in ruinsRect) {
                bool isXEdge = cell.x == ruinsRect.minX || cell.x == ruinsRect.maxX;
                bool isZEdge = cell.z == ruinsRect.minZ || cell.z == ruinsRect.maxZ;
                bool isXInner = (cell.x - ruinsRect.minX) % (roomSize + 1) == 0;
                bool isZInner = (cell.z - ruinsRect.minX) % (roomSize + 1) == 0;
                if (!isXInner && !isZInner) continue;

                if (Rand.Value < 0.2f) continue; // Убираем часть стен

                if (cell.InBounds(map) && wallDef != null) {
                    Thing wall = ThingMaker.MakeThing(wallDef, sandstoneBlock);
                    GenSpawn.Spawn(wall, cell, map);
                }
            }


            /*// 3. Колонны на пересечениях между секциями (сетка 4x4)
            for (int ix = 0; ix <= roomsPerSide; ix++) {
                for (int iz = 0; iz <= roomsPerSide; iz++) {
                    IntVec3 pos = new IntVec3(
                        ruinsRect.minX + ix * roomSize,
                        0,
                        ruinsRect.minZ + iz * roomSize
                    );
                    if (pos.InBounds(map) && columnDef != null) {
                        Thing column = ThingMaker.MakeThing(columnDef, sandstoneBlock);
                        GenSpawn.Spawn(column, pos, map);
                    }
                }
            }*/

            // 5. Сфинкс в центре
            PawnKindDef sphinxKind = PawnKindDef.Named("lotr_Sphinx");
            if (sphinxKind != null) {
                Pawn sphinx = PawnGenerator.GeneratePawn(sphinxKind, null);
                StartPermanentManhunter(sphinx);
                GenSpawn.Spawn(sphinx, center, map);
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