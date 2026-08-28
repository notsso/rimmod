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
    // Hunter creatures
    public class GenStep_MagmaElf : GenStep {
        public override int SeedPart => 12347;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_MagmaElf");
            GenStepUtility.SpawnPawns(map, kind, new IntRange(2, 4), 5f);
        }
    }

    public class GenStep_Sphinx : GenStep {
        public override int SeedPart => 12359;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_Sphinx");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_DemonicWolf : GenStep {
        public override int SeedPart => 12358;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_DemonicWolf");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_MarshBoar : GenStep {
        public override int SeedPart => 12347;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_MarshBoar");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_BloodRedChestnutGrove : GenStep {
        public override int SeedPart => 12401;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            int clearingRadius = 8;

            // Очищаем область вокруг дерева
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, clearingRadius, true)) {
                if (!cell.InBounds(map)) continue;

                // Удаляем все растения, кроме травы
                var plants = map.thingGrid.ThingsListAt(cell)
                    .Where(t => t.def.category == ThingCategory.Plant && t.def.defName != "Plant_TallGrass" && t.def.defName != "Plant_Grass")
                    .ToList();
                foreach (var plant in plants) {
                    plant.Destroy(DestroyMode.Vanish);
                }

                // Сажаем высокую траву
                if (Rand.Value < 0.6f) {
                    ThingDef tallGrass = ThingDef.Named("Plant_TallGrass");
                    if (PlantUtility.CanEverPlantAt(tallGrass, cell, map)) {
                        Plant grass = (Plant)GenSpawn.Spawn(tallGrass, cell, map);
                        grass.Growth = Rand.Range(0.7f, 1f);
                    }
                }
            }

            ThingDef treeDef = ThingDef.Named("lotr_BloodRedChestnutTree");
            if (treeDef != null) {
                IntVec3 centerCell = center;
                if (!PlantUtility.CanEverPlantAt(treeDef, centerCell, map)) {
                    centerCell = GenRadial.RadialCellsAround(center, 4f, true)
                        .Where(c => PlantUtility.CanEverPlantAt(treeDef, c, map))
                        .FirstOrDefault();
                }
                if (centerCell != default(IntVec3)) {
                    Plant tree = (Plant)GenSpawn.Spawn(treeDef, centerCell, map);
                    tree.Growth = 0.7f;
                }
            }

            // Спавним 2-4 духов
            PawnKindDef spiritKind = PawnKindDef.Named("lotr_Spirit");
            if (spiritKind != null) {
                int count = Rand.RangeInclusive(2, 4);
                GenStepUtility.SpawnPawns(map, spiritKind, count, 12f, map.Center);
            }
        }
    }

    public class GenStep_ForestHunter : GenStep {
        public override int SeedPart => 12360;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_ForestHunter");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_BlackHuntingSpider : GenStep {
        public override int SeedPart => 12360;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_BlackHuntingSpider");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_CuspidsParrot : GenStep {
        public override int SeedPart => 12361;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_CuspidsParrot");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_CorpseLilyMarsh : GenStep {
        public override int SeedPart => 12402;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            ThingDef lilyDef = ThingDef.Named("lotr_CorpseLily");

            // Гарантируем почву вокруг центра, чтобы растения могли вырасти
            int soilRadius = 3;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, soilRadius + 1, true)) {
                if (!cell.InBounds(map)) continue;
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            }

            // Спавним несколько лилий
            if (lilyDef != null) {
                int lilyCount = Rand.RangeInclusive(2, 4);
                for (int i = 0; i < lilyCount; i++) {
                    IntVec3 pos = center + new IntVec3(Rand.Range(-soilRadius, soilRadius), 0, Rand.Range(-soilRadius, soilRadius));
                    if (!pos.InBounds(map)) continue;
                    if (PlantUtility.CanEverPlantAt(lilyDef, pos, map)) {
                        Plant lily = (Plant)GenSpawn.Spawn(lilyDef, pos, map);
                        lily.Growth = Rand.Range(0.6f, 1f);
                    } else {
                        // Резерв: ищем ближайшую подходящую клетку
                        IntVec3 fallback = GenRadial.RadialCellsAround(center, soilRadius, true)
                            .Where(c => PlantUtility.CanEverPlantAt(lilyDef, c, map))
                            .FirstOrDefault();
                        if (fallback != default) {
                            Plant lily = (Plant)GenSpawn.Spawn(lilyDef, fallback, map);
                            lily.Growth = Rand.Range(0.6f, 1f);
                        }
                    }
                }
            }

            // Спавним духов
            PawnKindDef spiritKind = PawnKindDef.Named("lotr_Spirit");
            if (spiritKind != null) {
                int count = Rand.RangeInclusive(3, 5);
                GenStepUtility.SpawnPawns(map, spiritKind, count, 12f, map.Center);
            }
        }
    }

    public class GenStep_FireSalamander : GenStep {
        public override int SeedPart => 12361;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_FireSalamander");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_MagmaGiant : GenStep {
        public override int SeedPart => 12361;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_MagmaGiant");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    // Assassin creatures
    public class GenStep_SerpentMonsterBird : GenStep {
        public override int SeedPart => 12371;

        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_SerpentMonsterBird");
            GenStepUtility.SpawnPawns(map, kind, 1, 5f);
        }
    }

    public class GenStep_ShadowSwampSpiritsAndFlower : GenStep {
        public override int SeedPart => 12397;

        public override void Generate(Map map, GenStepParams parms) {
            var spiritKind = PawnKindDef.Named("lotr_Spirit");
            if (spiritKind != null) {
                int count = Rand.RangeInclusive(2, 4);
                GenStepUtility.SpawnPawns(map, spiritKind, count, 12f, map.Center);
            }

            ThingDef flowerDef = ThingDef.Named("lotr_ShadowPoisonFlower");
            if (flowerDef != null) {
                IntVec3 center = map.Center;
                if (PlantUtility.CanEverPlantAt(flowerDef, center, map)) {
                    Plant flower = (Plant)GenSpawn.Spawn(flowerDef, center, map);
                    flower.Growth = 0.7f;
                } else {
                    IntVec3? cell = GenRadial.RadialCellsAround(center, 5f, true)
                        .Where(c => PlantUtility.CanEverPlantAt(flowerDef, c, map))
                        .Select(c => (IntVec3?)c)
                        .FirstOrDefault();
                    if (cell.HasValue) {
                        Plant flower = (Plant)GenSpawn.Spawn(flowerDef, cell.Value, map);
                        flower.Growth = 0.7f;
                    }
                }
            }
        }
    }

    public class GenStep_DemonThroatHoneyguide : GenStep {
        public override int SeedPart => 12372;

        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_DemonThroatHoneyguide");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_DarkProwler : GenStep {
        public override int SeedPart => 12396;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_DarkProwler");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_AgatePeacock : GenStep {
        public override int SeedPart => 12373;

        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_AgatePeacock");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_AbyssDemonicFish : GenStep {
        public override int SeedPart => 12403;

        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_AbyssDemonicFish");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_Succubus : GenStep {
        public override int SeedPart => 12375;

        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_Succubus");
            GenStepUtility.SpawnPawns(map, kind, 1, 2f);
        }
    }

    public class GenStep_BlackWidowSpider : GenStep {
        public override int SeedPart => 12376;

        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_BlackWidowSpider");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_FlowerFacedBat : GenStep {
        public override int SeedPart => 12378;

        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_FlowerFacedBat");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_TwoTailedBlackSnake : GenStep {
        public override int SeedPart => 12380;

        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_TwoTailedBlackSnake");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_PlagueMotherSerpent : GenStep {
        public override int SeedPart => 12381;

        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_PlagueMotherSerpent");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_SilverHunter : GenStep {
        public override int SeedPart => 12382;

        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_SilverHunter");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    // Bard creatures
    public class GenStep_FireBird : GenStep {
        public override int SeedPart => 12390;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_FireBird");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_SingingSunflowerGlade : GenStep {
        public override int SeedPart => 12398;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            int clearingRadius = 10;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, clearingRadius, true)) {
                if (!cell.InBounds(map)) continue;

                var plants = map.thingGrid.ThingsListAt(cell)
                    .Where(t => t.def.category == ThingCategory.Plant && t.def.defName != "Plant_TallGrass" && t.def.defName != "Plant_Grass")
                    .ToList();
                foreach (var plant in plants) {
                    plant.Destroy(DestroyMode.Vanish);
                }

                if (Rand.Value < 0.7f) {
                    ThingDef tallGrass = ThingDef.Named("Plant_TallGrass");
                    if (PlantUtility.CanEverPlantAt(tallGrass, cell, map)) {
                        Plant grass = (Plant)GenSpawn.Spawn(tallGrass, cell, map);
                        grass.Growth = Rand.Range(0.7f, 1f);
                    }
                }
            }

            // Спавним поющий подсолнух в центре
            ThingDef sunflowerDef = ThingDef.Named("lotr_Plant_SingingSunflower");
            if (sunflowerDef != null) {
                IntVec3 centerCell = center;
                if (!PlantUtility.CanEverPlantAt(sunflowerDef, centerCell, map)) {
                    centerCell = GenRadial.RadialCellsAround(center, 5f, true)
                        .Where(c => PlantUtility.CanEverPlantAt(sunflowerDef, c, map))
                        .FirstOrDefault();
                }
                if (centerCell != default(IntVec3)) {
                    Plant flower = (Plant)GenSpawn.Spawn(sunflowerDef, centerCell, map);
                    flower.Growth = 0.8f;
                }
            }

            // Спавним 2-4 духов вокруг
            var spiritKind = PawnKindDef.Named("lotr_Spirit");
            if (spiritKind != null) {
                int count = Rand.RangeInclusive(2, 4);
                GenStepUtility.SpawnPawns(map, spiritKind, count, 12f, map.Center);
            }
        }
    }

    public class GenStep_MirrorHedgehog : GenStep {
        public override int SeedPart => 12391;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_MirrorHedgehog");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_DawnRooster : GenStep {
        public override int SeedPart => 12392;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_DawnRooster");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_RadianceSpiritPactTreeGrove : GenStep {
        public override int SeedPart => 12399;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            int clearingRadius = 12;

            // Очищаем область вокруг дерева
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, clearingRadius, true)) {
                if (!cell.InBounds(map)) continue;

                // Удаляем все растения, кроме травы
                var plants = map.thingGrid.ThingsListAt(cell)
                    .Where(t => t.def.category == ThingCategory.Plant && t.def.defName != "Plant_TallGrass" && t.def.defName != "Plant_Grass")
                    .ToList();
                foreach (var plant in plants) {
                    plant.Destroy(DestroyMode.Vanish);
                }

                // Сажаем высокую траву
                if (Rand.Value < 0.6f) {
                    ThingDef tallGrass = ThingDef.Named("Plant_TallGrass");
                    if (PlantUtility.CanEverPlantAt(tallGrass, cell, map)) {
                        Plant grass = (Plant)GenSpawn.Spawn(tallGrass, cell, map);
                        grass.Growth = Rand.Range(0.7f, 1f);
                    }
                }
            }

            // Спавним дерево в центре
            ThingDef treeDef = ThingDef.Named("lotr_Plant_RadianceSpiritPactTree");
            if (treeDef != null) {
                IntVec3 centerCell = center;
                if (!PlantUtility.CanEverPlantAt(treeDef, centerCell, map)) {
                    centerCell = GenRadial.RadialCellsAround(center, 8f, true)
                        .Where(c => PlantUtility.CanEverPlantAt(treeDef, c, map))
                        .FirstOrDefault();
                }
                if (centerCell != default(IntVec3)) {
                    Plant tree = (Plant)GenSpawn.Spawn(treeDef, centerCell, map);
                    tree.Growth = 0.7f;
                }
            }

            // Спавним 5-8 духов вокруг
            var spiritKind = PawnKindDef.Named("lotr_Spirit");
            if (spiritKind != null) {
                int count = Rand.RangeInclusive(5, 8);
                GenStepUtility.SpawnPawns(map, spiritKind, count, 12f, map.Center);
            }
        }
    }

    public class GenStep_SpiritPactBird : GenStep {
        public override int SeedPart => 12393;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_SpiritPactBird");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_CrystallizedElderTreeForest : GenStep {
        public override int SeedPart => 12400;

        public override void Generate(Map map, GenStepParams parms) {
            IntVec3 center = map.Center;
            int clearingRadius = 15;

            // Очищаем область вокруг дерева
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, clearingRadius, true)) {
                if (!cell.InBounds(map)) continue;

                // Удаляем все растения, кроме травы
                var plants = map.thingGrid.ThingsListAt(cell)
                    .Where(t => t.def.category == ThingCategory.Plant && t.def.defName != "Plant_TallGrass" && t.def.defName != "Plant_Grass")
                    .ToList();
                foreach (var plant in plants) {
                    plant.Destroy(DestroyMode.Vanish);
                }

                // Сажаем высокую траву
                if (Rand.Value < 0.6f) {
                    ThingDef tallGrass = ThingDef.Named("Plant_TallGrass");
                    if (PlantUtility.CanEverPlantAt(tallGrass, cell, map)) {
                        Plant grass = (Plant)GenSpawn.Spawn(tallGrass, cell, map);
                        grass.Growth = Rand.Range(0.7f, 1f);
                    }
                }
            }

            // Спавним большое дерево в центре (без очистки леса)
            ThingDef treeDef = ThingDef.Named("lotr_Plant_CrystallizedElderTree");
            if (treeDef != null) {
                IntVec3 centerCell = center;
                if (!PlantUtility.CanEverPlantAt(treeDef, centerCell, map)) {
                    centerCell = GenRadial.RadialCellsAround(center, 8f, true)
                        .Where(c => PlantUtility.CanEverPlantAt(treeDef, c, map))
                        .FirstOrDefault();
                }
                if (centerCell != default(IntVec3)) {
                    Plant tree = (Plant)GenSpawn.Spawn(treeDef, centerCell, map);
                    tree.Growth = 0.7f;
                }
            }

            // Спавним 10-15 духов вокруг дерева
            var spiritKind = PawnKindDef.Named("lotr_Spirit");
            if (spiritKind != null) {
                int count = Rand.RangeInclusive(10, 15);
                GenStepUtility.SpawnPawns(map, spiritKind, count, 12f, map.Center);
            }
        }
    }

    public class GenStep_DawnRoosterKing : GenStep {
        public override int SeedPart => 12394;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_DawnRoosterKing");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }

    public class GenStep_SunDivineBird : GenStep {
        public override int SeedPart => 12395;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_SunDivineBird");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
        }
    }
}
