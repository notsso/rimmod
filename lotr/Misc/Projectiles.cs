using System;
using System.Collections.Generic;
using System.Linq;

using Verse;
using Verse.Sound;

using UnityEngine;

using RimWorld;

namespace lotr {
    public abstract class ProjectileEffect {
        public ThingDef equipmentDef;

        // Применить до проверки щита. Возвращает false, если снаряд должен прекратить обработку (например, уже уничтожен).
        public virtual bool ApplyBeforeShield(Projectile_Base projectile, Thing hitThing) => true;

        // Применить после щита (если щит не заблокировал или снаряд проходит сквозь).
        public virtual void ApplyAfterShield(Projectile_Base projectile, Thing hitThing) { }

        // метод преимущественно для визуальных эффектов
        public virtual void OnTick(Projectile_Base projectile) { }
        protected ThingDef GetEquipmentDef(Projectile_Base projectile) => equipmentDef ?? projectile.EquipmentDef;
    }

    public abstract class Projectile_Base : Projectile {
        public new ThingDef EquipmentDef => equipmentDef;
        protected List<ProjectileEffect> effects = new List<ProjectileEffect>();

        public float currentDamageMultiplier = 1f;
        public float customArmorPenetration = -1f;

        public override int DamageAmount => (int)((float)def.projectile.GetDamageAmount(this, null) * currentDamageMultiplier);
        public override float ArmorPenetration => Mathf.Max(def.projectile.GetArmorPenetration(this, null), customArmorPenetration);
        public float ExplosionRadius => def.projectile.explosionRadius;
        public new DamageDef DamageDef => def.projectile.damageDef;

        public bool IsInvisible = false;
        public int tickCounter = 0;

        public bool InstantKill { get; protected set; } = false;
        public bool InstantKillBonus { get; protected set; } = false;
        public bool VictimKilled { get; protected set; } = false;
        protected float minBodySizeForInstantKillBonus = 2.0f;

        protected override void Tick() {
            base.Tick();

            if (this.Spawned && !this.Destroyed) {
                tickCounter++;
            }

            foreach (var effect in effects)
                effect.OnTick(this);

            OnTick();
        }

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null) {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);

            // Если базовый Launch не заспавнил снаряд — делаем это вручную
            if (!this.Spawned) {
                Log.Warning($"Base.Launch failed for {this.def.defName}, spawning manually.");
                GenSpawn.Spawn(this, origin.ToIntVec3(), launcher.Map);
            }

