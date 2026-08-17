using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 6 - Заговорщик
    public class Hunter6_Hediff : Beyonder_Hediff {

        public Hunter6_Hediff() {
            // способы действия: Заговоры (враги дерутся сами с собой)
            maxProgressPerCategory = 0.8f;

            // анти-действия: Использование грубой силы (blazing spear)
        }
    }
}
