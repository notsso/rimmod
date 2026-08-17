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
}
