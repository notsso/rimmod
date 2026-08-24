using System.Linq;

using RimWorld;

using Verse;
using Verse.AI;

namespace lotr {
    public abstract class Verb_CharmBase : Verb_CastBase {
        // Каждый наследник может указать своё исследование (по умолчанию Artifact Usage)
        protected virtual string RequiredResearchDefName => "lotr_CharmCarving";

        public override bool Available() {
            // Базовая доступность (владелец, заряды и т.п.)
            if (!base.Available())
                return false;

            // Проверяем, изучено ли исследование
            ResearchProjectDef research = ResearchProjectDef.Named(RequiredResearchDefName);
            return research != null && research.IsFinished;
        }
    }
}