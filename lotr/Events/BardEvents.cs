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

    public class SitePartWorker_FireBirdNest : SitePartWorker {
        public override void PostMapGenerate(Map map) { }
    }

    public class IncidentWorker_MirrorHedgehogGlade : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("MirrorHedgehogGlade_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("MirrorHedgehogGlade_World");
    }

    public class SitePartWorker_MirrorHedgehogGlade : SitePartWorker {
        public override void PostMapGenerate(Map map) { }
    }

    public class IncidentWorker_DawnRoosterMeadow : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("DawnRoosterMeadow_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("DawnRoosterMeadow_World");
    }

    public class SitePartWorker_DawnRoosterMeadow : SitePartWorker {
        public override void PostMapGenerate(Map map) { }
    }

    public class IncidentWorker_SpiritPactBirdGrove : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SpiritPactBirdGrove_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SpiritPactBirdGrove_World");
    }

    public class SitePartWorker_SpiritPactBirdGrove : SitePartWorker {
        public override void PostMapGenerate(Map map) { }
    }

    public class IncidentWorker_DawnRoosterKingThrone : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("DawnRoosterKingThrone_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("DawnRoosterKingThrone_World");
    }

    public class SitePartWorker_DawnRoosterKingThrone : SitePartWorker {
        public override void PostMapGenerate(Map map) { }
    }

    public class IncidentWorker_SunDivineBirdPeak : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SunDivineBirdPeak_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SunDivineBirdPeak_World");
    }

    public class SitePartWorker_SunDivineBirdPeak : SitePartWorker {
        public override void PostMapGenerate(Map map) { }
    }
}