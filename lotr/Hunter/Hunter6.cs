using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Последовательность 6 - Заговорщик
    public class Hunter6_Hediff : Beyonder_Hediff {

        public Hunter6_Hediff() {
            // способы действия: Заговоры (враги дерутся сами с собой)
            maxProgressPerCategory = 0.8f;

            // анти-действия: Использование грубой силы (blazing spear)
        }
    }

    public class PawnFlyer_FireTeleport : PawnFlyer {
        public float customSpeed = 50f;

        // Устанавливает скорость и пересчитывает время полёта.
        public void SetCustomSpeed(float speed) {
            customSpeed = speed;
            float distance = Vector3.Distance(startVec, DestinationPos);
            float minSeconds = def.pawnFlyer.flightDurationMin;
            float flightSeconds = Mathf.Max(distance / speed, minSeconds);
            ticksFlightTime = Mathf.Max(1, flightSeconds.SecondsToTicks());
            ticksFlying = 0;
        }

        // Основная отрисовка: шар вместо пешки, с учётом поворота
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false) {
            if (Graphic != null) {
                // Если нужно повернуть, можно использовать матрицу, но для сферы не критично
                Graphic.Draw(drawLoc, Rotation, this, 0f);
            }
        }

        // Лёгкое свечение и искры во время полёта
        protected override void Tick() {
            base.Tick();
            if (this.IsHashIntervalTick(3)) {
                // Искры
                FleckMaker.Static(this.DrawPos, Map, FleckDefOf.MicroSparks, 1.0f);
                // Мягкий свет
                // FleckMaker.ThrowLightingGlow(this.DrawPos, Map, 2.5f);
            }
        }
    }

    public class CompProperties_FireJump : CompProperties_AbilityEffect {
        public ThingDef flyerDef;
        public float teleportSpeed = 50f;

        public CompProperties_FireJump() {
            compClass = typeof(CompAbilityEffect_FireJump);
        }
    }

    public class CompAbilityEffect_FireJump : CompAbilityEffect {
        public new CompProperties_FireJump Props => (CompProperties_FireJump)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            Pawn caster = parent.pawn;
            if (caster == null || !target.Cell.IsValid)
                return;

            Map map = caster.Map;
            IntVec3 startPos = caster.Position;

            // Запрещаем телепорт на свою текущую клетку
            if (target.Cell == caster.Position) {
                Messages.Message("Cannot teleport to the same tile.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!JumpUtility.ValidJumpTarget(caster, map, target.Cell)) {
                Messages.Message("Invalid jump target.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            ThingDef flyerDef = Props.flyerDef;
            if (flyerDef == null) {
                Log.Error("FireJump: flyerDef is null.");
                return;
            }

            SoundDef soundLanding = SoundDefOf.Click; // или свой

            PawnFlyer flyer = PawnFlyer.MakeFlyer(
                flyerDef,
                caster,
                target.Cell,
                null,
                soundLanding,
                false,
                null,
                parent,
                target
            );

            if (flyer == null)
                return;

            // Устанавливаем поворот в направлении полёта
            Vector3 direction = (target.Cell.ToVector3Shifted() - startPos.ToVector3Shifted()).normalized;
            flyer.Rotation = Rot4.FromAngleFlat(direction.AngleFlat());

            // Спавним летуна
            GenSpawn.Spawn(flyer, target.Cell, map, WipeMode.Vanish);

            // Если подкласс – задаём скорость (с защитой от нуля)
            var fireFlyer = flyer as PawnFlyer_FireTeleport;
            if (fireFlyer != null)
                fireFlyer.SetCustomSpeed(Props.teleportSpeed);

            // Сохраняем выделение
            bool wasSelected = Find.Selector.IsSelected(caster);
            if (wasSelected)
                Find.Selector.Select(caster, false, false);

            // Эффекты в начальной точке
            FleckMaker.ThrowSmoke(startPos.ToVector3Shifted(), map, 0.8f);
            FleckMaker.Static(startPos, map, FleckDefOf.MicroSparks, 1.0f);
        }
    }
}
