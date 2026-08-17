using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 5 - Жнец
    public class Hunter5_Hediff : Beyonder_Hediff {

        public Hunter5_Hediff() {
            // способы действия: Быстрые убийства (казнь)
            maxProgressPerCategory = 0.8f;

            // анти-действия: Затяжные битвы (казнь провалилась)
        }
    }
}
