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

    // ========== Болотный кабан (9): мистическое болото ==========
    public class IncidentWorker_MysticalMarsh : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("MysticalMarsh_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("MysticalMarsh_World");
    }

    public class SitePartWorker_MysticalMarsh : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;
            var def = DefDatabase<SitePartDef>.GetNamed("MysticalMarsh_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            map.weatherManager.TransitionTo(WeatherDefOf.FoggyRain);
        }
    }

    // ========== Острозубый попугай (8): джунгли ==========
    public class IncidentWorker_JungleParrot : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("JungleParrot_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("JungleParrot_World");
    }

    public class SitePartWorker_JungleParrot : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("JungleParrot_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;
        }
    }

    // ========== Огненная саламандра (7): пустыня ==========
    public class IncidentWorker_FireSalamanderRuins : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("FireSalamanderRuins_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("FireSalamanderRuins_World");
    }

    public class SitePartWorker_FireSalamanderRuins : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("FireSalamanderRuins_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;
        }
    }

    // ========== Магмовый эльф (7): Странный вулкан ==========
    public class IncidentWorker_StrangeVolcano : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("StrangeVolcano_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("StrangeVolcano_World");
    }

    public class SitePartWorker_StrangeVolcano : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("StrangeVolcano_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            float threatPoints = sitePart.parms.threatPoints;
        }
    }

    // ========== Черный охотничий паук (6): туманный лес ==========
    public class IncidentWorker_SpiderForest : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("SpiderForest_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("SpiderForest_World");
    }

    public class SitePartWorker_SpiderForest : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("SpiderForest_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            map.weatherManager.TransitionTo(WeatherDefOf.Fog);
        }
    }

    // ========== Сфинкс (6): пустынные руины ==========
    public class IncidentWorker_DesertRuins : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("DesertRuins_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("DesertRuins_World");
    }

    public class SitePartWorker_DesertRuins : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("DesertRuins_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;
        }
    }

    // ========== Демонический волк (5): туманный лес ==========
    public class IncidentWorker_TemperateForest : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("TemperateForest_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("TemperateForest_World");
    }

    public class SitePartWorker_TemperateForest : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("TemperateForest_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            map.weatherManager.TransitionTo(WeatherDefOf.Fog);
        }
    }

    // ========== Лесной охотник (5): джунгли ==========
    public class IncidentWorker_JungleCabin : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("JungleCabin_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("JungleCabin_World");
    }

    public class SitePartWorker_JungleCabin : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("JungleCabin_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;
        }
    }

    // ========== Магматический гигант (4): странный вулкан ==========
    public class IncidentWorker_MagmaGiantVolcano : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("MagmaGiantVolcano_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("MagmaGiantVolcano_World");
    }

    public class SitePartWorker_MagmaGiantVolcano : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;

            var def = DefDatabase<SitePartDef>.GetNamed("MagmaGiantVolcano_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;
        }
    }
}
