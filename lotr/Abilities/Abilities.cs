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

    // Способность hunter7 (pyromaniac): Огненная броня
    public class CompProperties_AbilityGiveHediff : CompProperties_AbilityEffect {
        public HediffDef hediffDef;
        public float severity = 0f;
        public bool applyToCaster = true;
        public bool showFleck = true;

        public CompProperties_AbilityGiveHediff() {
            compClass = typeof(CompAbilityEffect_GiveHediff);
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
            if (Props.severity > 0f) hediff.Severity = Props.severity;

            targetPawn.health.AddHediff(hediff);

            if (Props.showFleck) FleckMaker.Static(targetPawn.Position, targetPawn.Map, FleckDefOf.MicroSparks, 1.5f);
        }
    }
    
    public class CompProperties_GiveHediffArea : CompProperties_AbilityEffect {
        public float radius = 5f;
        public float severity = 0f;
        public HediffDef hediffDef;
        public bool targetFriendly = false;
        public bool targetEnemies = false;
        public bool targetSelf = false;

        public CompProperties_GiveHediffArea() {
            compClass = typeof(CompAbilityEffect_GiveHediffArea);
        }
    }
    
    public class CompAbilityEffect_GiveHediffArea : CompAbilityEffect {
        public new CompProperties_GiveHediffArea Props => (CompProperties_GiveHediffArea)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            Pawn caster = parent.pawn;
            if (caster == null || caster.Map == null) return;

            Map map = caster.Map;
            float radius = Props.radius;
            IntVec3 center = target.Cell;

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            foreach (Pawn pawn in pawns) {
                if (pawn == null || pawn.Dead) continue;
                if (pawn.Position.DistanceToSquared(center) > radius * radius) continue;

                bool flag1 = !(Props.targetSelf == true && caster == pawn);
                bool flag2 = !(Props.targetEnemies == true && pawn.HostileTo(caster));
                bool flag3 = !(Props.targetFriendly == true && !(pawn.HostileTo(caster)));

                if (flag1 && flag2 && flag3) continue;

                if (pawn.health.hediffSet.HasHediff(Props.hediffDef) && Props.severity != 0f) {
                    pawn.health.hediffSet.TryGetHediff(Props.hediffDef, out Hediff hediff);
                    hediff.Severity += Props.severity;
                } else {
                    Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, pawn);
                    if (Props.severity != 0f) hediff.Severity = Props.severity;
                    pawn.health.AddHediff(hediff);
                }

                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.MicroSparks, 1.5f);
            }
        }
    }

}