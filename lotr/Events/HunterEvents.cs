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

    // ========== Кроваво красный каштан (9): лес ==========
    public class IncidentWorker_BloodRedChestnutGrove : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("BloodRedChestnutGrove_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("BloodRedChestnutGrove_World");
    }

    public class SitePartWorker_BloodRedChestnutGrove : SitePartWorker { }

    // ========== Острозубый попугай (8): джунгли ==========
    public class IncidentWorker_JungleParrot : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("JungleParrot_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("JungleParrot_World");
    }

    public class SitePartWorker_JungleParrot : SitePartWorker { }

    // ========== Трупоядная флора (8): кладбище ==========
    public class IncidentWorker_CorpseLilyMarsh : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("CorpseLilyMarsh_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("CorpseLilyMarsh_World");
    }

    public class SitePartWorker_CorpseLilyMarsh : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;
            var def = DefDatabase<SitePartDef>.GetNamed("MysticalMarsh_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            map.weatherManager.TransitionTo(WeatherDefOf.Fog);
        }
    }

    // ========== Трупоядная флора (8) ==========
    public class IncidentWorker_CorpseFloraGrown : IncidentWorker {
        private const int MinCorpsesInCluster = 5;
        private const float SearchRadius = 5.9f;
        private const int MaxPlantsToSpawn = 3;

        protected override bool CanFireNowSub(IncidentParms parms) {
            if (!base.CanFireNowSub(parms)) return false;

            Map map = (Map)parms.target;
            if (map == null) return false;

            return FindCorpseEpicenters(map).Any();
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            Map map = (Map)parms.target;
            if (map == null) return false;

            List<Corpse> epicenters = FindCorpseEpicenters(map);
            if (epicenters.Count == 0) return false;

            int plantsSpawned = 0;
            IntVec3 lastSpawnCell = IntVec3.Invalid;

            foreach (var corpse in epicenters.InRandomOrder()) {
                if (plantsSpawned >= MaxPlantsToSpawn) break;

                if (CellFinder.TryFindRandomCellNear(corpse.Position, map, 2, c => IsValidForBloodPlant(c, map), out IntVec3 spawnCell)) {
                    ThingDef plantDef = ThingDef.Named("lotr_CorpseLily");
                    if (plantDef != null) {
                        Plant newPlant = (Plant)GenSpawn.Spawn(plantDef, spawnCell, map, WipeMode.Vanish);
                        newPlant.Growth = 0.05f;

                        FleckMaker.Static(spawnCell, map, FleckDefOf.ExplosionFlash, 0.6f);

                        lastSpawnCell = spawnCell;
                        plantsSpawned++;
                    }
                }
            }

            if (plantsSpawned > 0) {
                if (PawnUtility.ShouldSendNotificationAbout(map.mapPawns.FreeColonists.FirstOrDefault())) {
                    Messages.Message("Трупы привлекли опасную флору.", new TargetInfo(lastSpawnCell, map), MessageTypeDefOf.PositiveEvent, true);
                }
                return true;
            }

            return false;
        }

        // Вспомогательный метод для поиска кучи трупов
        private List<Corpse> FindCorpseEpicenters(Map map) {
            List<Corpse> result = new List<Corpse>();

            // Собираем все трупы на земле
            List<Corpse> allCorpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse)
                .Cast<Corpse>()
                .Where(c => c != null && c.Spawned)
                .ToList();

            if (allCorpses.Count < MinCorpsesInCluster) return result;

            foreach (var corpse in allCorpses) {
                // Считаем соседей в радиусе
                int count = allCorpses.Count(other => other.Position.InHorDistOf(corpse.Position, SearchRadius));
                if (count >= MinCorpsesInCluster) {
                    result.Add(corpse);
                }
            }

            return result;
        }

        private bool IsValidForBloodPlant(IntVec3 cell, Map map) {
            if (!cell.InBounds(map)) return false;
            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain == null || terrain.IsWater) return false;

            List<Thing> thingList = cell.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++) {
                if (thingList[i].def.category == ThingCategory.Plant || thingList[i].def.category == ThingCategory.Building) {
                    return false;
                }
            }
            return true;
        }
    }

    // ========== Огненная саламандра (7): пустыня ==========
    public class IncidentWorker_FireSalamanderRuins : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("FireSalamanderRuins_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("FireSalamanderRuins_World");
    }

    public class SitePartWorker_FireSalamanderRuins : SitePartWorker { }

    // ========== Магмовый эльф (7): Странный вулкан ==========
    public class IncidentWorker_StrangeVolcano : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("StrangeVolcano_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("StrangeVolcano_World");
    }

    public class SitePartWorker_StrangeVolcano : SitePartWorker { }

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

    public class SitePartWorker_DesertRuins : SitePartWorker { }

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

    public class SitePartWorker_JungleCabin : SitePartWorker { }

    // ========== Магматический гигант (4): странный вулкан ==========
    public class IncidentWorker_MagmaGiantVolcano : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("MagmaGiantVolcano_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("MagmaGiantVolcano_World");
    }

    public class SitePartWorker_MagmaGiantVolcano : SitePartWorker { }

    // ========== Камень катастроф (4): Заброшенный храм ==========

    public class IncidentWorker_StoneofCatastropheSanctuary : IncidentWorker_WorldSiteBase {
        protected override SitePartDef GetSitePartDef() => DefDatabase<SitePartDef>.GetNamed("StoneofCatastropheSanctuary_Site");
        protected override WorldObjectDef GetWorldObjectDef() => DefDatabase<WorldObjectDef>.GetNamed("StoneofCatastropheSanctuary_World");
    }

    public class SitePartWorker_StoneofCatastropheSanctuary : SitePartWorker {
        public override void PostMapGenerate(Map map) {
            if (map.IsPlayerHome) return;
            if (!(map.Parent is Site site)) return;
            var def = DefDatabase<SitePartDef>.GetNamed("StoneofCatastropheSanctuary_Site");
            SitePart sitePart = site.parts.FirstOrDefault(p => p.def == def);
            if (sitePart == null) return;

            var param = new SanctuaryLabyrinthParams();
            param.roomsPerSide = 5;
            param.spiritsPerRoom = new IntRange(3, 4);
            param.spiritChancePerRoom = 0.8f;
            param.addBossSpirit = true;
            param.lootDef = ThingDef.Named("lotr_StoneofCatastrophe");
            param.lootCount = 1;
            param.mysticalComponentsCount = 3;
            param.sideLootChance = 0.6f;
            param.wallStuff = ThingDefOf.BlocksGranite;
            param.floorTerrain = TerrainDef.Named("AncientTile");
            param.useDoors = false;
            param.useFog = true;

            SanctuaryLabyrinthGenerator.Generate(map, param);
        }
    }
}
