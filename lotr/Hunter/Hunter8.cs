using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 8 - Провокатор
    public class Hunter8_Hediff : Beyonder_Hediff {

        public Hunter8_Hediff() {
            // способы действия: успешная провокация
            maxProgressPerCategory = 0.8f;

            // анти-действия: безуспешная провокация, быть оскорбленным
        }
    }

}
