using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 7
    public class Warrior7_Hediff : Beyonder_Hediff {

        public Warrior7_Hediff() {
            // способы действия: контроль огня (готовка на костре), метание огня (blazing spear on enemy impact)
            maxProgressPerCategory = 0.4f;

            // анти-действия: получение урона от огня
        }
    }
}
