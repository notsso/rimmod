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

                // 1. Медленно снижаем Искажение Разума (Sanity Loss)
                // HediffDef sanityDef = DefDatabase<HediffDef>.GetNamed("lotr_SanityLoss", false);
                HediffDef sanityDef = LotrDefOf.lotr_SanityLoss;
                Hediff sanityHediff = p.health.hediffSet.GetFirstHediffOfDef(sanityDef);
                if (sanityHediff != null) {
                    sanityHediff.Severity -= 0.00005f;
                }

                // 2. Медленно восстанавливаем Духовность (Spirituality Need)
                // NeedDef spiritualityDef = DefDatabase<NeedDef>.GetNamed("lotr_SpiritualityNeed", false);
                NeedDef spiritualityDef = LotrDefOf.lotr_SpiritualityNeed;
                if (spiritualityDef != null) {
                    Need spiritualityNeed = p.needs?.TryGetNeed(spiritualityDef);
                    if (spiritualityNeed != null) {
                        // Берем текущий уровень из приватного поля curLevelInt
                        float curLevel = AccessTools.Field(typeof(Need), "curLevelInt").GetValue(spiritualityNeed) is float v ? v : spiritualityNeed.CurLevel;

                        // Рассчитываем прирост (за сеанс восстановит около 15-20% шкалы маны)
                        float nextLevel = Mathf.Min(spiritualityNeed.MaxLevel, curLevel + 0.0001f);

                        // Записываем обратно напрямую
                        AccessTools.Field(typeof(Need), "curLevelInt").SetValue(spiritualityNeed, nextLevel);
                    }
                }

                // 3. Заставляем пешку принять позу медитации (сидя на полу)
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