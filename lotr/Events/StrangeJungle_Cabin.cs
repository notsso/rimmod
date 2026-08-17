using System.Collections.Generic;
using System.Linq;
using System;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

namespace lotr {
    public class IncidentWorker_JungleCabin : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("JungleCabin_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("JungleCabin_World");
    }

    public class SitePartWorker_JungleCabin : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("JungleCabin_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;
        }
    }
}
