using Verse;

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
    }
}
