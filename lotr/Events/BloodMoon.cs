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
    public class GameCondition_BloodMoon : GameCondition {
        public override void Init() {
            base.Init();
            // Можно сразу добавить всем потусторонним пешкам sanity loss, если его нет
        }

        public override SkyTarget? SkyTarget(Map map) {
            Color skyColor = new Color(0.75f, 0.05f, 0.05f);
            Color shadowColor = new Color(0.3f, 0.02f, 0.02f);
            Color overlayColor = new Color(0.5f, 0.0f, 0.0f);
            SkyColorSet colorSet = new SkyColorSet(skyColor, shadowColor, overlayColor, 1f);

            // glow, colorSet, lightsourceShineSize, lightsourceShineIntensity
            return new SkyTarget(0.9f, colorSet, 0.4f, 0.6f);
        }

        public override float SkyTargetLerpFactor(Map map) => 1f;
    }

    public class IncidentWorker_BloodMoon : IncidentWorker {
        protected override bool CanFireNowSub(IncidentParms parms) {
            // Если цель не задана (ручной вызов), подставляем текущую карту
            if (parms.target == null) {
                parms.target = Find.CurrentMap;
            }


            Map map = parms.target as Map;
            if (map == null) {
                return false;
            }

            if (!base.CanFireNowSub(parms)) {
                return false;
            }

            // ночь с 21 до 6
            int hour = GenLocalDate.HourOfDay(map);
            if (!(hour >= 21 || hour < 6)) {
                return false;
            }

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms) {
            Map map = (parms.target as Map) ?? Find.CurrentMap;
            if (map == null)
                return false;

            int durationTicks = Mathf.RoundToInt(0.3f * 60000); // около 7 игровых часов
            GameCondition condition = GameConditionMaker.MakeCondition(
                DefDatabase<GameConditionDef>.GetNamed("BloodMoon"), durationTicks);
            map.gameConditionManager.RegisterCondition(condition);

            Messages.Message("Кровавая Луна взошла над поселением!", MessageTypeDefOf.NegativeEvent);
            return true;
        }
    }
}
