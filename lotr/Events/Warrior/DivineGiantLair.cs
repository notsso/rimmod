using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

namespace lotr {
    // ========== Синий Гигант: древние руины ==========
    public class IncidentWorker_DivineGiantLair : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("DivineGiantLair_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("DivineGiantLair_World");
    }

    public class SitePartWorker_DivineGiantLair : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;
            var def = DefDatabase<SitePartDef>.GetNamed("DivineGiantLair_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            var param = new GiantVillageParams();
            param.minHouses = 8;
            param.maxHouses = 12;
            param.streetLength = 60f;
            param.streetWidth = 3;
            param.buildingTemplates = new List<GiantBuildingTemplate> {
                new GiantHouseTemplate_Stone(),
                new GiantHouseTemplate_Ancient()
            };
            param.mainBuildingTemplate = new GiantPalaceTemplate_Large(); // большой дворец
            param.giantKind = PawnKindDef.Named("lotr_DivineGiant");
            param.damageFactorForNonMain = 0.5f;
            param.roadTerrainDefName = "FlagstoneMarble";
            param.fourRoads = true;
            param.usePalace = true;

            GiantVillageGenerator.GenerateVillage(map, param);
        }
    }
}
