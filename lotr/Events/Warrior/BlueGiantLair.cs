using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

namespace lotr {
    // ========== Синий Гигант: древние руины ==========
    public class IncidentWorker_BlueGiantLair : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("BlueGiantLair_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("BlueGiantLair_World");
    }

    public class SitePartWorker_BlueGiantLair : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("BlueGiantLair_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            var param = new GiantVillageParams();
            param.minHouses = 7;
            param.maxHouses = 11;
            param.streetLength = 45f;
            param.streetWidth = 3;
            param.buildingTemplates = new List<GiantBuildingTemplate> {
                new GiantHouseTemplate_Stone(),
                new GiantHouseTemplate_MediumWood()
            };
            param.mainBuildingTemplate = new GiantHouseTemplate_Ancient(); // древний главный дом
            param.giantKind = PawnKindDef.Named("lotr_BlueGiant");
            param.damageFactorForNonMain = 0.5f;
            param.roadTerrainDefName = "FlagstoneSlate";
            param.useRoads = true;
            param.usePalace = false;

            GiantVillageGenerator.GenerateVillage(map, param);
        }
    }
}
