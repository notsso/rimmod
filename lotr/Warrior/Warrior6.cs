using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 6
    public class Warrior6_Hediff : Beyonder_Hediff {

        public Warrior6_Hediff() {
            // способы действия: Заговоры (враги дерутся сами с собой)
            maxProgressPerCategory = 0.8f;

            // анти-действия: Использование грубой силы (blazing spear)
        }
    }

    public class CompProperties_SunsetLightEffect : CompProperties_AbilityEffect {
        public float radius = 40f;
        public CompProperties_SunsetLightEffect() => compClass = typeof(Comp_SunsetLightEffect);
    }

    public class Comp_SunsetLightEffect : CompAbilityEffect{
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Map map = caster?.Map;
            if (map == null) return;

            float rad = ((CompProperties_SunsetLightEffect)props).radius;

            // Правильный ванильный перебор клеток по кругу
            foreach (IntVec3 c in GenRadial.RadialCellsAround(caster.Position, rad, useCenter: true)) {
                if (!c.InBounds(map)) continue;

                Pawn pawnAt = c.GetFirstPawn(map); // Получаем пешку на этой клетке, если она есть
                if (pawnAt != null && pawnAt != caster && pawnAt.HostileTo(caster)) { // TODO: обработка всяких нечистей
                    // Накладываем ослабление на враждебного злого духа / потустороннее существо
                    HealthUtility.AdjustSeverity(pawnAt, HediffDefOf.PsychicShock, 0.15f);
                }
            }

            // MoteMaker.MakeStaticMote(caster.Position, map, ThingDefOf.Mote_ExplosionFlash, 3f);
            Messages.Message("Свет заката озарил поле боя, рассеивая мрачные тени!", caster, MessageTypeDefOf.PositiveEvent);
        }
    }

}
