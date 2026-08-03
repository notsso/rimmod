using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Специфичная логика Охотника
    public class Hunter9_Hediff : Beyonder_Hediff {
        public override float SpiritualityFactor => 1.2f;

        // private int ticksCounter = 0;

        public override void Tick() {
            base.Tick();

            // Специфичная логика Охотника: регенерация ран
            /*
            ticksCounter++;
            if (ticksCounter >= 180) {
                ticksCounter = 0;
                TryHealWounds();
            }*/
        }

        // disabled, for now
        private void TryHealWounds() {
            if (this.pawn == null || this.pawn.health == null) return;

            float healAmount = 0.1f;
            if (this.CurStageIndex == 1) healAmount = 0.2f;
            if (this.CurStageIndex == 2) healAmount = 0.3f;

            if (healAmount <= 0f) return;

            List<Hediff_Injury> injuries = this.pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(x => x.Severity > 0f)
                .ToList();

            if (injuries.Any()) {
                Hediff_Injury worstInjury = injuries.OrderByDescending(x => x.Severity).First();
                worstInjury.Severity -= healAmount;
            }
        }
    }

    // Harmony patch - отслеживает 'действие' охотника
    [HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
    public static class Patch_Pawn_JobTracker_EndCurrentJob {
        private static float factor { get; } = 0.1f;

        [HarmonyPrefix]
        public static void Prefix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob, bool canReturnToPool, ref float __state) {
            __state = 0.0f;

            if (__instance.curJob != null && __instance.curJob.def == JobDefOf.Hunt && condition == JobCondition.Succeeded) {
                __state = 1.0f;

                if (__instance.curJob.targetA.Thing is Pawn victim) {
                    __state = victim.RaceProps.baseBodySize; // в зависимости от размера добычи, усвоение меняется
                }
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob, bool canReturnToPool, ref float __state, Pawn ___pawn) {
            if (__state > 0.01f && ___pawn != null && ___pawn.IsColonist) {
                Hediff hediff = ___pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Hunter9_Hediff"));

                if (hediff != null) {
                    float victimBodySize = __state;
                    float severityIncrement = factor * victimBodySize;
                    severityIncrement = Mathf.Clamp(severityIncrement, 0.02f, 0.40f);

                    hediff.Severity += severityIncrement;

                    string messageText = $"После действия, {___pawn.LabelShortCap} усвоил свое зелье на {severityIncrement.ToStringPercent()}!";

                    Messages.Message(messageText, ___pawn, MessageTypeDefOf.SilentInput, historical: false);
                }
            }
        }
    }

    public class Hunter8_Hediff : Hunter9_Hediff {
        public override float SpiritualityFactor => 1.5f;
    }

    // абилка провокация
    public class CompAbilityEffect_Provoke : CompAbilityEffect {
        // Получаем доступ к настройкам из XML (если нужно)
        public new CompProperties_AbilityProvoke Props => (CompProperties_AbilityProvoke)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);

            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn == null || targetPawn.Dead || targetPawn.Downed) {
                return;
            }

            if (targetPawn.Faction == caster.Faction) {
                return;
            }

            ProvokePawn(targetPawn, caster);

            if (targetPawn.RaceProps.ToolUser || targetPawn.RaceProps.IsMechanoid) {
                if (caster.health?.hediffSet?.hediffs != null) {
                    foreach (var hediff in caster.health.hediffSet.hediffs) {
                        if (hediff is Beyonder_Hediff beyonderHediff) {
                            float severityIncrement = 0.05f;
                            float oldSeverity = beyonderHediff.Severity;
                            beyonderHediff.Severity += severityIncrement;

                            float diff = beyonderHediff.Severity - oldSeverity;
                            if (diff > 0.0f) {
                                string messageText = $"{caster.LabelShortCap} успешно спровоцировал врага! Зелье усвоено на {diff.ToStringPercent()}.";
                                Messages.Message(messageText, caster, MessageTypeDefOf.SilentInput, historical: false);
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void ProvokePawn(Pawn victim, Pawn aggressor) {
            if (victim == null || aggressor == null) return;

            victim.jobs.StopAll();

            victim.mindState.enemyTarget = aggressor;

            Job tauntJob = JobMaker.MakeJob(JobDefOf.AttackMelee, aggressor);

            tauntJob.expiryInterval = 600;
            tauntJob.checkOverrideOnExpire = true;
            tauntJob.playerForced = true;

            victim.jobs.StartJob(tauntJob, JobCondition.InterruptForced, null, false, true);

            MoteMaker.ThrowText(victim.DrawPos, victim.Map, "Provoked!", 3f);
        }
    }

    // Класс свойств для связи с XML
    public class CompProperties_AbilityProvoke : CompProperties_AbilityEffect {
        public CompProperties_AbilityProvoke() {
            compClass = typeof(CompAbilityEffect_Provoke);
        }
    }

    public class Hunter7_Hediff : Hunter8_Hediff {
        public override float SpiritualityFactor => 5f;
    }

    public class Projectile_PenetratingExplosive : Projectile {
        // Переменная-счетчик для оптимизации спавна эффектов
        private int tickCounter = 0;

        protected override void Tick() {
            base.Tick();

            // Проверяем, что снаряд на карте и летит
            if (this.Spawned && !this.Destroyed) {
                tickCounter++;

                // Спавним искру каждые 2 тика (чтобы шлейф был плотным, но не лагал)
                if (tickCounter % 2 == 0) {
                    // Бросаем ванильную зажигательную искру прямо в текущей координате снаряда
                    FleckMaker.ThrowSmoke(this.ExactPosition, this.Map, 0.8f); // Легкий дымок

                    // FleckDefOf.ThermalGlow — это те самые тепловые искры пламени
                    FleckMaker.Static(this.ExactPosition, this.Map, FleckDefOf.MicroSparks, 1.0f);
                }
            }
        }

        protected override void Impact(Thing hitThing, bool maskedByFlame = false) {
            if (hitThing != null) {
                // Проверяем, является ли цель пешкой (живым существом/механоидом)
                Pawn hitPawn = hitThing as Pawn;

                // Получаем базовые параметры урона и пробития из XML
                float baseDamage = (float)this.def.projectile.GetDamageAmount(this.launcher);
                float baseArmorPenetration = this.def.projectile.GetArmorPenetration(this.launcher);

                // Физический порез/царапина (Cut) 
                DamageInfo cutDinfo = new DamageInfo(
                    DamageDefOf.Cut,                         // Тип урона: Порез (как от стрелы/меча)
                    baseDamage,                              // Урон берется из XML снаряда
                    baseArmorPenetration,                    // Пробитие берется из XML снаряда
                    this.ExactRotation.eulerAngles.y,
                    this.launcher,
                    null,
                    this.equipmentDef,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    this.intendedTarget.Thing
                );
                hitThing.TakeDamage(cutDinfo);

                // Термический ожог (Burn)
                DamageInfo burnDinfo = new DamageInfo(
                    DamageDefOf.Burn,                        // Тип урона: Ожог
                    baseDamage * 0.5f,                       // Можно сделать ожог чуть слабее (например, 50% от базы)
                    baseArmorPenetration,
                    this.ExactRotation.eulerAngles.y,
                    this.launcher,
                    null,
                    this.equipmentDef,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    this.intendedTarget.Thing
                );
                hitThing.TakeDamage(burnDinfo);

                // Heatstroke)
                if (hitPawn != null && hitPawn.RaceProps.FleshType == FleshTypeDefOf.Normal) {
                    // Ищем, есть ли уже у цели тепловой удар
                    Hediff heatstroke = hitPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);

                    if (heatstroke != null) {
                        // Если есть — увеличиваем его тяжесть (например, на +20%)
                        heatstroke.Severity += 0.25f;
                    } else {
                        // Если нет — создаем новый тепловой удар с начальной тяжестью 25%
                        hitPawn.health.AddHediff(HediffDefOf.Heatstroke);
                        Hediff newHeatstroke = hitPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
                        if (newHeatstroke != null) {
                            newHeatstroke.Severity = 0.25f;
                        }
                    }
                }
            }

            // Взрыв по площади
            if (this.def.projectile.explosionRadius > 0f) {
                int explosionDamage = this.def.projectile.GetDamageAmount(this.launcher);
                float explosionArmorPenetration = this.def.projectile.GetArmorPenetration(this.launcher);

                GenExplosion.DoExplosion(
                    this.Position,
                    this.Map,
                    this.def.projectile.explosionRadius,
                    this.def.projectile.damageDef, // Взрыв оставим с типом урона из XML (Burn)
                    this.launcher,
                    explosionDamage,
                    explosionArmorPenetration,
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
            }

            base.Impact(hitThing, maskedByFlame);
        }
    }
}
