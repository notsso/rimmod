using System;
using System.Collections.Generic;
using System.Linq;

using Verse;

using UnityEngine;

using RimWorld;

namespace lotr {
    public abstract class ProjectileEffect {
        public ThingDef equipmentDef;

        // Применить до проверки щита. Возвращает false, если снаряд должен прекратить обработку (например, уже уничтожен).
        public virtual bool ApplyBeforeShield(Projectile_Base projectile, Thing hitThing) => true;

        // Применить после щита (если щит не заблокировал или снаряд проходит сквозь).
        public virtual void ApplyAfterShield(Projectile_Base projectile, Thing hitThing) { }
    }

    public abstract class Projectile_Base : Projectile {
        protected List<ProjectileEffect> effects = new List<ProjectileEffect>();

        public Hediff_ReaperState linkedReaperState = null;
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
        protected float minBodySizeForInstantKillBonus = 2.0f;

        protected override void Tick() {
            base.Tick();
            if (this.Spawned && !this.Destroyed) {
                tickCounter++;
            }
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

        protected virtual void GatherEffects() {
            // для всех снарядов добавляем проверку на ReaperStrike
            if (launcher is Pawn pawn && pawn.health?.hediffSet != null) {
                Hediff_ReaperState reaperHediff = null;
                foreach (var h in pawn.health.hediffSet.hediffs) {
                    if (h is Hediff_ReaperState hr) {
                        reaperHediff = hr;
                        break;
                    }
                }

                if (reaperHediff != null && !reaperHediff.isExpended && !reaperHediff.isReserved) {
                    this.linkedReaperState = reaperHediff;
                    reaperHediff.isReserved = true;
                    effects.Add(new Effect_ReaperStrike());
                }
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false) {
            Pawn victimPawn = hitThing as Pawn;
            bool wasAlive = victimPawn != null && !victimPawn.Dead;
            bool wasUnharmed = false;
            bool bigEnough = false;

            if (wasAlive) {
                wasUnharmed = IsPawnNearlyUnharmed(victimPawn);
                bigEnough = victimPawn.BodySize >= minBodySizeForInstantKillBonus;
            }

            foreach (var effect in effects)
                if (!effect.ApplyBeforeShield(this, hitThing))
                    break;

            if (blockedByShield) {
                Destroy(DestroyMode.Vanish);
                return;
            }

            foreach (var effect in effects)
                effect.ApplyAfterShield(this, hitThing);

            if (wasAlive && victimPawn != null && victimPawn.Dead) {
                InstantKill = wasUnharmed;
                InstantKillBonus = InstantKill && bigEnough;

                if (InstantKillBonus)
                    Log.Message($"{this.def.defName} совершил мгновенное убийство {victimPawn} (здоров {wasUnharmed}, размер ≥ {minBodySizeForInstantKillBonus})");

                OnKilledByProjectile(victimPawn);
            }

            if (this is Projectile_BlazingSpear) {
                var launcherReaction = new Effect_LauncherReaction();
                launcherReaction.ApplyAfterShield(this, hitThing);
            }

            if (!this.Destroyed)
                this.Destroy(DestroyMode.Vanish);
        }

        private bool IsPawnNearlyUnharmed(Pawn pawn) {
            return pawn.health?.summaryHealth?.SummaryHealthPercent > 0.95f;
        }

        protected virtual void OnKilledByProjectile(Pawn victim) {
            Log.Message($"{this.def.defName} killed {victim.Name} with this impact.");
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
        public static void DoExplosion(Projectile_Base proj, ThingDef equipmentDef, float? radiusOverride = null, float? chanceToStartFire = null) {
            float radius = radiusOverride ?? proj.ExplosionRadius;
            float fireChance = chanceToStartFire ?? proj.def.projectile.explosionChanceToStartFire;

            GenExplosion.DoExplosion(
                center: proj.Position,
                map: proj.Map,
                radius: radius,
                damType: proj.DamageDef,
                instigator: proj.Launcher,
                damAmount: proj.DamageAmount,
                armorPenetration: proj.ArmorPenetration,
                explosionSound: proj.def.projectile.soundExplode,
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

        public override void ApplyAfterShield(Projectile_Base projectile, Thing hitThing) {
            float actualRadius = radius ?? projectile.ExplosionRadius;
            ExplosionHelper.DoExplosion(projectile, equipmentDef, radius, fireChance);
        }
    }

    // эффект пожинания
    public class Effect_ReaperStrike : ProjectileEffect {
        public override bool ApplyBeforeShield(Projectile_Base projectile, Thing hitThing) {
            var state = projectile.linkedReaperState;
            if (state == null || state.isExpended) {
                return true;
            }

            state.ExpendCharge();
            // DoReaperExplosion(projectile);

            if (hitThing == null) {
                return true;
            }

            // Модификаторы снаряда
            projectile.currentDamageMultiplier = 3f;
            projectile.customArmorPenetration = 3.0f;

            if (hitThing is Pawn victimPawn && victimPawn.Dead) return true;

            // Ломаем щиты
            if (hitThing is Pawn victim && victim.apparel != null) {
                foreach (var apparel in victim.apparel.WornApparel) {
                    var shield = apparel?.GetComp<CompShield>();
                    if (shield?.parent != null) {
                        shield.parent.TakeDamage(new DamageInfo(DamageDefOf.Bomb, 9999f));
                    }
                }
            }

            // если мы попали в не пешку (строение)
            if (!(hitThing is Pawn)) {
                float bonus = projectile.DamageAmount * 0.5f;
                var dinfo = new DamageInfo(
                    DamageDefOf.Crush,
                    bonus,
                    projectile.ArmorPenetration,
                    projectile.ExactRotation.eulerAngles.y,
                    projectile.Launcher,
                    null,
                    equipmentDef
                );
                dinfo.SetIgnoreArmor(true);
                hitThing.TakeDamage(dinfo);
            }

            if (hitThing.Spawned && hitThing.Map != null) {
                MoteMaker.ThrowText(hitThing.Position.ToVector3Shifted(), hitThing.Map, "ПОЖИНАНИЕ: РАЗРУШЕНИЕ", 4f);
            }
            return true;
        }

        private void DoReaperExplosion(Projectile_Base proj) {
            ExplosionHelper.DoExplosion(proj, equipmentDef, radiusOverride: 2f, chanceToStartFire: 0f);
        }
    }

    public class Effect_ApplyHediff : ProjectileEffect {
        public HediffDef hediff;
        public float severity;
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

            Hediff heatstroke = hitPawn.health.hediffSet.GetFirstHediffOfDef(hediff);

            if (heatstroke != null) {
                heatstroke.Severity += severity;
            } else {
                hitPawn.health.AddHediff(hediff);
                Hediff newHeatstroke = hitPawn.health.hediffSet.GetFirstHediffOfDef(hediff);
                if (newHeatstroke != null) {
                    newHeatstroke.Severity = severity;
                }
            }
        }
    }

    public class Effect_TeleportPawn : ProjectileEffect {
        public Pawn pawn;
        public bool wasDrafted;

        public override bool ApplyBeforeShield(Projectile_Base projectile, Thing hitThing) {
            if (pawn != null && projectile.Spawned) {
                IntVec3 spawnPos = projectile.Position;
                if (!spawnPos.Walkable(projectile.Map) || spawnPos.Fogged(projectile.Map)) {
                    spawnPos = CellFinder.RandomClosewalkCellNear(projectile.Position, projectile.Map, 3);
                }

                GenSpawn.Spawn(pawn, spawnPos, projectile.Map);

                pawn.drafter.Drafted = wasDrafted;

                FleckMaker.ThrowSmoke(projectile.ExactPosition, projectile.Map, 0.8f);
                FleckMaker.Static(projectile.ExactPosition, projectile.Map, FleckDefOf.MicroSparks, 1.0f);

                pawn = null;
            }
            return true;
        }
    }

    public class Effect_LauncherReaction : ProjectileEffect {
        public override void ApplyAfterShield(Projectile_Base projectile, Thing hitThing) {
            if (projectile.Launcher is Pawn launcherPawn) {
                var hunter7 = launcherPawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter7_Hediff) as Hunter7_Hediff;
                if (hunter7 != null) {
                    hunter7.AddActingProgress(2, 0.02f, launcherPawn);
                }

                var hunter6 = launcherPawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter6_Hediff) as Hunter6_Hediff;
                if (hunter6 != null) {
                    BeyonderUtility.AddSanityLoss(launcherPawn, 0.05f, "Заговорщик использует грубую силу!");
                }

                Log.Message($"[InstantKillBonus] projectile.InstantKillBonus={projectile.InstantKillBonus}");
                if (projectile.InstantKillBonus) {
                    var hunter5 = launcherPawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter5_Hediff) as Hunter5_Hediff;
                    if (hunter5 == null) {
                        Log.Message($"[InstantKillBonus] Hunter5 hediff not found on {launcherPawn.Name}. Available hediffs: {string.Join(", ", launcherPawn.health.hediffSet.hediffs.Select(h => h.def.defName))}");
                    }

                    if (hunter5 != null) {
                        Log.Message($"[InstantKillBonus] Начислен бонус Hunter5 за убийство {hitThing}");
                        hunter5.AddActingProgress(1, 0.01f, launcherPawn);
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

    public class Projectile_BlazingSpear : Projectile_Fire {
        protected override void Tick() {
            base.Tick();

            // копье оставляет след в виде дыма и искр
            if (this.Spawned && !this.Destroyed && tickCounter % 2 == 0) {
                FleckMaker.ThrowSmoke(this.ExactPosition, this.Map, 0.8f);
                FleckMaker.Static(this.ExactPosition, this.Map, FleckDefOf.MicroSparks, 1.0f);
            }
        }

        protected override void GatherEffects() {
            base.GatherEffects();
            effects.Add(new Effect_Explosion());
            effects.Add(new Effect_DirectDamage { damageDef = DamageDefOf.Bomb });
            // Дополнительный порез
            effects.Add(new Effect_DirectDamage { damageDef = DamageDefOf.Cut });
            // Тепловой удар
            effects.Add(new Effect_ApplyHediff {
                hediff = HediffDefOf.Heatstroke,
                severity = 0.25f,
                condition = (proj, hit) => hit is Pawn p && p.RaceProps.FleshType == FleshTypeDefOf.Normal
            });

            // effects.Add(new Effect_LauncherReaction());
        }
    }

    public class Projectile_FireTeleport : Projectile_Fire {
        public Pawn teleportPawn;
        public bool wasDrafted;

        protected override void GatherEffects() {
            base.GatherEffects();
            effects.Add(new Effect_TeleportPawn {
                pawn = teleportPawn,
                wasDrafted = wasDrafted
            });
        }
    }
}

