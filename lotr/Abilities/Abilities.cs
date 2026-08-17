using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // определение для способности, которая тратит духовность
    public class Ability_SpendSpirituality : Ability {
        public Ability_SpendSpirituality() : base() { }
        public Ability_SpendSpirituality(Pawn pawn) : base(pawn) { }
        public Ability_SpendSpirituality(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        // высчитывает сколько духовности тратит способность 
        public float AbilityCost() {

            float finalCost = 10f;

            finalCost = ((BeyonderAbilityDef)this.def).spiritualityCost;

            return finalCost;
            
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest) {
            bool result = base.Activate(target, dest);

            if (result) {
                Pawn caster = this.pawn;
                SpiritualityUtility.ConsumeSpirituality(caster, AbilityCost());
            }

            return result;
        }

        // определяет, когда кнопка отвечающая за способность должна быть выключена
        public override bool GizmoDisabled(out string reason) {
            if (base.GizmoDisabled(out reason)) {
                return true;
            }

            Need_Spirituality spirituality = this.pawn.needs.TryGetNeed(LotrDefOf.lotr_SpiritualityNeed) as Need_Spirituality;

            if (spirituality == null) {
                reason = "Нет духовной энергии.";
                return true;
            }

            float cost = AbilityCost();

            if (spirituality.CurLevel < cost * 0.01f) {
                reason = $"Недостаточно духовности (Нужно {(cost).ToString("F0")}).";
                return true;
            }

            reason = null;
            return false;
        }
    }

    public class CompAbilityEffect_GiveHediff : CompAbilityEffect {
        public new CompProperties_AbilityGiveHediff Props => (CompProperties_AbilityGiveHediff)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);

            Pawn targetPawn = target.Pawn;
            if (targetPawn == null) return;

            if (targetPawn.health.hediffSet.HasHediff(Props.hediffDef)) return;

            Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, targetPawn);
            if (Props.severity > 0f)
                hediff.Severity = Props.severity;
            targetPawn.health.AddHediff(hediff);

            if (Props.showFleck)
                FleckMaker.Static(targetPawn.Position, targetPawn.Map, FleckDefOf.MicroSparks, 1.5f);
        }
    }

}