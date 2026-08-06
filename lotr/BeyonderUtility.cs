using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public static class BeyonderUtility {
        // Метод для нанесения урона рассудку
        public static void AddSanityLoss(Pawn pawn, float amount, string reasonMote = null) {
            if (pawn == null || pawn.health == null) return;
            if (amount <= 0f) return;

            HediffDef sanityDef = LotrDefOf.lotr_SanityLoss;
            Hediff sanityHediff = pawn.health.hediffSet.GetFirstHediffOfDef(sanityDef);

            if (sanityHediff != null) {
                sanityHediff.Severity += amount;
            } else {
                sanityHediff = HediffMaker.MakeHediff(sanityDef, pawn);
                sanityHediff.Severity = amount;
                pawn.health.AddHediff(sanityHediff);
            }

            // Показываем всплывающую надпись, если передана
            if (!reasonMote.NullOrEmpty() && pawn.Spawned) {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, reasonMote, 3.5f);
            }
        }

        // Проверяет, является ли пешка "Потусторонним" (любого уровня)
        public static bool IsBeyonder(Pawn pawn) {
            if (pawn?.health?.hediffSet?.hediffs == null) return false;
            foreach (var hediff in pawn.health.hediffSet.hediffs) {
                if (hediff is Beyonder_Hediff) return true;
            }
            return false;
        }
    }
}
