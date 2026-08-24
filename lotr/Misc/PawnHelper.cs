using RimWorld;

using Verse;

namespace lotr {
    public static class PawnHelper {
        public static void MakePermanentManhunter(Pawn pawn) {
            if (pawn?.mindState?.mentalStateHandler == null) return;
            var manhunter = MentalStateDefOf.ManhunterPermanent;
            pawn.mindState.mentalStateHandler.TryStartMentalState(manhunter, null, true);
        }
    }
}
