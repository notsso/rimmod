using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

namespace lotr {
    public class IncidentWorker_GiantWarriorLair : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("GiantWarriorLair_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("GiantWarriorLair_World");
    }

    public class SitePartWorker_GiantWarriorLair : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("GiantWarriorLair_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            var param = new GiantVillageParams();
            param.minHouses = 3;
            param.maxHouses = 5;
            param.streetLength = 25f;
            param.streetWidth = 3;
            param.buildingTemplates = new List<GiantBuildingTemplate> {
                new GiantHouseTemplate_SmallWood()
            };
            param.mainBuildingTemplate = new GiantHouseTemplate_MediumWood();
            param.giantKind = PawnKindDef.Named("lotr_GiantWarrior");
            param.damageFactorForNonMain = 0.5f;
            param.roadTerrainDefName = "PackedDirt";
            param.useRoads = true;
            param.usePalace = false;

            GiantVillageGenerator.GenerateVillage(map, param);
        }
    }
}
