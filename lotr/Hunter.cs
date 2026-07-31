using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;

namespace lotr {
    public class Hunter9_Hediff : HediffWithComps {
        public override string SeverityLabel {
            get {
                string baseLabel = base.SeverityLabel;
                string percent = (this.Severity).ToStringPercent();

                if (!baseLabel.NullOrEmpty()) {
                    return $"{baseLabel} ({percent})";
                }

                return percent;
            }
        }

        private int ticksCounter = 0;

        public override void Tick() {
            base.Tick();

            ticksCounter++;
            // Будем запускать проверку лечения каждые 180 тиков (примерно раз в 3 секунды реального времени)
            if (ticksCounter >= 180) {
                ticksCounter = 0;
                TryHealWounds();
            }
        }

        private void TryHealWounds() {
            if (this.pawn == null || this.pawn.health == null) return;

            float healAmount = 0f;
            switch (this.CurStageIndex) {
                case 0: // Стадия: лох
                    healAmount = 0.1f;
                    break;
                case 1: // Стадия: нормик
                    healAmount = 0.2f;
                    break;
                case 2: // Стадия: смешарик
                    healAmount = 0.3f;
                    break;
            }

            if (healAmount <= 0f) return;

            List<Hediff_Injury> injuries = this.pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(x => x.CanHealNaturally())
                .ToList();

            if (injuries.Any()) {
                Hediff_Injury worstInjury = injuries.OrderByDescending(x => x.Severity).First();
                worstInjury.Severity -= healAmount;
            }
        }
    }
}
