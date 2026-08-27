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
            GenStepUtility.SpawnPawns(map, kind, new IntRange(2, 4), 25f);
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

    public class GenStep_DemonThroatHoneyguide : GenStep {
        public override int SeedPart => 12372;

        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_DemonThroatHoneyguide");
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

    public class GenStep_SpiritPactBird : GenStep {
        public override int SeedPart => 12393;
        public override void Generate(Map map, GenStepParams parms) {
            var kind = PawnKindDef.Named("lotr_SpiritPactBird");
            GenStepUtility.SpawnPawns(map, kind, 1, 0f);
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
