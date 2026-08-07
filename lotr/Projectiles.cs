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
            BeforeImpact(hitThing);

            if (blockedByShield) {
                Destroy(DestroyMode.Vanish);
                return;
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

        public virtual void BeforeImpact(Thing hitThing) { }

        public virtual void OnImpact(Thing hitThing) { }

        public virtual void OnDestroy() { }

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null) {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);

            // Если базовый Launch не заспавнил снаряд — делаем это вручную
            if (!this.Spawned) {
                Log.Warning($"Base.Launch failed for {this.def.defName}, spawning manually.");
                GenSpawn.Spawn(this, origin.ToIntVec3(), launcher.Map);
            }

            OnLaunch();
        }
    }

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

    // Абстрактный класс для описания снарядов, которые взрываются
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


    // Класс для описания снаряда способности "шар огня"
    public class Projectile_Fireball : Projectile_FireExplosive {
        public override void OnImpact(Thing hitThing) {
            base.OnImpact(hitThing);

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
        }
    }

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

                    // но если заговорщик, то он получает дебафф
                    var hunter6Hediff = launcherPawn.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter6_Hediff) as Hunter6_Hediff;
                    if (hunter6Hediff != null) {
                        var sanityPenalty = 0.05f;
                        BeyonderUtility.AddSanityLoss(launcherPawn, sanityPenalty, "Заговорщик использует грубую силу!");
                    }
                }
            }
        }
    }

    public class Projectile_FireTeleport : Projectile_Fire {
        public Pawn teleportPawn;
        public bool wasDrafted;

        public override void BeforeImpact(Thing hitThing) {
            if (teleportPawn != null && this.Spawned) {
                IntVec3 spawnPos = this.Position;
                if (!spawnPos.Walkable(this.Map) || spawnPos.Fogged(this.Map)) {
                    spawnPos = CellFinder.RandomClosewalkCellNear(this.Position, this.Map, 3);
                }

                GenSpawn.Spawn(teleportPawn, spawnPos, this.Map);

                teleportPawn.drafter.Drafted = wasDrafted;

                FleckMaker.ThrowSmoke(this.ExactPosition, this.Map, 0.8f);
                FleckMaker.Static(this.ExactPosition, this.Map, FleckDefOf.MicroSparks, 1.0f);

                teleportPawn = null;
            }
        }
    }

    // Абстрактный класс для описания снарядов молнии? 
    public abstract class Projectile_Lightning : Projectile_Base {
        public bool CanStun => def.GetModExtension<Projectile_LightningExtension>().canStun ?? false;
        public float StunChance => def.GetModExtension<Projectile_LightningExtension>().stunChance ?? 0f;

        public override void OnImpact(Thing hitThing) {
            base.OnImpact(hitThing);
            // Применяем эффект оглушения
        }
    }

    // Абстрактный класс для описания ядовитых снарядов 
    public abstract class Projectile_Poison : Projectile_Base {
        public HediffDef PoisonHediff => def.GetModExtension<Projectile_PoisonExtension>().poisonHediff;
        public float PoisonSeverity => def.GetModExtension<Projectile_PoisonExtension>().poisonSeverity ?? 0f;

        public override void OnImpact(Thing hitThing) {
            base.OnImpact(hitThing);
            // Применяем эффект заражения
        }
    }

    // Абстрактный класс для описания святых/очищающих снарядов
    public abstract class Projectile_Sunlight : Projectile_Base {
        public bool isEffectiveVsUndead => def.GetModExtension<Projectile_SunlightExtension>().isEffectiveVsUndead ?? false;
        public float BonusDamageVsUndead => def.GetModExtension<Projectile_SunlightExtension>().bonusDamageVsUndead ?? 0;

        public override void OnImpact(Thing hitThing) {
            base.OnImpact(hitThing);
            // Применяем эффект очищения
        }
    }

    // Абстрактный класс для описания снарядов дающими контроль над противником?
    public abstract class Projectile_Marionette : Projectile_Base {
        public int ControlDuration => def.GetModExtension<Projectile_MarionetteExtension>().controlDuration ?? 0;
        public float ControlChance => def.GetModExtension<Projectile_MarionetteExtension>().controlChance ?? 0f;

        public override void OnImpact(Thing hitThing) {
            base.OnImpact(hitThing);
            // Применяем эффект контроля?
        }
    }
}