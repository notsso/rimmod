using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 5
    public class Warrior5_Hediff : Beyonder_Hediff {
        public Warrior5_Hediff() {
            // способы действия: Быстрые убийства (казнь)
            maxProgressPerCategory = 0.8f;

            // анти-действия: Затяжные битвы (казнь провалилась)
        }
    }
}
