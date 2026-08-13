using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 8 - Провокатор
    public class Warrior8_Hediff : Beyonder_Hediff {

        public Warrior8_Hediff() {
            maxProgressPerCategory = 0.8f;
        }
    }
}
