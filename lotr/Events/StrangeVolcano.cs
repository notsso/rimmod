using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

using UnityEngine;

namespace lotr {
    public class SitePartWorker_StrangeVolcano : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("StrangeVolcano_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            float threatPoints = sitePart.parms.threatPoints;
        }
    }

    public class IncidentWorker_StrangeVolcano : IncidentWorker {
        protected override bool CanFireNowSub(IncidentParms parms) {
            if (!(parms.target is World)) return false;
            return base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            World world = parms.target as World;
            if (world == null) return false;

            SitePartDef sitePart = DefDatabase<SitePartDef>.GetNamed("StrangeVolcano_Site");
            if (sitePart == null) return false;

            int tile = Find.WorldSelector.SelectedTile;
            if (tile < 0 || !Find.WorldGrid.InBounds(tile)) {
                Log.Error("Select a tile on the world map before running this incident.");
                return false;
            }

            float threatPoints = parms.points > 0f ? parms.points : 100f;

            WorldObjectDef siteObjectDef = DefDatabase<WorldObjectDef>.GetNamed("StrangeVolcano_World");
            Site site = (Site)WorldObjectMaker.MakeWorldObject(siteObjectDef);
            site.Tile = tile;
            site.SetFaction(null);

            SitePartParams partParams = new SitePartParams { threatPoints = threatPoints };
            SitePart part = new SitePart(site, sitePart, partParams);
            site.AddPart(part);

            Find.WorldObjects.Add(site);

            Log.Message($"Volcanic site created with {site.parts.Count} parts.");

            var timedRaids = site.GetComponent<TimedDetectionRaids>();
            if (timedRaids != null) timedRaids.ResetCountdown();

            CameraJumper.TryJump(site);
            SendStandardLetter(parms, site);
            return true;
        }
    }
}
