using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;


namespace lotr {
    
    public class HediffCompProperties_DamageAura : HediffCompProperties {
        public float radius;
        public DamageDef damageDef;
        public int damageAmount;
        public int tickInterval;

        public HediffCompProperties_DamageAura() {
            this.compClass = typeof(HediffComp_DamageAura);
        }
    }

    public class HediffComp_DamageAura : HediffComp {
        public HediffCompProperties_DamageAura Props => (HediffCompProperties_DamageAura)this.props;

        public override void CompPostTick(ref float severityAdjustment) {
            base.CompPostTick(ref severityAdjustment);

            if (Find.TickManager.TicksGame % Props.tickInterval == 0) {

                Pawn caster = this.Pawn;
                if (caster == null || !caster.Spawned || caster.Dead) return;

                IntVec3 center = caster.Position;
                Map map = caster.Map;

                foreach (Pawn pawn in Utility.GetPawnsInRadius(caster, Props.radius)) {

                    DamageInfo dinfo = new DamageInfo(Props.damageDef, Props.damageAmount, 0f, -1f, caster);
                    pawn.TakeDamage(dinfo);

                    // FleckMaker.ThrowMicroSparks(targetPawn.Position.ToVector3Shifted(), map); TODO: эффекты

                }

            }

        }

    }

    public class Hediff_Trackable : HediffWithComps {
        
        public Pawn Instigator;

    }

}