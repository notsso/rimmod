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

    // Класс-контейнер для настроек конкретной способности внутри Hediff
    public class AbilityModConfig {
        public AbilityDef abilityDef;

        // Используем nullable-типы (float?), чтобы понимать, задали мы значение в XML или нет
        public float? rangeOverride = null;
        public int? damageOverride = null;
        public float? explosionRadiusOverride = null;
        public float? speedOverride = null;
        public float? warmupOverride = null;
    }

    // Свойства компонента (то, что считывается напрямую из XML)
    public class HediffCompProperties_AbilityModifier : HediffCompProperties {
        public List<AbilityModConfig> modifiers = new List<AbilityModConfig>();

        public HediffCompProperties_AbilityModifier() {
            this.compClass = typeof(HediffComp_AbilityModifier);
        }
    }

    // Сам компонент, который будет висеть на пешке вместе с Hediff
    public class HediffComp_AbilityModifier : HediffComp {
        public HediffCompProperties_AbilityModifier Props => (HediffCompProperties_AbilityModifier)props;

        // Вспомогательный метод для быстрого поиска настроек конкретной способности
        public AbilityModConfig GetConfigFor(AbilityDef ability) {
            if (Props.modifiers == null) return null;

            for (int i = 0; i < Props.modifiers.Count; i++) {
                if (Props.modifiers[i].abilityDef == ability) {
                    return Props.modifiers[i];
                }
            }
            return null;
        }
    }

    public static class LotrUtils {
        public static AbilityModConfig GetActiveModConfig(Pawn pawn, AbilityDef ability) {
            if (pawn?.health?.hediffSet?.hediffs == null) return null;

            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++) {
                var comp = hediffs[i].TryGetComp<HediffComp_AbilityModifier>();
                if (comp != null) {
                    var config = comp.GetConfigFor(ability);
                    if (config != null) return config;
                }
            }
            return null;
        }
    }

    public class Verb_CastAbility_Custom : Verb_CastAbility {
        public override float EffectiveRange {
            get {
                // Проверяем, кто кастует и какая способность активна
                if (CasterPawn != null && this.ability != null) {
                    // Ищем кастомный XML-конфиг из Hediff
                    var config = LotrUtils.GetActiveModConfig(CasterPawn, this.ability.def);
                    if (config != null && config.rangeOverride.HasValue) {
                        return config.rangeOverride.Value; // Возвращаем дистанцию из XML Hediff'а
                    }
                }

                // Если конфига нет, возвращаем стандартную дальность, заложенную в родителе
                return base.EffectiveRange;
            }
        }

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventPreTargetingCast = false, bool allowUnreachable = false) {
            if (this.verbProps == null) {
                return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventPreTargetingCast, allowUnreachable);
            }

            // Запоминаем оригинальные ванильные значения из XML
            float originalWarmup = this.verbProps.warmupTime;
            float originalSpeed = -1f;
            ThingDef projDef = this.verbProps.defaultProjectile;

            if (CasterPawn != null && this.ability != null) {
                var config = LotrUtils.GetActiveModConfig(CasterPawn, this.ability.def);
                if (config != null) {
                    // Подменяем время подготовки, если задано в Hediff
                    if (config.warmupOverride.HasValue) {
                        this.verbProps.warmupTime = config.warmupOverride.Value;
                    }

                    // Подменяем скорость снаряда прямо внутри его DefProperties перед вылетом
                    if (config.speedOverride.HasValue && projDef?.projectile != null) {
                        originalSpeed = projDef.projectile.speed;
                        projDef.projectile.speed = config.speedOverride.Value;
                    }
                }
            }

            // Запускаем ванильный процесс каста и спавна снаряда
            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventPreTargetingCast, allowUnreachable);

            // СРАЗУ ЖЕ возвращаем все исходные значения на место, чтобы не сломать глобальную базу данных игры
            this.verbProps.warmupTime = originalWarmup;
            if (originalSpeed > 0f && projDef?.projectile != null) {
                projDef.projectile.speed = originalSpeed;
            }

            return result;
        }

        // 3. Динамическая скорость полета снаряда
        // Этот метод вызывается игрой ровно в тот фрейм, когда пешка докастовала способность и снаряд вылетает на карту
        protected override bool TryCastShot() {
            if (this.verbProps == null) {
                return base.TryCastShot();
            }

            float originalSpeed = -1f;
            ThingDef projDef = this.verbProps.defaultProjectile;

            if (CasterPawn != null && this.ability != null) {
                var config = LotrUtils.GetActiveModConfig(CasterPawn, this.ability.def);
                if (config != null && config.speedOverride.HasValue && projDef?.projectile != null) {
                    // Запоминаем оригинальную скорость
                    originalSpeed = projDef.projectile.speed;
                    // Подменяем скорость прямо в момент генерации снаряда игрой
                    projDef.projectile.speed = config.speedOverride.Value;
                }
            }

            // Вызываем ванильный спавн и полет снаряда
            bool result = base.TryCastShot();

            // Сразу после создания снаряда возвращаем базовую скорость в XML на место
            if (originalSpeed > 0f && projDef?.projectile != null) {
                projDef.projectile.speed = originalSpeed;
            }

            return result;
        }
    }

    public class Projectile_ExplosiveCustom : Projectile_Explosive {
        // Храним оригинальную скорость дефа, чтобы вернуть её назад, когда снаряд исчезнет
        private float originalSpeed = -1f;

        // Идеально повторяем вашу 8-аргументную сигнатуру со скриншота
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null) {
            if (launcher is Pawn pawn) {
                AbilityDef castingAbility = null;
                var allAbilities = DefDatabase<AbilityDef>.AllDefsListForReading;

                for (int i = 0; i < allAbilities.Count; i++) {
                    var comps = allAbilities[i].comps;
                    if (comps == null) continue;

                    for (int j = 0; j < comps.Count; j++) {
                        if (comps[j] is CompProperties_AbilityLaunchProjectile prop && prop.projectileDef?.defName == this.def?.defName) {
                            castingAbility = allAbilities[i];
                            break;
                        }
                    }
                    if (castingAbility != null) break;
                }

                if (castingAbility != null) {
                    var config = LotrUtils.GetActiveModConfig(pawn, castingAbility);
                    if (config != null && config.speedOverride.HasValue) {
                        // Запоминаем оригинальную ванильную скорость XML-дефа (например, 50)
                        this.originalSpeed = this.def.projectile.speed;

                        // Меняем скорость в дефе на ВСЁ время, пока снаряд находится в воздухе
                        this.def.projectile.speed = config.speedOverride.Value;
                    }
                }
            }

            // Вызываем базовый запуск. Теперь игра на протяжении ВСЕГО полета видит 
            // кастомную скорость (150). Визуал и физика просчитаются плавно, без рывков!
            base.Launch(
                launcher: launcher,
                origin: origin,
                usedTarget: usedTarget,
                intendedTarget: intendedTarget,
                hitFlags: hitFlags,
                preventFriendlyFire: preventFriendlyFire,
                equipment: equipment,
                targetCoverDef: targetCoverDef
            );
        }

        // Переопределяем метод удаления снаряда с карты (срабатывает сразу после CustomExplode)
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
            // ЖЕЛЕЗНЫЙ СБРОС: Как только снаряд взорвался или исчез, немедленно возвращаем 
            // дефолтную скорость обратно в деф, чтобы не сломать баллистику для других пешек.
            if (this.originalSpeed > 0f) {
                this.def.projectile.speed = this.originalSpeed;
            }

            base.Destroy(mode);
        }

        protected override void Explode() {
            int finalDamage = this.def.projectile.GetDamageAmount(this, null);
            float finalArmorPenetration = this.def.projectile.GetArmorPenetration(this, null);
            float finalRadius = this.def.projectile.explosionRadius;

            if (this.launcher is Pawn pawn) {
                AbilityDef castingAbility = null;

                foreach (var abilityDef in DefDatabase<AbilityDef>.AllDefsListForReading) {
                    if (abilityDef.comps != null) {
                        for (int j = 0; j < abilityDef.comps.Count; j++) {
                            if (abilityDef.comps[j] is CompProperties_AbilityLaunchProjectile prop && prop.projectileDef?.defName == this.def?.defName) {
                                castingAbility = abilityDef;
                                break;
                            }
                        }
                    }
                    if (castingAbility != null) break;
                }

                if (castingAbility != null) {
                    var config = LotrUtils.GetActiveModConfig(pawn, castingAbility);
                    if (config != null) {
                        if (config.damageOverride.HasValue)
                            finalDamage = config.damageOverride.Value;

                        if (config.explosionRadiusOverride.HasValue)
                            finalRadius = config.explosionRadiusOverride.Value;
                    }
                }
            }

            GenExplosion.DoExplosion(
                this.Position,
                this.Map,
                finalRadius,                               // Наш измененный радиус
                this.def.projectile.damageDef,
                this.launcher,
                finalDamage,                               // Наш измененный урон
                finalArmorPenetration,                     // Наше пробитие брони
                this.def.projectile.soundExplode,
                this.equipmentDef,
                this.def,
                this.intendedTarget.Thing,
                this.def.projectile.postExplosionSpawnThingDef,
                this.def.projectile.postExplosionSpawnChance,
                this.def.projectile.postExplosionSpawnThingCount,
                null, // postExplosionGasType
                null, // postExplosionGasRadiusOverride
                255,  // postExplosionGasAmount
                this.def.projectile.applyDamageToExplosionCellsNeighbors,
                this.def.projectile.preExplosionSpawnThingDef,
                this.def.projectile.preExplosionSpawnChance,
                this.def.projectile.preExplosionSpawnThingCount,
                this.def.projectile.explosionChanceToStartFire,
                this.def.projectile.explosionDamageFalloff
            );

            if (!this.Destroyed) {
                this.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
