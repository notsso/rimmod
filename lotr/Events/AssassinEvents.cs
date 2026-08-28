using System.Collections.Generic;
using System.Linq;

using Verse;

using RimWorld;
using RimWorld.Planet;

namespace lotr {

    // ========== змеиная птица (9): джунгли ==========
    public class IncidentWorker_SerpentBirdTower : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SerpentBirdTower_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SerpentBirdTower_World");
    }

    public class SitePartWorker_SerpentBirdTower : SitePartWorker { }

    // ========== теневой цветок (9): мистическое болото ==========
    public class IncidentWorker_ShadowPoisonSwamp : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("ShadowPoisonSwamp_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("ShadowPoisonSwamp_World");
    }

    public class SitePartWorker_ShadowPoisonSwamp : SitePartWorker { }

    // ========== демонический медоед (8): лес ==========
    public class IncidentWorker_DemonThroatHoneyguideLair : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("DemonThroatHoneyguideLair_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("DemonThroatHoneyguideLair_World");
    }

    public class SitePartWorker_DemonThroatHoneyguideLair : SitePartWorker { }

    // ========== темный бродяга (8): лес ==========
    public class IncidentWorker_DarkProwlerDen : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("DarkProwlerDen_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("DarkProwlerDen_World");
    }

    public class SitePartWorker_DarkProwlerDen : SitePartWorker { }

    // ========== Агатовый павлин (7): лес ==========
    public class IncidentWorker_AgatePeacockMeadow : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("AgatePeacockMeadow_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("AgatePeacockMeadow_World");
    }

    public class SitePartWorker_AgatePeacockMeadow : SitePartWorker { }

    // ========== рыба бездны (7): мистическое болото ==========
    public class IncidentWorker_AbyssDemonicFishMarsh : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("AbyssDemonicFishMarsh_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("AbyssDemonicFishMarsh_World");
    }

    public class SitePartWorker_AbyssDemonicFishMarsh : SitePartWorker { }

    // ========== храм суккуба (6): лес ==========
    public class IncidentWorker_SuccubusTemple : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SuccubusTemple_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SuccubusTemple_World");
    }

    public class SitePartWorker_SuccubusTemple : SitePartWorker { }

    // ========== Черная вдова (6): туманный лес ==========
    public class IncidentWorker_BlackWidowForest : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("BlackWidowForest_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("BlackWidowForest_World");
    }

    public class SitePartWorker_BlackWidowForest : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("BlackWidowForest_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            map.weatherManager.TransitionTo(WeatherDefOf.Fog);
        }
    }

    // ========== цветоголовая летучая мышь (5): пещера ==========
    public class IncidentWorker_FlowerFacedBatCave : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("FlowerFacedBatCave_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("FlowerFacedBatCave_World");
    }

    public class SitePartWorker_FlowerFacedBatCave : SitePartWorker { }

    // ========== двухвостая змея (5): лес ==========
    public class IncidentWorker_TwoTailedSnakeHollow : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("TwoTailedSnakeHollow_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("TwoTailedSnakeHollow_World");
    }

    public class SitePartWorker_TwoTailedSnakeHollow : SitePartWorker { }

    // ========== чумная змея (4): мистическое болото ==========
    public class IncidentWorker_PlagueSerpentLair : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("PlagueSerpentLair_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("PlagueSerpentLair_World");
    }

    public class SitePartWorker_PlagueSerpentLair : SitePartWorker { }

    // ========== серебрянный охотник (4): лес ==========
    public class IncidentWorker_SilverHunterTerritory : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SilverHunterTerritory_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SilverHunterTerritory_World");
    }

    public class SitePartWorker_SilverHunterTerritory : SitePartWorker { }
}
