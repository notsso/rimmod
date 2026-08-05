using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // класс, для зелий потусторонних, которые продвигают
    public class IngestionOutcomeDoer_SequenceAdvance : IngestionOutcomeDoer {
        // Поля будут настраиваться через XML
        public HediffDef hediffToRemove; // Что ищем
        public HediffDef hediffToGive; // На что меняем
        public float severity;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount) {
            if (pawn == null) return;

            // Ищем старый Hediff
            Hediff oldHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(hediffToRemove);

            if (oldHediff != null && oldHediff.Severity >= 1.0f) {
                // Если нашли: удаляем его
                pawn.health.RemoveHediff(oldHediff);

                // И добавляем новый
                Hediff newHediff = HediffMaker.MakeHediff(hediffToGive, pawn);
                newHediff.Severity = severity;
                pawn.health.AddHediff(newHediff);

                // Сообщение игроку
                Messages.Message($"{pawn.LabelShort} успешно продвинулся.", pawn, MessageTypeDefOf.PositiveEvent);
            } else {
                pawn.Kill(null);

                // Сообщение о смерти
                Messages.Message($"{pawn.LabelShort} погиб, выпив зелье без подготовки!", TargetInfo.Invalid, MessageTypeDefOf.NegativeEvent);
            }
        }
    }

    public class Projectile_GlowingExplosive : Projectile_Explosive {
        // Переменная для отслеживания последней клетки, где мы обновили свет
        private IntVec3 lastLightPosition = IntVec3.Invalid;

        protected override void Tick() {
            base.Tick();

            // Проверяем, что снаряд летит и находится на карте
            if (this.Spawned && !this.Destroyed) {
                // Если снаряд перелетел в новую клетку
                if (this.Position != lastLightPosition) {
                    lastLightPosition = this.Position;

                    // Получаем компонент свечения, который прикреплен к нашему снаряду
                    CompGlower glower = this.GetComp<CompGlower>();

                    if (glower != null) {
                        // Ванильный и безопасный способ заставить карту перерисовать свет:
                        // Мы принудительно выключаем и включаем свет обратно. 
                        // Игра сама сотрет старое световое пятно и нарисует его в текущей позиции снаряда.
                        this.Map.glowGrid.DeRegisterGlower(glower);
                        this.Map.glowGrid.RegisterGlower(glower);
                    }
                }
            }
        }
    }
}