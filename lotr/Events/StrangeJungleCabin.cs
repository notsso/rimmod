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
    // ===== Инцидент =====
    public class IncidentWorker_JungleCabin : IncidentWorker {
        protected override bool CanFireNowSub(IncidentParms parms) {
            if (!(parms.target is World)) return false;
            return base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            World world = parms.target as World;
            if (world == null) return false;

            SitePartDef sitePart = DefDatabase<SitePartDef>.GetNamed("JungleCabin_Site");
            if (sitePart == null) return false;

            int tile = Find.WorldSelector.SelectedTile;
            if (tile < 0 || !Find.WorldGrid.InBounds(tile)) {
                Log.Error("Select a tile on the world map before running this incident.");
                return false;
            }

            float threatPoints = parms.points > 0f ? parms.points : 100f;

            WorldObjectDef siteObjectDef = DefDatabase<WorldObjectDef>.GetNamed("JungleCabin_World");
            Site site = (Site)WorldObjectMaker.MakeWorldObject(siteObjectDef);
            site.Tile = tile;
            site.SetFaction(null);

            SitePartParams partParams = new SitePartParams { threatPoints = threatPoints };
            SitePart part = new SitePart(site, sitePart, partParams);
            site.AddPart(part);

            Find.WorldObjects.Add(site);

            var timedRaids = site.GetComponent<TimedDetectionRaids>();
            if (timedRaids != null) timedRaids.ResetCountdown();

            CameraJumper.TryJump(site);
            SendStandardLetter(parms, site);
            return true;
        }
    }

    // ===== SitePartWorker (пост-генерация карты) =====
    public class SitePartWorker_JungleCabin : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("JungleCabin_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            // Дополнительная логика не требуется, охотник спавнится в GenStep
        }
    }
}
