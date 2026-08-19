using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

namespace lotr {
    // ========== Гигант-Оруженосец: каменный дом ==========
    public class IncidentWorker_GiantSquireLair : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("GiantSquireLair_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("GiantSquireLair_World");
    }

    public class SitePartWorker_GiantSquireLair : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("GiantSquireLair_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            var param = new GiantVillageParams();
            param.minHouses = 5;
            param.maxHouses = 8;
            param.streetLength = 35f;
            param.streetWidth = 3;
            param.buildingTemplates = new List<GiantBuildingTemplate> {
                new GiantHouseTemplate_MediumWood(),
                new GiantHouseTemplate_SmallWood()
            };
            param.mainBuildingTemplate = new GiantHouseTemplate_Stone(); // каменный главный дом
            param.giantKind = PawnKindDef.Named("lotr_GiantSquire");
            param.damageFactorForNonMain = 0.5f;
            param.roadTerrainDefName = "FlagstoneSandstone";
            param.useRoads = true;
            param.usePalace = false;

            GiantVillageGenerator.GenerateVillage(map, param);
        }
    }
}
