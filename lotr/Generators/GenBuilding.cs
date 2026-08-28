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
        }
    }

    public class GenStep_SerpentBirdTower : GenStep {
        public override int SeedPart => 12370;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            TerrainDef ancientTile = TerrainDef.Named("AncientTile");
            ThingDef wallDef = ThingDef.Named("Wall");
            ThingDef stoneBlock = ThingDef.Named("BlocksSandstone");
            if (stoneBlock == null) stoneBlock = ThingDef.Named("BlocksGranite");

            // Размеры башни (примерно 9x9 внутреннего помещения)
            const int innerSize = 7;

            // Пол из древней плитки
            CellRect floorRect = CellRect.CenteredOn(center, innerSize, innerSize);
            foreach (IntVec3 cell in floorRect) {
                if (cell.InBounds(map) && Rand.Value < 0.8f)
                    map.terrainGrid.SetTerrain(cell, ancientTile);
            }

            // Стены по периметру с пропусками (полуразрушенная башня)
            foreach (IntVec3 cell in floorRect) {
                bool isEdge = cell.x == floorRect.minX || cell.x == floorRect.maxX ||
                              cell.z == floorRect.minZ || cell.z == floorRect.maxZ;
                if (!isEdge) continue;

                // Вход с одной стороны (юг), иногда пропускаем стены
                if (cell.z == floorRect.maxZ && cell.x == floorRect.CenterCell.x)
                    continue; // дверной проём

                if (Rand.Value < 0.3f) continue; // разрушенная стена

                if (cell.InBounds(map) && wallDef != null) {
                    Thing wall = ThingMaker.MakeThing(wallDef, stoneBlock);
                    GenSpawn.Spawn(wall, cell, map);
                }
            }

            // Крыша над центром (частично)
            foreach (IntVec3 cell in floorRect) {
                if (cell.InBounds(map) && Rand.Value < 0.5f)
                    map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
            }

            // Немного мусора на полу
            for (int i = 0; i < 3; i++) {
                IntVec3 pos = floorRect.RandomCell;
                if (pos.InBounds(map)) {
                    Thing filth = ThingMaker.MakeThing(ThingDef.Named("Filth_RubbleRock"));
                    GenSpawn.Spawn(filth, pos, map);
                }
            }
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
