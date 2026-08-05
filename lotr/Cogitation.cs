using System.Collections.Generic;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

using HarmonyLib;

namespace lotr {
    public class JobDriver_Cogitation : JobDriver {
        private const int DurationTicks = 2000;

        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils() {
            // Создаем фазу действия (Toil)
            Toil cogitate = ToilMaker.MakeToil();

            // Действие при старте: принудительно останавливаем пешку на месте
            cogitate.initAction = delegate {
                pawn.pather.StopDead();
            };

            // Настройки поведения и анимации
            cogitate.handlingFacing = true; // Разрешаем пешке менять направление взгляда
            cogitate.defaultCompleteMode = ToilCompleteMode.Delay; // Работа завершится по истечении таймера
            cogitate.defaultDuration = DurationTicks;

            // Каждый игровой тик (60 раз в секунду) выполняем внутреннюю логику
            cogitate.tickAction = delegate {
                Pawn p = pawn;
                if (p == null || p.Destroyed || !p.Spawned) return;

                // Медленно снижаем Искажение Разума (Sanity Loss)
                HediffDef sanityDef = LotrDefOf.lotr_SanityLoss;
                Hediff sanityHediff = p.health.hediffSet.GetFirstHediffOfDef(sanityDef);
                if (sanityHediff != null) {
                    sanityHediff.Severity -= 0.00005f;
                }

                // Медленно восстанавливаем Духовность (Spirituality Need)
                Need_Spirituality spiritualityNeed = p.needs?.TryGetNeed<Need_Spirituality>();
                if (spiritualityNeed != null) {
                    // Просто увеличиваем уровень через публичное свойство
                    float newLevel = Mathf.Min(spiritualityNeed.MaxLevel, spiritualityNeed.CurLevel + 0.0001f);
                    spiritualityNeed.CurLevel = newLevel;
                }

                // Заставляем пешку принять позу медитации (сидя на полу)
                p.Rotation = Rot4.South; // Всегда лицом к игроку во время транса

                // Визуальный эффект: раз в секунду испускаем мягкие психо-волны над головой
                if (p.IsHashIntervalTick(60) && p.Map != null) {
                    FleckMaker.ThrowMetaIcon(p.Position, p.Map, FleckDefOf.PsycastAreaEffect);
                }
            };

            // Указываем игре, что во время этой работы пешка совершает "Медитацию" 
            // (это подтянет правильные анимации тела из ядра RimWorld)
            cogitate.WithEffect(EffecterDefOf.Research, TargetIndex.A);

            yield return cogitate;
        }
    }
}