using System.Collections.Generic;
using RimWorld;
using Verse;

namespace lotr {
    public class BeyonderHediffDef : HediffDef {
        public int sequence;
        public string pathway;
        public float spiritualityOffset;
        public List<string> newAbilities = new List<string>();
    }

}
