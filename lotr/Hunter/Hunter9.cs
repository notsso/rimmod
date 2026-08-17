using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 9 - Охотник
    public class Hunter9_Hediff : Beyonder_Hediff {
        
        public Hunter9_Hediff() {
            // способы действия: охота
            maxProgressPerCategory = 0.8f;

            // анти-действия: быть жертвой (harmony patch)
        }
    }
}
