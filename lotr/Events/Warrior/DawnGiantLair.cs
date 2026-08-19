using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

namespace lotr {
    // ========== Синий Гигант: древние руины ==========
    public class IncidentWorker_DawnGiantLair : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("DawnGiantLair_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("DawnGiantLair_World");
    }

    public class SitePartWorker_DawnGiantLair : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;
            var def = DefDatabase<SitePartDef>.GetNamed("DawnGiantLair_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            var param = new GiantVillageParams();
            param.minHouses = 9;
            param.maxHouses = 13;
            param.streetLength = 55f;
            param.streetWidth = 3;
            param.buildingTemplates = new List<GiantBuildingTemplate> {
                new GiantHouseTemplate_Stone(),
                new GiantHouseTemplate_MediumWood()
            };
            param.mainBuildingTemplate = new GiantHouseTemplate_Ancient();
            param.giantKind = PawnKindDef.Named("lotr_DawnGiant");
            param.damageFactorForNonMain = 0.5f;
            param.roadTerrainDefName = "FlagstoneGranite";
            param.useRoads = true;
            param.usePalace = false;

            GiantVillageGenerator.GenerateVillage(map, param);
        }
    }
}
