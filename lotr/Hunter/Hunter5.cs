using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public class Hunter5_Hediff : Hunter6_Hediff {
        public override float SpiritualityFactor => 15f;

        public Hunter5_Hediff() {
            maxProgressPerCategory = 0.6f;
        }
    }

    // Способность hunter5 (reaper): жнец
    public class Hediff_ReaperState : HediffWithComps {
        public bool isReserved = false;
        public bool isExpended = false;

        public void ExpendCharge() {
            if (isExpended) return;
            isExpended = true;

            // Логируем, что заряд потрачен, но удалим его на следующем тике
            Log.Message($"[ReaperMod] Заряд Жнеца помечен как использованный на пешке {pawn.LabelShort}");
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