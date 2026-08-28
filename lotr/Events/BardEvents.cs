using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

namespace lotr {

    // ========== Огненная птица (9): лес ==========
    public class IncidentWorker_FireBirdNest : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("FireBirdNest_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("FireBirdNest_World");
    }

    public class SitePartWorker_FireBirdNest : SitePartWorker { }

    // ========== Поющий подсолнух (9): лес ==========
    public class IncidentWorker_SingingSunflowerGlade : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SingingSunflowerGlade_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SingingSunflowerGlade_World");
    }

    public class SitePartWorker_SingingSunflowerGlade : SitePartWorker { }

    // ========== Зеркальный еж (8): лес ==========
    public class IncidentWorker_MirrorHedgehogGlade : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("MirrorHedgehogGlade_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("MirrorHedgehogGlade_World");
    }

    public class SitePartWorker_MirrorHedgehogGlade : SitePartWorker { }

    // ========== Брилиантовый камень (8): заброшенный храм ==========
    public class IncidentWorker_BrillianceRockSanctuary : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("BrillianceRockSanctuary_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("BrillianceRockSanctuary_World");
    }

    public class SitePartWorker_BrillianceRockSanctuary : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;
            var def = DefDatabase<SitePartDef>.GetNamed("BrillianceRockSanctuary_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            var param = new SanctuaryLabyrinthParams();
            param.roomsPerSide = 3;
            param.spiritsPerRoom = new IntRange(1, 1);
            param.spiritChancePerRoom = 0.5f;
            param.addBossSpirit = false;
            param.lootDef = ThingDef.Named("lotr_BrillianceRock");
            param.lootCount = 1;
            param.mysticalComponentsCount = 1;
            param.sideLootChance = 0.4f;
            param.wallStuff = ThingDef.Named("BlocksSandstone");
            param.floorTerrain = TerrainDef.Named("AncientTile");
            param.useDoors = true;
            param.useFog = true;

            SanctuaryLabyrinthGenerator.Generate(map, param);
        }
    }

    // ========== Закатный петух (7): лес ==========
    public class IncidentWorker_DawnRoosterMeadow : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("DawnRoosterMeadow_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("DawnRoosterMeadow_World");
    }

    public class SitePartWorker_DawnRoosterMeadow : SitePartWorker { }

    // ========== дерево договоров (7): лес ==========
    public class IncidentWorker_RadianceSpiritPactTreeGrove : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("RadianceSpiritPactTreeGrove_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("RadianceSpiritPactTreeGrove_World");
    }

    public class SitePartWorker_RadianceSpiritPactTreeGrove : SitePartWorker { }

    // ========== духовная птица (6): лес ==========
    public class IncidentWorker_SpiritPactBirdGrove : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SpiritPactBirdGrove_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SpiritPactBirdGrove_World");
    }

    public class SitePartWorker_SpiritPactBirdGrove : SitePartWorker { }

    // ========== кристальное дерево (6): лес ==========
    public class IncidentWorker_CrystallizedElderTreeForest : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("CrystallizedElderTreeForest_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("CrystallizedElderTreeForest_World");
    }

    public class SitePartWorker_CrystallizedElderTreeForest : SitePartWorker { }

    // ========== король закатных петухов (5): лес ==========
    public class IncidentWorker_DawnRoosterKingThrone : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("DawnRoosterKingThrone_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("DawnRoosterKingThrone_World");
    }

    public class SitePartWorker_DawnRoosterKingThrone : SitePartWorker { }

    // ========== чистый брилиантовый камень (5): заброшенный храм ==========
    public class IncidentWorker_PureWhiteBrilliantRockSanctuary : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("PureWhiteBrilliantRockSanctuary_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("PureWhiteBrilliantRockSanctuary_World");
    }

    public class SitePartWorker_PureWhiteBrilliantRockSanctuary : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;
            var def = DefDatabase<SitePartDef>.GetNamed("PureWhiteBrilliantRockSanctuary_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            var param = new SanctuaryLabyrinthParams();
            param.roomsPerSide = 4;
            param.spiritsPerRoom = new IntRange(2, 3);
            param.spiritChancePerRoom = 0.7f;
            param.addBossSpirit = true;
            param.lootDef = ThingDef.Named("lotr_PureWhiteBrilliantRock");
            param.lootCount = 1;
            param.mysticalComponentsCount = 2;
            param.sideLootChance = 0.5f;
            param.wallStuff = ThingDef.Named("BlocksSlate");
            param.floorTerrain = TerrainDef.Named("AncientTile");
            param.useDoors = false;
            param.useFog = true;

            SanctuaryLabyrinthGenerator.Generate(map, param);
        }
    }

    // ========== священная солнечная птица (4): лес ==========
    public class IncidentWorker_SunDivineBirdPeak : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SunDivineBirdPeak_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SunDivineBirdPeak_World");
    }

    public class SitePartWorker_SunDivineBirdPeak : SitePartWorker { }

    // ========== святой камень (4): заброшенный храм ==========
    public class IncidentWorker_HolyBrillianceRockSanctuary : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("HolyBrillianceRockSanctuary_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("HolyBrillianceRockSanctuary_World");
    }

    public class SitePartWorker_HolyBrillianceRockSanctuary : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;
            var def = DefDatabase<SitePartDef>.GetNamed("HolyBrillianceRockSanctuary_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            var param = new SanctuaryLabyrinthParams();
            param.roomsPerSide = 5;
            param.spiritsPerRoom = new IntRange(3, 4);
            param.spiritChancePerRoom = 0.8f;
            param.addBossSpirit = true;
            param.lootDef = ThingDef.Named("lotr_HolyBrillianceRock");
            param.lootCount = 1;
            param.mysticalComponentsCount = 3;
            param.sideLootChance = 0.6f;
            param.wallStuff = ThingDef.Named("BlocksMarble");
            param.floorTerrain = TerrainDef.Named("AncientTile");
            param.useDoors = false;
            param.useFog = true;

            SanctuaryLabyrinthGenerator.Generate(map, param);
        }
    }
}