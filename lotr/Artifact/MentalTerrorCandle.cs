using System;

using RimWorld;

using Verse;
using Verse.AI;

namespace lotr {
    public abstract class Verb_MentalTerrorCandleBase : Verb_CastBase {
        private const float SuccessChance = 0.8f;

        protected abstract void ApplyEffect(Pawn target);

        protected override bool TryCastShot() {
            Pawn targetPawn = currentTarget.Thing as Pawn;
            if (targetPawn == null || targetPawn.Dead)
                return false;

            CompApparelReloadable reloadable = base.ReloadableCompSource;
            if (reloadable == null || !reloadable.CanBeUsed(out string reason))
                return false;

            reloadable.UsedOnce();

            Pawn casterPawn = caster as Pawn;
            if (Rand.Chance(SuccessChance)) {
                // Успех: применяем эффект к цели
                ApplyEffect(targetPawn);
            } else {
                // Провал: кастер впадает в ступор
                MentalStateDef stuporDef = MentalStateDefOf.Wander_Psychotic;
                if (casterPawn != null && casterPawn.mindState?.mentalStateHandler != null) {
                    casterPawn.mindState.mentalStateHandler.TryStartMentalState(stuporDef, null, false);
                }
            }

            return true;
        }
    }

    public class Verb_MentalTherapy : Verb_MentalTerrorCandleBase {
        protected override void ApplyEffect(Pawn target) {
            // Снимаем нервный срыв
            if (target.MentalState != null)
                target.MentalState.RecoverFromState();
        }
    }

    public class Verb_ReduceResistance : Verb_MentalTerrorCandleBase {
        protected override void ApplyEffect(Pawn target) {
            // Снижаем сопротивление вербовке
            if (target.guest != null) {
                target.guest.resistance = Math.Max(0, target.guest.resistance - 5);
            }
        }
    }
}
