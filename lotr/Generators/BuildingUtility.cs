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

    // ========== Параметры ==========
    public class GiantVillageParams {
        public int minHouses = 3;
        public int maxHouses = 6;
        public float streetLength = 25f;
        public int streetWidth = 3;
        public List<GiantBuildingTemplate> buildingTemplates;
        public GiantBuildingTemplate mainBuildingTemplate;
        public PawnKindDef giantKind;
        public float damageFactorForNonMain = 0.5f;
        public string roadTerrainDefName = "PackedDirt";
        public bool useRoads = true;
        public bool usePalace = false;
        public bool fourRoads = false;
        public float palaceDistanceToRoads = 1f;
    }

    // ========== Шаблоны зданий ==========
    public abstract class GiantBuildingTemplate {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract ThingDef WallStuff { get; }
        public abstract TerrainDef FloorTerrain { get; }
        public abstract List<FurniturePlacement> Furniture { get; }
        public abstract bool IsPalace { get; } // если true, используем специальную генерацию

        // Качество 0..1 (1 = целое, 0 = руины)
        public void Generate(Map map, IntVec3 center, float quality, bool hasDoor = false) {
            if (IsPalace) {
                GeneratePalace(map, center, quality, hasDoor);
                return;
            }

            CellRect rect = CellRect.CenteredOn(center, Width, Height);

            GenStepUtility.ClearRect(map, rect);

            // Пол
            foreach (IntVec3 c in rect) {
                if (!Rand.Chance(quality)) continue;

                if (c.InBounds(map)) {
                    map.terrainGrid.SetTerrain(c, FloorTerrain);
                }
            }

            // Стены
            foreach (IntVec3 c in rect) {
                if (!Rand.Chance(quality)) continue;

                bool isEdge = c.x == rect.minX || c.x == rect.maxX || c.z == rect.minZ || c.z == rect.maxZ;
                if (!isEdge) continue;

                // Вход
                if (c.z == rect.maxZ && c.x == rect.CenterCell.x) {
                    if (hasDoor && quality > 0.5f) {
                        Thing door = ThingMaker.MakeThing(ThingDef.Named("Door"), WallStuff);
                        GenSpawn.Spawn(door, c, map);
                    }
                    continue;
                }

                if (c.InBounds(map)) {
                    Thing wall = ThingMaker.MakeThing(ThingDefOf.Wall, WallStuff);
                    GenSpawn.Spawn(wall, c, map);
                }
            }

            // Крыша
            if (quality > 0.5f) {
                foreach (IntVec3 c in rect) {
                    if (c.InBounds(map))
                        map.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
                }
            }

            // Мебель
            foreach (var furniture in Furniture) {
                if (Rand.Value > quality) continue;

                IntVec3 pos = new IntVec3(rect.minX + furniture.offsetX, 0, rect.minZ + furniture.offsetZ);
                if (!pos.InBounds(map)) continue;
                if (map.thingGrid.ThingsListAt(pos).Any(t => t.def == ThingDefOf.Wall || t.def == ThingDef.Named("Door"))) continue;

                if (furniture.defName == "TorchLamp") {
                    GenSpawn.Spawn(ThingMaker.MakeThing(ThingDef.Named(furniture.defName)), pos, map);
                } else {
                    Thing thing = ThingMaker.MakeThing(ThingDef.Named(furniture.defName), WallStuff);
                    GenSpawn.Spawn(thing, pos, map);
                }
            }
        }

        // Генерация дворца (по образцу GenStep_DesertRuins)
        private void GeneratePalace(Map map, IntVec3 center, float quality, bool hasDoor) {
            TerrainDef ancientTile = TerrainDef.Named("AncientTile");
            ThingDef wallDef = ThingDef.Named("Wall");
            ThingDef sandstoneBlock = ThingDef.Named("BlocksSandstone");
            if (sandstoneBlock == null) sandstoneBlock = ThingDef.Named("BlocksGranite");

            const int roomSize = 11;      // размер одной секции 
            const int roomsPerSide = 3;   // 3x3
            int totalSize = (roomSize + 1) * roomsPerSide + 1; // 37
            int half = totalSize / 2;     // 18

            CellRect palaceRect = new CellRect(center.x - half, center.z - half, totalSize, totalSize);

            GenStepUtility.ClearRect(map, palaceRect);

            // 1. Пол из древней плитки
            foreach (IntVec3 cell in palaceRect) {
                if (Rand.Value < 0.2f) continue; // Убираем часть клеток пола

                if (cell.InBounds(map))
                    map.terrainGrid.SetTerrain(cell, ancientTile);
            }

            // 2. Внешние и внутренние стены
            foreach (IntVec3 cell in palaceRect) {
                bool isXInner = (cell.x - palaceRect.minX) % (roomSize + 1) == 0;
                bool isZInner = (cell.z - palaceRect.minZ) % (roomSize + 1) == 0;
                if (!isXInner && !isZInner) continue;

                if (Math.Abs(cell.z - center.z) <= 1) continue;
                if (Math.Abs(cell.x - center.x) <= 1) continue;

                if (Math.Abs(cell.z - (center.z + roomSize + 1)) <= 1 && Math.Abs(cell.x - center.x) < roomSize) continue;
                if (Math.Abs(cell.z - (center.z - roomSize - 1)) <= 1 && Math.Abs(cell.x - center.x) < roomSize) continue;
                if (Math.Abs(cell.x - (center.x + roomSize + 1)) <= 1 && Math.Abs(cell.z - center.z) < roomSize) continue;
                if (Math.Abs(cell.x - (center.x - roomSize - 1)) <= 1 && Math.Abs(cell.z - center.z) < roomSize) continue;

                if (Rand.Value < 0.2f) continue; // Убираем часть стен

                if (cell.InBounds(map) && wallDef != null) {
                    Thing wall = ThingMaker.MakeThing(wallDef, sandstoneBlock);
                    GenSpawn.Spawn(wall, cell, map);
                }
            }
        }
    }

    public class FurniturePlacement {
        public int offsetX;
        public int offsetZ;
        public string defName;
    }

    // ========== Конкретные шаблоны ==========
    public class GiantHouseTemplate_SmallWood : GiantBuildingTemplate {
        public override int Width => 5;
        public override int Height => 5;
        public override ThingDef WallStuff => ThingDefOf.WoodLog;
        public override TerrainDef FloorTerrain => TerrainDefOf.WoodPlankFloor;
        public override bool IsPalace => false;
        public override List<FurniturePlacement> Furniture => new List<FurniturePlacement>
        {
            new FurniturePlacement { offsetX = 1, offsetZ = 1, defName = "Bed" }
        };
    }

    public class GiantHouseTemplate_MediumWood : GiantBuildingTemplate {
        public override int Width => 7;
        public override int Height => 7;
        public override ThingDef WallStuff => ThingDefOf.WoodLog;
        public override TerrainDef FloorTerrain => TerrainDefOf.WoodPlankFloor;
        public override bool IsPalace => false;
        public override List<FurniturePlacement> Furniture => new List<FurniturePlacement>
        {
            new FurniturePlacement { offsetX = 1, offsetZ = 1, defName = "Bed" },
            new FurniturePlacement { offsetX = 3, offsetZ = 3, defName = "Table2x2c" },
            new FurniturePlacement { offsetX = 4, offsetZ = 4, defName = "Stool" }
        };
    }

    public class GiantHouseTemplate_Stone : GiantBuildingTemplate {
        public override int Width => 7;
        public override int Height => 7;
        public override ThingDef WallStuff => ThingDef.Named("BlocksSandstone");
        public override TerrainDef FloorTerrain => TerrainDef.Named("TileSandstone");
        public override bool IsPalace => false;
        public override List<FurniturePlacement> Furniture => new List<FurniturePlacement>
        {
            new FurniturePlacement { offsetX = 1, offsetZ = 1, defName = "Bed" },
            new FurniturePlacement { offsetX = 3, offsetZ = 3, defName = "Table2x2c" },
            new FurniturePlacement { offsetX = 4, offsetZ = 4, defName = "Stool" }
        };
    }

    public class GiantHouseTemplate_Ancient : GiantBuildingTemplate {
        public override int Width => 9;
        public override int Height => 9;
        public override ThingDef WallStuff => ThingDef.Named("BlocksGranite");
        public override TerrainDef FloorTerrain => TerrainDef.Named("AncientTile");
        public override bool IsPalace => false;
        public override List<FurniturePlacement> Furniture => new List<FurniturePlacement>
        {
            new FurniturePlacement { offsetX = 1, offsetZ = 1, defName = "Bed" },
            new FurniturePlacement { offsetX = 3, offsetZ = 3, defName = "Table2x2c" },
            new FurniturePlacement { offsetX = 4, offsetZ = 4, defName = "Stool" },
            new FurniturePlacement { offsetX = 6, offsetZ = 6, defName = "Column" }
        };
    }

    public class GiantPalaceTemplate_Small : GiantBuildingTemplate {
        public override int Width => 37;
        public override int Height => 37;
        public override ThingDef WallStuff => ThingDef.Named("BlocksGranite");
        public override TerrainDef FloorTerrain => TerrainDef.Named("TileGranite");
        public override bool IsPalace => true;
        public override List<FurniturePlacement> Furniture => new List<FurniturePlacement>
        {
            new FurniturePlacement { offsetX = 1, offsetZ = 1, defName = "Bed" },
            new FurniturePlacement { offsetX = 4, offsetZ = 4, defName = "Table2x2c" },
            new FurniturePlacement { offsetX = 5, offsetZ = 5, defName = "Stool" },
            new FurniturePlacement { offsetX = 2, offsetZ = 7, defName = "Column" },
            new FurniturePlacement { offsetX = 6, offsetZ = 7, defName = "Column" }
        };
    }

    public class GiantPalaceTemplate_Large : GiantBuildingTemplate {
        public override int Width => 37;
        public override int Height => 37;
        public override ThingDef WallStuff => ThingDef.Named("BlocksSlate");
        public override TerrainDef FloorTerrain => TerrainDef.Named("AncientTile");
        public override bool IsPalace => true;
        public override List<FurniturePlacement> Furniture => new List<FurniturePlacement> { };
    }

    // ========== Основной генератор ==========
    public static class GiantVillageGenerator {
        public static void GenerateVillage(Map map, GiantVillageParams param) {
            if (param.buildingTemplates == null || param.buildingTemplates.Count == 0) return;

            IntVec3 center = map.Center;
            int targetCount = Rand.Range(param.minHouses, param.maxHouses + 1);
            if (targetCount <= 0) return;

            // Выбираем главное здание
            GiantBuildingTemplate mainTemplate = param.mainBuildingTemplate ?? param.buildingTemplates.OrderByDescending(t => t.Width * t.Height).First();

            if (param.usePalace && param.fourRoads) {
                GeneratePalaceWithFourRoads(map, param, center, mainTemplate, targetCount);
            } else if (param.usePalace) {
                GeneratePalaceWithHouses(map, param, center, mainTemplate, targetCount);
            } else {
                GenerateLinearVillage(map, param, center, mainTemplate, targetCount);
            }
        }

        private static void GenerateLinearVillage(Map map, GiantVillageParams param, IntVec3 center, GiantBuildingTemplate mainTemplate, int targetCount) {
            // Главный дом: с севера от дороги, в центре
            int roadHalfWidth = param.streetWidth / 2;

            int spacingFromRoad = 1;

            IntVec3 mainPos = new IntVec3(center.x, 0, center.z - (mainTemplate.Height / 2 + roadHalfWidth + spacingFromRoad));
            mainTemplate.Generate(map, mainPos, 1, true);

            // Дорога
            if (param.useRoads) {
                int roadWidth = param.streetWidth;
                int roadLength = (int)param.streetLength;
                CellRect roadRect = new CellRect(center.x - roadLength / 2, center.z - roadWidth / 2, roadLength, roadWidth);
                GenStepUtility.GenerateRoad(map, roadRect, center, param.roadTerrainDefName);
            }

            // Дома: северная и южная стороны
            List<IntVec3> positions = new List<IntVec3>();
            List<GiantBuildingTemplate> templates = new List<GiantBuildingTemplate>();

            // Северная сторона: слева и справа от главного
            int northCount = targetCount / 2;
            int southCount = targetCount - northCount - 1; // минус главный

            // Размещаем на север
            float currentX = mainPos.x - mainTemplate.Width - 2;
            for (int i = 0; i < northCount / 2; i++) {
                GiantBuildingTemplate t = param.buildingTemplates.RandomElement();
                IntVec3 pos = new IntVec3(Mathf.RoundToInt(currentX - t.Width / 2f), 0, mainPos.z);
                positions.Add(pos);
                templates.Add(t);
                currentX -= t.Width + 2;
            }
            currentX = mainPos.x + mainTemplate.Width + 2;
            for (int i = 0; i < northCount - northCount / 2; i++) {
                GiantBuildingTemplate t = param.buildingTemplates.RandomElement();
                IntVec3 pos = new IntVec3(Mathf.RoundToInt(currentX + t.Width / 2f), 0, mainPos.z);
                positions.Add(pos);
                templates.Add(t);
                currentX += t.Width + 2;
            }

            // Южная сторона
            int southStartZ = center.z + roadHalfWidth + spacingFromRoad;
            int southPlaced = 0;
            currentX = center.x - Mathf.RoundToInt(param.streetLength / 4f);
            while (southPlaced < southCount && currentX < center.x + param.streetLength / 4f) {
                GiantBuildingTemplate t = param.buildingTemplates.RandomElement();
                IntVec3 pos = new IntVec3(Mathf.RoundToInt(currentX + t.Width / 2f), 0, southStartZ + t.Height / 2);
                // Проверяем, не пересекается ли с уже размещёнными
                bool overlaps = positions.Any(p => new CellRect(p.x - 2, p.z - 2, 4, 4).Overlaps(new CellRect(pos.x - 2, pos.z - 2, 4, 4)));
                if (!overlaps && pos.InBounds(map)) {
                    positions.Add(pos);
                    templates.Add(t);
                    southPlaced++;
                }
                currentX += t.Width + 2;
            }

            // Строим обычные дома с качеством, зависящим от расстояния
            for (int i = 0; i < positions.Count; i++) {
                float dist = positions[i].DistanceTo(center);
                float quality = Mathf.Clamp01(1f - dist / (param.streetLength * 0.7f)) * (1f - param.damageFactorForNonMain) + param.damageFactorForNonMain;
                templates[i].Generate(map, positions[i], quality, false);
            }

            // Спавн гиганта
            if (param.giantKind != null) {
                Pawn giant = PawnGenerator.GeneratePawn(param.giantKind, null);
                if (giant != null)
                    GenSpawn.Spawn(giant, mainPos, map);
            }
        }

        private static void GeneratePalaceWithHouses(Map map, GiantVillageParams param, IntVec3 center, GiantBuildingTemplate palace, int targetCount) {
            // Дворец в центре
            palace.Generate(map, center, 1f, true);

            // Вокруг дворца 8 позиций (основные и диагональные)
            List<IntVec3> positions = new List<IntVec3> {
                center + new IntVec3(0, 0, (int)(palace.Height / 1.5f)), // север
                center + new IntVec3(0, 0, -(int)(palace.Height / 1.5f)), // юг
                center + new IntVec3((int)(palace.Width / 1.5f), 0, 0), // восток
                center + new IntVec3(-(int)(palace.Width / 1.5f), 0, 0), // запад
                center + new IntVec3((int)(palace.Width / 1.5f), 0, (int)(palace.Height / 1.5f)), // северо-восток
                center + new IntVec3(-(int)(palace.Width / 1.5f), 0, (int)(palace.Height / 1.5f)), // северо-запад
                center + new IntVec3((int)(palace.Width / 1.5f), 0, -(int)(palace.Height / 1.5f)), // юго-восток
                center + new IntVec3(-(int)(palace.Width / 1.5f), 0, -(int)(palace.Height / 1.5f)), // юго-запад
            };

            // Перемешиваем
            positions.Shuffle();

            int housesToBuild = Mathf.Min(targetCount - 1, positions.Count);
            for (int i = 0; i < housesToBuild; i++) {
                IntVec3 pos = positions[i];
                if (!pos.InBounds(map)) continue;

                GiantBuildingTemplate t = param.buildingTemplates.RandomElement();
                CellRect rect = CellRect.CenteredOn(pos, t.Width, t.Height);
                float dist = pos.DistanceTo(center);
                t.Generate(map, pos, 0.5f, false);
            }

            // Гигант во дворце
            if (param.giantKind != null) {
                Pawn giant = PawnGenerator.GeneratePawn(param.giantKind, null);
                if (giant != null) GenSpawn.Spawn(giant, center, map);
            }
        }

        private static void GeneratePalaceWithFourRoads(Map map, GiantVillageParams param, IntVec3 center, GiantBuildingTemplate palace, int targetCount) {
            // Дворец
            palace.Generate(map, center, 1f, true);

            int palaceHalfWidth = palace.Width / 2;
            int roadWidth = param.streetWidth;
            int roadLength = Mathf.RoundToInt(param.streetLength);
            string roadDef = param.roadTerrainDefName;

            // Генерируем дороги в 4 стороны
            if (param.useRoads) {
                // Север
                CellRect northRoad = new CellRect(center.x - roadWidth / 2, center.z - palaceHalfWidth - roadLength, roadWidth, roadLength);
                GenStepUtility.GenerateRoad(map, northRoad, center, roadDef, 0.7f, 0.2f);
                // Юг
                CellRect southRoad = new CellRect(center.x - roadWidth / 2, center.z + palaceHalfWidth + 1, roadWidth, roadLength);
                GenStepUtility.GenerateRoad(map, southRoad, center, roadDef, 0.7f, 0.2f);
                // Восток
                CellRect eastRoad = new CellRect(center.x + palaceHalfWidth + 1, center.z - roadWidth / 2, roadLength, roadWidth);
                GenStepUtility.GenerateRoad(map, eastRoad, center, roadDef, 0.7f, 0.2f);
                // Запад
                CellRect westRoad = new CellRect(center.x - palaceHalfWidth - roadLength, center.z - roadWidth / 2, roadLength, roadWidth);
                GenStepUtility.GenerateRoad(map, westRoad, center, roadDef, 0.7f, 0.2f);
            }

            // Дома вдоль дорог
            int housesPerRoad = (targetCount - 1) / 4;
            int remainder = (targetCount - 1) % 4;
            IntVec3[] directions = { IntVec3.North, IntVec3.South, IntVec3.East, IntVec3.West };
            for (int d = 0; d < 4; d++) {
                IntVec3 dir = directions[d];
                int count = housesPerRoad + (d < remainder ? 1 : 0);
                int side = 1;
                for (int h = 0; h < count; h++) {
                    GiantBuildingTemplate t = param.buildingTemplates.RandomElement();
                    // Расстояние от дворца, с учётом дороги и зазора
                    int distanceFromPalace = palaceHalfWidth + roadWidth / 2 + 10 + h * 11;
                    // Смещение вбок от дороги
                    int sideOffset = (t.Height / 2 + roadWidth / 2 + 1) * side;
                    IntVec3 pos = center + dir * distanceFromPalace + dir.RotatedBy(Rot4.West) * sideOffset;
                    side *= -1; // чередуем стороны дороги

                    if (pos.InBounds(map)) {
                        float dist = pos.DistanceTo(center);
                        float quality = Mathf.Clamp01(1f - dist / (param.streetLength * 0.8f));
                        t.Generate(map, pos, quality, false);
                    }
                }
            }

            // Гигант во дворце
            if (param.giantKind != null) {
                Pawn giant = PawnGenerator.GeneratePawn(param.giantKind, null);
                if (giant != null) GenSpawn.Spawn(giant, center, map);
            }
        }
    }
}