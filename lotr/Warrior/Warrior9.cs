using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 9 - Охотник
    public class Warrior9_Hediff : Beyonder_Hediff {
        
        public Warrior9_Hediff() {
            // способы действия: воевать?
            maxProgressPerCategory = 0.8f;
        }
    }
}