            GatherEffects();
            OnLaunch();
        }

        protected virtual void GatherEffects() { }

        protected override void Impact(Thing hitThing, bool blockedByShield = false) {
            try {
                Pawn victimPawn = hitThing as Pawn;
                bool wasAlive = victimPawn != null && !victimPawn.Dead;
                bool wasUnharmed = false;
                bool bigEnough = false;

                if (wasAlive) {
                    wasUnharmed = IsPawnNearlyUnharmed(victimPawn);
                    bigEnough = victimPawn.BodySize >= minBodySizeForInstantKillBonus;
                }

                // Эффекты до щита
                foreach (var effect in effects)
                    if (!effect.ApplyBeforeShield(this, hitThing))
                        break;

                if (blockedByShield) {
                    Destroy(DestroyMode.Vanish);
                    return;
                }

                // Эффекты после щита
                foreach (var effect in effects)
                    effect.ApplyAfterShield(this, hitThing);

                if (wasAlive && victimPawn != null && victimPawn.Dead) {
                    VictimKilled = victimPawn.Dead;
                    InstantKill = wasUnharmed;
                    InstantKillBonus = InstantKill && bigEnough;

                    OnKilledByProjectile(victimPawn);
                }

                var launcherReaction = new Effect_LauncherReaction();
                launcherReaction.ApplyAfterShield(this, hitThing);
            } catch (Exception ex) {
                Log.Error($"Exception in Projectile_Base.Impact for {this.def?.defName ?? "null"} at {this.Position}: {ex}");
            } finally {
                if (!this.Destroyed)
                    this.Destroy(DestroyMode.Vanish);
            }
        }

        private bool IsPawnNearlyUnharmed(Pawn pawn) {
            return pawn.health?.summaryHealth?.SummaryHealthPercent > 0.90f;
        }

        protected virtual void OnKilledByProjectile(Pawn victim) {
            // Log.Message($"{this.def.defName} killed {victim.def.defName} with this impact.");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
            OnDestroy();
            base.Destroy(mode);
        }

        public virtual void OnLaunch() { }
        public virtual void OnTick() { }
        public virtual void OnDestroy() { }
    }

    public static class ExplosionHelper {
        public static void DoExplosion(Projectile_Base proj, ThingDef equipmentDef, float? radiusOverride = null, float? chanceToStartFire = null, int? damage = null) {
            float radius = radiusOverride ?? proj.ExplosionRadius;
            float fireChance = chanceToStartFire ?? proj.def.projectile.explosionChanceToStartFire;

            GenExplosion.DoExplosion(
                center: proj.Position,
                map: proj.Map,
                radius: radius,
                damType: proj.DamageDef,
                instigator: proj.Launcher,
                damAmount: damage ?? proj.DamageAmount,
                armorPenetration: proj.ArmorPenetration,
                explosionSound: proj.def.projectile.soundExplode ?? SoundDef.Named("Explosion_Bomb"),
                weapon: equipmentDef,
                projectile: proj.def,
                intendedTarget: proj.intendedTarget.Thing,
                postExplosionSpawnThingDef: proj.def.projectile.postExplosionSpawnThingDef,
                postExplosionSpawnChance: proj.def.projectile.postExplosionSpawnChance,
                postExplosionSpawnThingCount: proj.def.projectile.postExplosionSpawnThingCount,
                postExplosionGasType: null,
                postExplosionGasRadiusOverride: null,
                postExplosionGasAmount: 255,
                applyDamageToExplosionCellsNeighbors: proj.def.projectile.applyDamageToExplosionCellsNeighbors,
                preExplosionSpawnThingDef: proj.def.projectile.preExplosionSpawnThingDef,
                preExplosionSpawnChance: proj.def.projectile.preExplosionSpawnChance,
                preExplosionSpawnThingCount: proj.def.projectile.preExplosionSpawnThingCount,
                chanceToStartFire: fireChance,
                damageFalloff: proj.def.projectile.explosionDamageFalloff
            );
        }
    }

    // Описание эффектов снарядов
    public class Effect_DirectDamage : ProjectileEffect {
        public DamageDef damageDef;
        public int? damageAmountOverride;
        public bool ignoreArmor = false;

        public override void ApplyAfterShield(Projectile_Base projectile, Thing hitThing) {
            if (hitThing == null) {
                return;
            }
            int amount = damageAmountOverride ?? projectile.DamageAmount;
            if (amount <= 0) return;

            var dinfo = new DamageInfo(
                damageDef ?? projectile.DamageDef,
                amount,
                projectile.ArmorPenetration,
                projectile.ExactRotation.eulerAngles.y,
                projectile.Launcher,
                null,
                equipmentDef
            );
            if (ignoreArmor) dinfo.SetIgnoreArmor(true);
            hitThing.TakeDamage(dinfo);
        }
    }

    // Эффект взрыва (после щита)
    public class Effect_Explosion : ProjectileEffect {
        public float? radius;
        public float? fireChance;
        public int? damage;

        public override void ApplyAfterShield(Projectile_Base projectile, Thing hitThing) {
            /*if (projectile.ReaperChargeUsed && hitThing is Pawn)
                return;*/

            float actualRadius = radius ?? projectile.ExplosionRadius;
            ExplosionHelper.DoExplosion(projectile, equipmentDef, radius, fireChance, damage);
        }
    }

    // эффект казни
    public class Effect_ReaperStrike : ProjectileEffect {
        public override bool ApplyBeforeShield(Projectile_Base projectile, Thing hitThing) {
            if (hitThing == null) {
                ExplosionHelper.DoExplosion(projectile, GetEquipmentDef(projectile));
                MoteMaker.ThrowText(projectile.ExactPosition, projectile.Map, "ПОЖИНАНИЕ: РАЗРУШЕНИЕ", 4f);
                return true;
            }

            // Снос щитов (если это пешка)
            if (hitThing is Pawn victim && victim.apparel != null) {
                foreach (Apparel apparel in victim.apparel.WornApparel) {
                    CompShield shield = apparel?.GetComp<CompShield>();
                    if (shield?.parent != null)
                        shield.parent.TakeDamage(new DamageInfo(DamageDefOf.Bomb, 9999f));
                }
            }

            // Сохраняем данные для визуальных эффектов
            Map map = projectile.Map;
            Vector3 loc = projectile.ExactPosition; // позиция снаряда (он в точке удара)

            if (hitThing is Pawn hitPawn) {
                // Собираем жизненно важные части
                List<BodyPartRecord> vitalParts = new List<BodyPartRecord>();
                BodyPartRecord torso = hitPawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Torso)
                    .FirstOrDefault(p => !hitPawn.health.hediffSet.PartIsMissing(p));
                BodyPartRecord neck = hitPawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Neck)
                    .FirstOrDefault(p => !hitPawn.health.hediffSet.PartIsMissing(p));
                BodyPartRecord head = hitPawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Head)
                    .FirstOrDefault(p => !hitPawn.health.hediffSet.PartIsMissing(p));

                if (torso != null) vitalParts.Add(torso);
                if (neck != null) vitalParts.Add(neck);
                if (head != null) vitalParts.Add(head);

                if (vitalParts.Count == 0) vitalParts.Add(null);

                BodyPartRecord targetPart = vitalParts.RandomElement();
                var dinfo = new DamageInfo(
                    projectile.DamageDef,
                    projectile.DamageAmount,
                    projectile.ArmorPenetration,
                    projectile.ExactRotation.eulerAngles.y,
                    projectile.Launcher,
                    targetPart,
                    GetEquipmentDef(projectile)
                );
                dinfo.SetIgnoreArmor(true);
                hitPawn.TakeDamage(dinfo);

                // Эффекты после удара (используем сохранённый Map)
                if (map != null) {
                    MoteMaker.ThrowText(loc, map, "ПОЖИНАНИЕ: КАЗНЬ", 4f);
                    ExplosionHelper.DoExplosion(projectile, GetEquipmentDef(projectile), null, null, 0);
                }
            } else {
                // Не пешка – строение/стена
                float bonus = projectile.DamageAmount * 0.5f;
                var dinfo = new DamageInfo(
                    DamageDefOf.Crush,
                    bonus,
                    projectile.ArmorPenetration,
                    projectile.ExactRotation.eulerAngles.y,
                    projectile.Launcher,
                    null,
                    GetEquipmentDef(projectile)
                );
                dinfo.SetIgnoreArmor(true);
                hitThing.TakeDamage(dinfo);

                ExplosionHelper.DoExplosion(projectile, GetEquipmentDef(projectile));

                if (map != null)
                    MoteMaker.ThrowText(loc, map, "ПОЖИНАНИЕ: РАЗРУШЕНИЕ", 4f);
            }
            return true;
        }
    }

    public class Effect_ApplyHediff : ProjectileEffect {
        public HediffDef hediff;
        public float basicSeverity;
        public delegate bool Condition(Projectile_Base proj, Thing hit);
        public Condition condition;

        public override void ApplyAfterShield(Projectile_Base projectile, Thing hitThing) {
            if (condition != null && !condition(projectile, hitThing)) {
                return;
            }

            Pawn hitPawn = hitThing as Pawn;
            if (hitPawn == null) {
                return;
            }

            if (hitPawn.Dead) {
                return;
            }

            Utility.AddOrAdjustHediff(hitPawn, hediff, basicSeverity);
        }
    }

    public class Effect_ApplyHeatstroke : ProjectileEffect {
        public HediffDef hediff = HediffDefOf.Heatstroke;
        public float basicSeverity;
        public delegate bool Condition(Projectile_Base proj, Thing hit);
        public Condition condition;

        public override void ApplyAfterShield(Projectile_Base projectile, Thing hitThing) {
            if (condition != null && !condition(projectile, hitThing)) {
                return;
            }

            Pawn hitPawn = hitThing as Pawn;
            if (hitPawn == null) {
                return;
            }

            if (hitPawn.Dead) {
                return;
            }

            float finalSeveiry = basicSeverity / (hitPawn.RaceProps.baseBodySize * (1 + hitPawn.GetStatValue(StatDefOf.ArmorRating_Heat)));

            Utility.AddOrAdjustHediff(hitPawn, hediff, finalSeveiry);
        }
    }

    public class Effect_LauncherReaction : ProjectileEffect {
        public override void ApplyAfterShield(Projectile_Base projectile, Thing hitThing) {
            if (projectile.Launcher is Pawn launcherPawn) {
                int sequence = BeyonderUtility.GetBeyonderSequence(launcherPawn);

                if (projectile is Projectile_BlazingSpear) {
                    var hunter7 = launcherPawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter7_Hediff) as Hunter7_Hediff;
                    bool isHunter7 = (hunter7 != null && sequence == 7);
                    if (isHunter7) {
                        hunter7.AddActingProgress(2, 0.02f, launcherPawn);
                        return;
                    }

                    var hunter6 = launcherPawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter6_Hediff) as Hunter6_Hediff;
                    bool isHunter6 = (hunter6 != null && sequence == 6);
                    if (isHunter6) {
                        BeyonderUtility.AdjustSanityLoss(launcherPawn, 0.05f, "Заговорщик использует грубую силу!");
                        return;
                    }
                }

                if (projectile is Projectile_Execution) {
                    var hunter5 = launcherPawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter5_Hediff) as Hunter5_Hediff;
                    bool isHunter5 = (hunter5 != null && sequence == 6);
                    if (isHunter5) {
                        if (projectile.InstantKillBonus) {
                            hunter5.AddActingProgress(1, 0.01f, launcherPawn);
                        } else if (!projectile.VictimKilled && hitThing != null) {
                            BeyonderUtility.AdjustSanityLoss(launcherPawn, 0.10f, "Пожинатель не казнил цель!");
                        }
                    }
                }
            }
        }
    }

    // Описание снарядов
    // Абстрактный класс для описания огненных снарядов
    public abstract class Projectile_Fire : Projectile_Base {
        // Переменная для отслеживания последней клетки, где мы обновили свет
        private IntVec3 lastLightPosition = IntVec3.Invalid;
        protected virtual bool CausesFire => def.projectile.ai_IsIncendiary;

        protected override void Tick() {
            base.Tick();

            // все огненные снаряды светятся, пока летят
            if (this.Spawned && !this.Destroyed && this.Position != lastLightPosition) {
                lastLightPosition = this.Position;
                CompGlower glower = this.GetComp<CompGlower>();
                if (glower != null) {
                    this.Map.glowGrid.DeRegisterGlower(glower);
                    this.Map.glowGrid.RegisterGlower(glower);
                }
            }
        }
    }

    // Класс для описания снаряда способности "шар огня"
    public class Projectile_Fireball : Projectile_Fire {
        protected override void GatherEffects() {
            base.GatherEffects();

            effects.Add(new Effect_Explosion());
            effects.Add(new Effect_DirectDamage { damageDef = DamageDef });
        }
    }

    // Класс для описания снаряда способности "копье огня"
    public class Projectile_BlazingSpear : Projectile_Fire {
        protected override void GatherEffects() {
            base.GatherEffects();

            effects.Add(new Effect_FireVisuals());

            effects.Add(new Effect_Explosion());
            effects.Add(new Effect_DirectDamage { damageDef = DamageDefOf.Burn });
            // Дополнительный порез
            effects.Add(new Effect_DirectDamage { damageDef = DamageDefOf.Cut });
            // Тепловой удар
            effects.Add(new Effect_ApplyHeatstroke {
                basicSeverity = 0.25f,
                condition = (proj, hit) => hit is Pawn p && p.RaceProps.FleshType == FleshTypeDefOf.Normal
            });
        }
    }

    // Класс для описания снаряда способности "казнь"
    public class Projectile_Execution : Projectile_Fire {
        protected override void GatherEffects() {
            base.GatherEffects();

            effects.Add(new Effect_ReaperVisuals());
            effects.Add(new Effect_ReaperStrike());
            effects.Add(new Effect_DirectDamage { damageDef = DamageDefOf.Burn });
        }
    }

    // визуальные эффекты смерти
    public class Effect_ReaperVisuals : ProjectileEffect {
        public override void OnTick(Projectile_Base projectile) {
            if (projectile.Spawned && !projectile.Destroyed) {
                FleckCreationData data = FleckMaker.GetDataStatic(
                    projectile.ExactPosition,
                    projectile.Map,
                    FleckDefOf.AirPuff,   // или свой FleckDef
                    0.8f                   // scale
                );

                data.instanceColor = Color.black;      // Color? – просто присваиваем Color
                data.solidTimeOverride = 0f;           // float? – без задержки, сразу затухает
                data.airTimeLeft = 0.3f;               // float? – живёт 0.3 секунды
                data.ageTicksOverride = -1;             // -1 = бесконечно (можно задать число тиков)
                projectile.Map.flecks.CreateFleck(data);
            }
        }
    }

    // визуальные эффекты огня
    public class Effect_FireVisuals : ProjectileEffect {
        public override void OnTick(Projectile_Base projectile) {
            // копье оставляет след в виде дыма и искр
            if (projectile.Spawned && !projectile.Destroyed && projectile.tickCounter % 2 == 0) {
                FleckMaker.ThrowSmoke(projectile.ExactPosition, projectile.Map, 0.8f);
                FleckMaker.Static(projectile.ExactPosition, projectile.Map, FleckDefOf.MicroSparks, 1.0f);
            }
        }
    }
}

