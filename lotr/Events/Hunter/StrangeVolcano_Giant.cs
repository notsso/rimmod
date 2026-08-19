using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

namespace lotr {
    // Инцидент
    public class IncidentWorker_MagmaGiantVolcano : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("MagmaGiantVolcano_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("MagmaGiantVolcano_World");
    }

    public class SitePartWorker_MagmaGiantVolcano : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("MagmaGiantVolcano_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;
        }
    }
}
