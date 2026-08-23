using RimWorld;

using Verse;
using Verse.AI;

namespace lotr {
    public class CompProperties_PeacefulHairStrands : CompProperties {
        public CompProperties_PeacefulHairStrands() {
            compClass = typeof(CompPeacefulHairStrands);
        }
    }

    public class CompPeacefulHairStrands : ThingComp {
        public float xpLossPerTick = 1f;
        public int checkInterval = 60;
        public CompProperties_PeacefulHairStrands Props => (CompProperties_PeacefulHairStrands)props;
        private bool hasMuteTrait = false;

        public override void CompTick() {
            base.CompTick();

            if (hasMuteTrait)
                return;

            // Проверяем только с заданным интервалом (по умолчанию каждый тик)
            if (!parent.IsHashIntervalTick(checkInterval))
                return;

            // Убеждаемся, что предмет надет на живую пешку
            Apparel apparel = parent as Apparel;
            if (apparel == null)
                return;

            Pawn wearer = apparel.Wearer;
            if (wearer == null || wearer.Dead || !wearer.Spawned)
                return;

            // Находим навык общения
            SkillRecord socialSkill = wearer.skills?.GetSkill(SkillDefOf.Social);
            if (socialSkill == null)
                return;

            // Уменьшаем опыт навыка
            if (socialSkill.Level > 0 || socialSkill.xpSinceLastLevel > 0f) {
                float xpLoss = xpLossPerTick * checkInterval;
                socialSkill.Learn(-xpLoss, false);
            }

            hasMuteTrait = wearer.story?.traits?.HasTrait(TraitDef.Named("lotr_Mute")) ?? false;

            if (socialSkill.Level == 0 && socialSkill.xpSinceLastLevel == 0f) {
                wearer.story.traits.GainTrait(new Trait(TraitDef.Named("lotr_Mute")));
            }
        }
    }

    public class Verb_CalmTarget : Verb_CastBase {
        protected override bool TryCastShot() {
            Pawn casterPawn = caster as Pawn;
            Pawn targetPawn = currentTarget.Thing as Pawn;

            if (casterPawn == null || targetPawn == null || targetPawn.Dead)
                return false;

            // Получаем компонент перезарядки
            CompApparelReloadable reloadableCompSource = base.ReloadableCompSource;
            if (reloadableCompSource != null && reloadableCompSource.CanBeUsed(out string reason)) {
                reloadableCompSource.UsedOnce();

                // Успокаиваем цель
                targetPawn.health.AddHediff(HediffDef.Named("Hediff_PeacefulCalm_Charm"));
                return true;
            }

            return false;
        }
    }

    public class HediffCompProperties_CalmTarget : HediffCompProperties {
        public HediffCompProperties_CalmTarget() {
            compClass = typeof(HediffComp_CalmTarget);
        }
    }

    public class HediffComp_CalmTarget : HediffComp {
        private const int StunTicks = 3600; // 1 час

        public override void CompPostPostAdd(DamageInfo? dinfo) {
            base.CompPostPostAdd(dinfo);
            // Накладываем стан на цель
            Pawn.stances.stunner.StunFor(StunTicks, null, false);
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt) {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (Pawn != null && parent != null && !Pawn.Dead) {
                // Снимаем стан
                Pawn.stances.stunner.StopStun();
                // Удаляем Hediff
                Pawn.health.RemoveHediff(parent);
            }
        }

        public override void CompPostPostRemoved() {
            base.CompPostPostRemoved();
            // на всякий случай
            Pawn.stances.stunner.StopStun();
        }
    }
}
