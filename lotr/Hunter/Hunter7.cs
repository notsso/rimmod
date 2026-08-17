using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 7 - Пиромант
    public class Hunter7_Hediff : Beyonder_Hediff {

        public Hunter7_Hediff() {
            // способы действия: контроль огня (готовка на костре), метание огня (blazing spear on enemy impact)
            maxProgressPerCategory = 0.4f;

            // анти-действия: получение урона от огня
        }
    }
}
