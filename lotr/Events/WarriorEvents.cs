using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

namespace lotr {
    // ========== Гигант-Воин (9): деревянный дом ==========
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

    // ========== Гигант-Оруженосец (8): каменный дом ==========
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
            param.mainBuildingTemplate = new GiantHouseTemplate_Stone();
            param.giantKind = PawnKindDef.Named("lotr_GiantSquire");
            param.damageFactorForNonMain = 0.5f;
            param.roadTerrainDefName = "FlagstoneSandstone";
            param.useRoads = true;
            param.usePalace = false;

            GiantVillageGenerator.GenerateVillage(map, param);
        }
    }

    // ========== Синий Гигант (7): заброшенная деревня ==========
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
            param.mainBuildingTemplate = new GiantHouseTemplate_Ancient();
            param.giantKind = PawnKindDef.Named("lotr_BlueGiant");
            param.damageFactorForNonMain = 0.5f;
            param.roadTerrainDefName = "FlagstoneSlate";
            param.useRoads = true;
            param.usePalace = false;

            GiantVillageGenerator.GenerateVillage(map, param);
        }
    }

    // ========== Рассветный гигант (6): древние руины ==========
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

    // ========== Серый Гигант (5): забытый замок ==========
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

    // ========== Святой Гигант (4): древний дворец ==========
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
            param.mainBuildingTemplate = new GiantPalaceTemplate_Large();
            param.giantKind = PawnKindDef.Named("lotr_DivineGiant");
            param.damageFactorForNonMain = 0.5f;
            param.roadTerrainDefName = "FlagstoneMarble";
            param.fourRoads = true;
            param.usePalace = true;

            GiantVillageGenerator.GenerateVillage(map, param);
        }
    }
}
