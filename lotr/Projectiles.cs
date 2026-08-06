using System;

using Verse;

using UnityEngine;

using RimWorld;

namespace lotr {
    // Абстрактный базовый класс для всех снарядов мода
    public abstract class Projectile_Base : Projectile {
        public override int DamageAmount => def.projectile.GetDamageAmount(this, null);
        public override float ArmorPenetration => def.projectile.GetArmorPenetration(this, null);
        public new DamageDef DamageDef => def.projectile.damageDef;
        public virtual float Speed => def.projectile.speed;

        public bool IsInvisible = false;
        public int tickCounter = 0;

        protected override void Tick() {
            base.Tick();
            if (this.Spawned && !this.Destroyed) {
                tickCounter++;
            }
            OnTick();
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false) {
            if (blockedByShield) {
                OnBlocked();
                Destroy(DestroyMode.Vanish);
                return;
            }

            // у всех снарядов есть какой то базовый тип урона
            if (hitThing != null && DamageAmount > 0) {
                DamageInfo dinfo = new DamageInfo(
                    DamageDef,
                    DamageAmount,
                    ArmorPenetration,
                    this.ExactRotation.eulerAngles.y,
                    this.launcher,
                    null,
                    this.equipmentDef,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    this.intendedTarget.Thing
                );
                hitThing.TakeDamage(dinfo);
            }

            OnImpact(hitThing);

            if (!this.Destroyed)
                this.Destroy(DestroyMode.Vanish);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
            OnDestroy();
            base.Destroy(mode);
        }

        public virtual void OnLaunch() { }

        public virtual void OnTick() { }

        public virtual void OnImpact(Thing hitThing) { }

        public virtual void OnBlocked() { }

        public virtual void OnDestroy() { }

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null) {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            OnLaunch();
        }
    }

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

    // при попадании во врага, снаряд взрывается
    public abstract class Projectile_FireExplosive : Projectile_Fire {
        public float ExplosionRadius => def.projectile.explosionRadius;

        public override void OnImpact(Thing hitThing) {
            base.OnImpact(hitThing);

            if (ExplosionRadius > 0f) {
                DoExplosion();
            }
        }

        protected virtual void DoExplosion() {
            GenExplosion.DoExplosion(
                center: this.Position,
                map: this.Map,
                radius: ExplosionRadius,
                damType: DamageDef,
                instigator: this.launcher,
                damAmount: DamageAmount,
                armorPenetration: ArmorPenetration,
                explosionSound: this.def.projectile.soundExplode,
                weapon: this.equipmentDef,
                projectile: this.def,
                intendedTarget: this.intendedTarget.Thing,
                postExplosionSpawnThingDef: this.def.projectile.postExplosionSpawnThingDef,
                postExplosionSpawnChance: this.def.projectile.postExplosionSpawnChance,
                postExplosionSpawnThingCount: this.def.projectile.postExplosionSpawnThingCount,
                postExplosionGasType: null,
                postExplosionGasRadiusOverride: null,
                postExplosionGasAmount: 255,
                applyDamageToExplosionCellsNeighbors: this.def.projectile.applyDamageToExplosionCellsNeighbors,
                preExplosionSpawnThingDef: this.def.projectile.preExplosionSpawnThingDef,
                preExplosionSpawnChance: this.def.projectile.preExplosionSpawnChance,
                preExplosionSpawnThingCount: this.def.projectile.preExplosionSpawnThingCount,
                chanceToStartFire: CausesFire ? this.def.projectile.explosionChanceToStartFire : 0f,
                damageFalloff: this.def.projectile.explosionDamageFalloff
            );
        }
    }


    // класс для описания снаряда способности "шар огня"
    public class Projectile_Fireball : Projectile_FireExplosive { }

    // класс для описания снаряда способности "копье огня"
    public class Projectile_BlazingSpear : Projectile_FireExplosive {
        protected override void Tick() {
            base.Tick();

            // копье оставляет след в виде дыма и искр
            if (this.Spawned && !this.Destroyed && tickCounter % 2 == 0) {
                FleckMaker.ThrowSmoke(this.ExactPosition, this.Map, 0.8f);
                FleckMaker.Static(this.ExactPosition, this.Map, FleckDefOf.MicroSparks, 1.0f);
            }
        }

        public override void OnImpact(Thing hitThing) {
            base.OnImpact(hitThing);

            if (hitThing != null) {
                Pawn hitPawn = hitThing as Pawn;

                // Помимо ожога цель получит:

                // Физический порез (Cut)
                DamageInfo cutDinfo = new DamageInfo(
                    DamageDefOf.Cut,
                    (float)DamageAmount,
                    ArmorPenetration,
                    this.ExactRotation.eulerAngles.y,
                    this.launcher,
                    null,
                    this.equipmentDef,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    this.intendedTarget.Thing
                );
                hitThing.TakeDamage(cutDinfo);

                // Тепловой удар (Heatstroke)
                if (hitPawn != null && hitPawn.RaceProps.FleshType == FleshTypeDefOf.Normal) {
                    Hediff heatstroke = hitPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);

                    if (heatstroke != null) {
                        heatstroke.Severity += 0.25f;
                    } else {
                        hitPawn.health.AddHediff(HediffDefOf.Heatstroke);
                        Hediff newHeatstroke = hitPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
                        if (newHeatstroke != null) {
                            newHeatstroke.Severity = 0.25f;
                        }
                    }
                }

                // А если снаряд запускал пиромант, он получит за это бафф к усвоению
                if (this.launcher is Pawn launcherPawn) {
                    var hunter7Hediff = launcherPawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter7_Hediff) as Hunter7_Hediff;
                    if (hunter7Hediff != null) {
                        hunter7Hediff.AddActingProgress(2, 0.02f, launcherPawn);
                    }
                }
            }
        }
    }

    public abstract class Projectile_Lightning : Projectile_Base {
        public bool CanStun;
        public float StunChance;

        public override void OnImpact(Thing thing) {

        }
    }

    public abstract class Projectile_Poison : Projectile_Base {
        public HediffDef PoisonHediff;
        public float PoisonSeverity;

        public override void OnImpact(Thing thing) {

        }
    }

    public abstract class Projectile_Sunlight : Projectile_Base {
        public bool isEffectiveVsUndead;
        public float BonusDamageVsUndead;

        public override void OnImpact(Thing thing) {

        }
    }

    public abstract class Projectile_Marionette : Projectile_Base {
        public int ControlDuration;
        public float ControlChance;

        public override void OnImpact(Thing thing) {

        }
    }
}