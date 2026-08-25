using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 9 - Охотник
    public class Hunter9_Hediff : Beyonder_Hediff {

        public Hunter9_Hediff() {
            // способы действия: охота
            maxProgressPerCategory = 0.8f;

            // анти-действия: быть жертвой (harmony patch)
        }
    }

    // Последовательность 8 - Провокатор
    public class Hunter8_Hediff : Beyonder_Hediff {

        public Hunter8_Hediff() {
            // способы действия: успешная провокация
            maxProgressPerCategory = 0.8f;

            // анти-действия: безуспешная провокация, быть оскорбленным
        }
    }

    // Последовательность 7 - Пиромант
    public class Hunter7_Hediff : Beyonder_Hediff {

        public Hunter7_Hediff() {
            // способы действия: контроль огня (готовка на костре), метание огня (blazing spear on enemy impact)
            maxProgressPerCategory = 0.4f;

            // анти-действия: получение урона от огня
        }
    }

    // Последовательность 6 - Заговорщик
    public class Hunter6_Hediff : Beyonder_Hediff {

        public Hunter6_Hediff() {
            // способы действия: Заговоры (враги дерутся сами с собой)
            maxProgressPerCategory = 0.8f;

            // анти-действия: Использование грубой силы (blazing spear)
        }
    }

    // Последовательность 5 - Жнец
    public class Hunter5_Hediff : Beyonder_Hediff {

        public Hunter5_Hediff() {
            // способы действия: Быстрые убийства (казнь)
            maxProgressPerCategory = 0.8f;

            // анти-действия: Затяжные битвы (казнь провалилась)
        }
    }

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
