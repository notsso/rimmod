using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Специфичная логика Охотника
    public class Hunter9_Hediff : Beyonder_Hediff {
        public override float SpiritualityFactor => 1.2f;

        public Hunter9_Hediff() {
            maxProgressPerCategory = 0.8f;
        }
    }

    public class Verb_ExplosionZone : Verb_CastAbility {
        protected override bool TryCastShot() {
            if (this.ability != null) {
                this.ability.Activate(this.currentTarget, this.currentDestination);
                return true;
            }
            return false;
        }

        public override void DrawHighlight(LocalTargetInfo target) {
            base.DrawHighlight(target);

            if (!target.IsValid || CasterPawn == null)
                return;

            // Пытаемся получить projectileDef и его explosionRadius
            float explosionRadius = 0f;
            if (this.ability != null) {
                var comp = this.ability.comps
                    .OfType<CompAbilityEffect_LaunchProjectile>()
                    .FirstOrDefault();
                if (comp != null && comp.Props.projectileDef != null) {
                    explosionRadius = comp.Props.projectileDef.projectile.explosionRadius;
                }
            }

            if (explosionRadius > 0f) {
                Vector3 center = target.Cell.ToVector3Shifted();
                GenDraw.DrawRadiusRing(center.ToIntVec3(), explosionRadius, new Color(1f, 0.8f, 0.2f)); // жёлто-оранжевый
            }
        }
    }
}
