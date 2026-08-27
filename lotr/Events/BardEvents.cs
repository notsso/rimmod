using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

namespace lotr {
    public class IncidentWorker_FireBirdNest : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("FireBirdNest_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("FireBirdNest_World");
    }

    public class SitePartWorker_FireBirdNest : SitePartWorker { }

    public class IncidentWorker_SingingSunflowerGlade : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SingingSunflowerGlade_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SingingSunflowerGlade_World");
    }

    public class SitePartWorker_SingingSunflowerGlade : SitePartWorker { }

    public class IncidentWorker_MirrorHedgehogGlade : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("MirrorHedgehogGlade_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("MirrorHedgehogGlade_World");
    }

    public class SitePartWorker_MirrorHedgehogGlade : SitePartWorker { }

    public class IncidentWorker_DawnRoosterMeadow : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("DawnRoosterMeadow_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("DawnRoosterMeadow_World");
    }

    public class IncidentWorker_RadianceSpiritPactTreeGrove : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("RadianceSpiritPactTreeGrove_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("RadianceSpiritPactTreeGrove_World");
    }

    public class SitePartWorker_RadianceSpiritPactTreeGrove : SitePartWorker { }

    public class SitePartWorker_DawnRoosterMeadow : SitePartWorker { }

    public class IncidentWorker_SpiritPactBirdGrove : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SpiritPactBirdGrove_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SpiritPactBirdGrove_World");
    }

    public class IncidentWorker_CrystallizedElderTreeForest : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("CrystallizedElderTreeForest_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("CrystallizedElderTreeForest_World");
    }

    public class SitePartWorker_CrystallizedElderTreeForest : SitePartWorker { }

    public class SitePartWorker_SpiritPactBirdGrove : SitePartWorker { }

    public class IncidentWorker_DawnRoosterKingThrone : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("DawnRoosterKingThrone_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("DawnRoosterKingThrone_World");
    }

    public class SitePartWorker_DawnRoosterKingThrone : SitePartWorker { }

    public class IncidentWorker_SunDivineBirdPeak : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SunDivineBirdPeak_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SunDivineBirdPeak_World");
    }

    public class SitePartWorker_SunDivineBirdPeak : SitePartWorker { }
}