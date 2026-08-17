using System.Collections.Generic;
using System.Linq;
using System;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 4 - Железнокровный рыцарь
    public class Hunter4_Hediff : Beyonder_Hediff {
        public Hunter4_Hediff() {
            // способы действия: -
            maxProgressPerCategory = 1f;

            // анти-действия: -
        }

        private int tickCounter = 0;
        public int checkInterval = 600;
        public float healAmount = 0.1f;
        private const int TicksToRegenPart = 6000;
        private int missingPartRegenTracker = 0;


        public override void Tick() {
            base.Tick();

            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

            tickCounter++;
            if (tickCounter >= checkInterval) {
                tickCounter = 0;
                HealInjuries();
                RegenerateMissingParts();
            }
        }

        private void HealInjuries() {
            List<Hediff_Injury> injuries = new List<Hediff_Injury>();
            pawn.health.hediffSet.GetHediffs(ref injuries);

            foreach (var injury in injuries) {
                if (injury.Severity > 0) {
                    injury.Severity -= healAmount;
                }
            }
        }

        private void RegenerateMissingParts() {
            var missingParts = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
            if (missingParts.Count == 0) {
                missingPartRegenTracker = 0;
                return;
            }

            missingPartRegenTracker += checkInterval;

            if (missingPartRegenTracker >= TicksToRegenPart) {
                missingPartRegenTracker = 0;

                Hediff_MissingPart partToRegen = missingParts[0];
                BodyPartRecord partRecord = partToRegen.Part;

                pawn.health.RemoveHediff(partToRegen);
                pawn.health.RestorePart(partRecord, null, true);

                HediffDef growthDef = DefDatabase<HediffDef>.GetNamed("FragileRegeneratedPart");

                Hediff_BodyPartGrowth injury = (Hediff_BodyPartGrowth)HediffMaker.MakeHediff(growthDef, pawn, partRecord);
                float maxHealth = partRecord.def.GetMaxHealth(pawn);
                injury.Severity = maxHealth - 1f;  // остаётся 1 HP
                pawn.health.AddHediff(injury, partRecord);
                injury.Tended(1f, 1f);

                if (PawnUtility.ShouldSendNotificationAbout(pawn)) {
                    Messages.Message(pawn.LabelShort + " regenerated their " + partRecord.Label + "!", pawn,
                        MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref missingPartRegenTracker, "missingPartRegenTracker", 0);
        }
    }

    public class Hediff_BodyPartGrowth : Hediff_Injury {
        public override void Heal(float amount) { }
        public override float BleedRate => 0f;
        public override float PainOffset => 0f;
    }

}
