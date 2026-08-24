using RimWorld;

using Verse;

namespace lotr {
    public class CompProperties_FlowerOfBlood : CompProperties {
        public CompProperties_FlowerOfBlood() {
            compClass = typeof(CompFlowerOfBlood);
        }
    }

    public class CompFlowerOfBlood : ThingComp {
        private const int SkillLossIntervalTicks = 60;
        private const int StunIntervalTicks = 3600;

        public override void Notify_Equipped(Pawn pawn) {
            base.Notify_Equipped(pawn);
            if (pawn != null && pawn.health != null) {
                pawn.health.AddHediff(HediffDef.Named("lotr_HediffFlowerOfBlood"));
            }
        }

        public override void Notify_Unequipped(Pawn pawn) {
            base.Notify_Unequipped(pawn);
            if (pawn != null && pawn.health != null) {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("lotr_HediffFlowerOfBlood"));
                if (hediff != null) {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        public override void CompTick() {
            base.CompTick();

            Apparel apparel = parent as Apparel;
            if (apparel == null)
                return;

            Pawn wearer = apparel.Wearer;
            if (wearer == null || wearer.Dead || !wearer.Spawned)
                return;

            // Каждые 60 тиков теряем навыки в общении и исследовании
            if (wearer.IsHashIntervalTick(SkillLossIntervalTicks)) {
                ReduceSkill(wearer, SkillDefOf.Social);
                ReduceSkill(wearer, SkillDefOf.Intellectual);
            }

            // Каждый час стан на случайное время
            if (wearer.IsHashIntervalTick(StunIntervalTicks)) {
                int stunDuration = Rand.Range(120, 600);
                wearer.stances.stunner.StunFor(stunDuration, null, false);
            }
        }

        private void ReduceSkill(Pawn pawn, SkillDef skillDef) {
            SkillRecord skill = pawn.skills?.GetSkill(skillDef);
            if (skill != null && skill.Level > 0) {
                skill.Learn(-30, false);
            }
        }
    }
}
