using System.Collections.Generic;
using System.Linq;

using RimWorld;

using Verse;

namespace lotr {
    public class CompProperties_SunEmblem : CompProperties {
        public CompProperties_SunEmblem() {
            compClass = typeof(CompSunEmblem);
        }
    }

    public class CompSunEmblem : ThingComp {
        private const int TickInterval = 60;
        private const float DamagePerInterval = 5f;
        private const float WorshipRate = 0.001f;
        private const int PurificationRadius = 30;

        public override void CompTick() {
            base.CompTick();
            if (!parent.IsHashIntervalTick(TickInterval))
                return;

            Apparel apparel = parent as Apparel;
            if (apparel == null)
                return;

            Pawn wearer = apparel.Wearer;
            if (wearer == null || wearer.Dead || !wearer.Spawned)
                return;

            // Прогресс владельца
            Hediff worship = wearer.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("lotr_HediffSunWorship"));
            if (worship == null) {
                worship = wearer.health.AddHediff(HediffDef.Named("lotr_HediffSunWorship"));
                worship.Severity = 0f;
            }

            worship.Severity += WorshipRate;
            if (worship.Severity >= 1f) {
                ApplyLobotomy(wearer);
            }

            // Урон врагам
            List<Pawn> targets = new List<Pawn>();
            foreach (Pawn pawn in wearer.Map.mapPawns.AllPawnsSpawned) {
                if (pawn == wearer || pawn.Dead)
                    continue;
                if (pawn.kindDef != PawnKindDef.Named("lotr_Spirit"))
                    continue;
                if (!pawn.Position.InHorDistOf(wearer.Position, PurificationRadius))
                    continue;

                targets.Add(pawn);
            }

            foreach (Pawn pawn in targets) {
                if (pawn.Dead)
                    continue;

                // Наносим урон светом
                DamageInfo damageInfo = new DamageInfo(
                    DefDatabase<DamageDef>.GetNamed("lotr_Light"),
                    DamagePerInterval,
                    0f,
                    -1f,
                    wearer,
                    null,
                    null,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    null,
                    true,
                    true
                );
                pawn.TakeDamage(damageInfo);
            }
        }

        private void ApplyLobotomy(Pawn wearer) {
            TraitDef lobotomyDef = TraitDef.Named("lotr_SunWorship");
            if (lobotomyDef == null) {
                Log.Error("lotr_SunWorship trait not found! Check XML.");
                return;
            }

            if (!wearer.story.traits.HasTrait(lobotomyDef)) {
                wearer.story.traits.GainTrait(new Trait(lobotomyDef));
            }
        }

        public override void Notify_Unequipped(Pawn pawn) {
            base.Notify_Unequipped(pawn);

            Hediff worship = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("lotr_HediffSunWorship"));
            if (worship != null) {
                pawn.health.RemoveHediff(worship);
            }
        }
    }
}
