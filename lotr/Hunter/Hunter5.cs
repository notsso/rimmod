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

    // Способность hunter5 (reaper): жнец
    public class Hediff_ReaperState : HediffWithComps {
        public bool isReserved = false;
        public bool isExpended = false;

        public void ExpendCharge() {
            if (isExpended) return;
            isExpended = true;
        }

        // Автоматически удаляем хедифф в конце кадра/тика, когда все фазы урона прошли
        public override void Tick() {
            base.Tick();
            if (isExpended) {
                pawn.health.RemoveHediff(this);
            }
        }
    }

    // Способность hunter5 (reaper): уязвимость 
    public class Hediff_Vulnerable : HediffWithComps { }
}
