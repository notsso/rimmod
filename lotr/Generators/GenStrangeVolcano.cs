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
            TerrainDef granite = TerrainDef.Named("FlagstoneGranite");
            TerrainDef water = TerrainDef.Named("WaterShallow"); // лава пока вода
            TerrainDef gravel = TerrainDef.Named("Gravel");

            // 1. Заменяем все открытые клетки на камень, оставляя горные породы и существующую воду
            foreach (IntVec3 cell in map.AllCells) {
                if (!cell.InBounds(map)) continue;
                map.terrainGrid.SetTerrain(cell, granite);
            }

            // 2. Лавовые озёра (пока вода)
            int waterPatches = Rand.Range(10, 20);
            for (int i = 0; i < waterPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(4, 10);
                foreach (IntVec3 c in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (c.InBounds(map) && Rand.Value < 0.8f)
                        map.terrainGrid.SetTerrain(c, water);
                }
            }

            // 3. Пятна гравия для разнообразия
            int gravelPatches = Rand.Range(15, 25);
            for (int i = 0; i < gravelPatches; i++) {
                IntVec3 center = RandomCell(map);
                int radius = Rand.Range(3, 8);
                foreach (IntVec3 c in GenRadial.RadialCellsAround(center, radius, true)) {
                    if (c.InBounds(map) && Rand.Value < 0.7f)
                        map.terrainGrid.SetTerrain(c, gravel);
                }
            }
        }

        private IntVec3 RandomCell(Map map) {
            return new IntVec3(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
        }
    }

    public class GenStep_CentralVolcanicAnimals : GenStep {
        public override int SeedPart => 12347;

        public override void Generate(Map map, GenStepParams parms) {
            PawnKindDef pawnKind = PawnKindDef.Named("lotr_MagmaElf");
            if (pawnKind == null) return;

            int count = Rand.Range(2, 5);
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
