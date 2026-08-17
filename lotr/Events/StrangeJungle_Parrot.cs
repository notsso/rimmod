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
    // Инцидент
    public class IncidentWorker_JungleParrot : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("JungleParrot_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("JungleParrot_World");
    }

    public class SitePartWorker_JungleParrot : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("JungleParrot_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;
        }
    }
}