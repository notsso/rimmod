using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

namespace lotr {
    public class IncidentWorker_StrangeVolcano : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("StrangeVolcano_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("StrangeVolcano_World");
    }

    public class SitePartWorker_StrangeVolcano : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("StrangeVolcano_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            float threatPoints = sitePart.parms.threatPoints;
        }
    }
}
