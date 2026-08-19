using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

namespace lotr {
    // ========== Синий Гигант: древние руины ==========
    public class IncidentWorker_GreyGiantLair : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("GreyGiantLair_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("GreyGiantLair_World");
    }

    public class SitePartWorker_GreyGiantLair : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;
            var def = DefDatabase<SitePartDef>.GetNamed("GreyGiantLair_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            var param = new GiantVillageParams();
            param.minHouses = 6;
            param.maxHouses = 8;
            param.streetLength = 40f;
            param.streetWidth = 3;
            param.buildingTemplates = new List<GiantBuildingTemplate> {
                new GiantHouseTemplate_Stone(),
                new GiantHouseTemplate_MediumWood()
            };
            param.mainBuildingTemplate = new GiantPalaceTemplate_Small();
            param.giantKind = PawnKindDef.Named("lotr_GreyGiant");
            param.damageFactorForNonMain = 0.5f;
            param.roadTerrainDefName = "FlagstoneSlate";
            param.useRoads = false;
            param.usePalace = true;

            GiantVillageGenerator.GenerateVillage(map, param);
        }
    }
}
