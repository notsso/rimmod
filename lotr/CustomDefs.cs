using System.Collections.Generic;
using System.Xml;
using RimWorld;
using Verse;

namespace lotr {

    public class BeyonderHediffDef : HediffDef {
        public int sequence;
        public string pathway;
        public float spiritualityOffset;
        public List<string> newAbilities = new List<string>();
        
    }

    public class DamageBindHediffDef : HediffDef {
        
        public float thisPawnDamageFactor;
        public float targetPawnDamageFactor;

        public float targetPawnArmorPenetrateFactor;

    }

    public class BeyonderAbilityDef : AbilityDef {

        public float spiritualityCost;

    }

}
