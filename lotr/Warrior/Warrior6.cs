using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 6
    public class Warrior6_Hediff : Beyonder_Hediff {

        public Warrior6_Hediff() {
            // способы действия: Заговоры (враги дерутся сами с собой)
            maxProgressPerCategory = 0.8f;

            // анти-действия: Использование грубой силы (blazing spear)
        }
    }
}
