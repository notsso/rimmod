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
    public abstract class IncidentWorker_WorldSiteBase : IncidentWorker {
        protected abstract SitePartDef GetSitePartDef();
        protected abstract WorldObjectDef GetWorldObjectDef();

        protected virtual SitePartParams GetSitePartParams(IncidentParms parms) {
            return new SitePartParams {
                threatPoints = parms.points > 0f ? parms.points : 100f
            };
        }

        protected virtual void PostSiteCreated(Site site, IncidentParms parms) { }

        protected override bool CanFireNowSub(IncidentParms parms) {
            return parms.target is World && base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            var world = parms.target as World;
            if (world == null) return false;

            var sitePartDef = GetSitePartDef();
            if (sitePartDef == null) {
                Log.Error($"[{GetType().Name}] SitePartDef is null.");
                return false;
            }

            var tile = Find.WorldSelector.SelectedTile;
            if (tile < 0 || !Find.WorldGrid.InBounds(tile)) {
                Log.Error("Select a tile on the world map before running this incident.");
                return false;
            }

            var siteObjectDef = GetWorldObjectDef();
            if (siteObjectDef == null) {
                Log.Error($"[{GetType().Name}] WorldObjectDef is null.");
                return false;
            }

            var site = (Site)WorldObjectMaker.MakeWorldObject(siteObjectDef);
            site.Tile = tile;
            site.SetFaction(null);

            var partParams = GetSitePartParams(parms);
            var part = new SitePart(site, sitePartDef, partParams);
            site.AddPart(part);

            Find.WorldObjects.Add(site);

            site.GetComponent<TimedDetectionRaids>()?.ResetCountdown();

            CameraJumper.TryJump(site);
            SendStandardLetter(parms, site);

            PostSiteCreated(site, parms);
            return true;
        }
    }
}
