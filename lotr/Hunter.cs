using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;
using System.Runtime.Remoting.Lifetime;

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

    // класс для описания снаряда способности "копье огня"
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

    // огненные вороны
    public class CompAbilityEffect_LaunchFireRavens : CompAbilityEffect {
        public new CompProperties_AbilityLaunchFireRavens Props => (CompProperties_AbilityLaunchFireRavens)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Map map = caster.Map;

            if (map == null) return;

            int existingRavensCount = 0;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned) {
                if (p.def == DefDatabase<ThingDef>.GetNamed("lotr_FireRavenRace")) {
                    CompFireRavenController controller = p.TryGetComp<CompFireRavenController>();
                    if (controller != null && controller.casterOwner == caster) {
                        existingRavensCount++;
                    }
                }
            }

            int maxTotalRavens = 3;

            int ravensToSpawn = maxTotalRavens - existingRavensCount;

            if (ravensToSpawn <= 0) {
                return;
            }

            int spawnedCount = 0;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(caster.Position, 2f, false).InRandomOrder()) {
                if (spawnedCount >= ravensToSpawn) break;

                if (cell.Walkable(map) && !cell.Fogged(map)) {
                    Pawn raven = PawnGenerator.GeneratePawn(Props.ravenPawnKind, caster.Faction);
                    GenSpawn.Spawn(raven, cell, map);

                    CompFireRavenController controller = raven.TryGetComp<CompFireRavenController>();
                    if (controller != null) {
                        controller.casterOwner = caster;
                    }

                    spawnedCount++;
                }
            }
        }
    }

    public class CompProperties_AbilityLaunchFireRavens : CompProperties_AbilityEffect {
        public PawnKindDef ravenPawnKind;

        public CompProperties_AbilityLaunchFireRavens() {
            compClass = typeof(CompAbilityEffect_LaunchFireRavens);
        }
    }

    public class CompFireRavenController : ThingComp {
        public int lifetime = 3600;
        public Pawn casterOwner = null;

        private const int totalFrames = 8;
        private const int ticksPerFrame = 5;

        // Массивы для предварительного кэширования графики в памяти
        private Graphic[] graphicsNorth { get; } = new Graphic[totalFrames];
        private Graphic[] graphicsSouth { get; } = new Graphic[totalFrames];
        private Graphic[] graphicsEast { get; } = new Graphic[totalFrames];

        private bool graphicsLoaded = false;

        // Метод вызывается ОДИН РАЗ при создании компонента вороны в мире
        public override void Initialize(CompProperties props) {
            base.Initialize(props);

            // Загружаем и кэшируем все кадры в память заранее
            Vector2 drawSize = new Vector2(1.3f, 1.3f);
            Color flameColor = new Color(1f, 1f, 1f); // unnecessary

            for (int i = 0; i < totalFrames; i++) {
                // Собираем пути строго по вашей структуре папок
                string pathNorth = $"Things/Animal/FireRaven/FireRaven_{i}_north";
                string pathSouth = $"Things/Animal/FireRaven/FireRaven_{i}_south";
                string pathEast = $"Things/Animal/FireRaven/FireRaven_{i}_east";

                // Кэшируем через ShaderDatabase.MoteGlow (чтобы не требовало масок _m)
                graphicsNorth[i] = GraphicDatabase.Get<Graphic_Single>(pathNorth, ShaderDatabase.Cutout, drawSize, flameColor);
                graphicsSouth[i] = GraphicDatabase.Get<Graphic_Single>(pathSouth, ShaderDatabase.Cutout, drawSize, flameColor);
                graphicsEast[i] = GraphicDatabase.Get<Graphic_Single>(pathEast, ShaderDatabase.Cutout, drawSize, flameColor);
            }

            graphicsLoaded = true;
        }

        // Перехват урона (инста смерть)
        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed) {
            base.PostPreApplyDamage(ref dinfo, out bool absorbedOut);
            absorbed = absorbedOut;

            if (dinfo.Amount > 0 && parent is Pawn raven && !raven.Dead) {
                absorbed = true;
                raven.Destroy(DestroyMode.Vanish);
            }
        }

        // Логика перемещения
        public override void CompTick() {
            base.CompTick();

            if (!(parent is Pawn raven) || raven.Map == null) return;

            if (casterOwner == null || casterOwner.Dead || casterOwner.Downed || !casterOwner.Spawned || casterOwner.Map != raven.Map || (lifetime-- <= 0)) {
                raven.Destroy(DestroyMode.Vanish);
                return;
            }
        }

        // Берёт готовую графику из кэша памяти без вызова GraphicDatabase.Get в рантайме
        public override void PostDraw() {
            base.PostDraw();

            if (!graphicsLoaded) return;
            if (!(parent is Pawn raven) || !raven.Spawned || raven.Dead) return;

            // 1. Вычисляем текущий кадр
            int currentFrame = (Find.TickManager.TicksGame / ticksPerFrame) % totalFrames;

            // 2. Выбираем нужный массив в зависимости от направления взгляда пешки
            Graphic animatedGraphic = null;
            Rot4 rotation = raven.Rotation;

            if (rotation == Rot4.North) {
                animatedGraphic = graphicsNorth[currentFrame];
            } else if (rotation == Rot4.South) {
                animatedGraphic = graphicsSouth[currentFrame];
            } else if (rotation == Rot4.East || rotation == Rot4.West) {
                animatedGraphic = graphicsEast[currentFrame];
            }

            // 3. Рисуем готовый меш из памяти
            if (animatedGraphic != null && animatedGraphic.MatSingle != null) {
                Material mat = animatedGraphic.MatSingle;
                Vector2 drawSize = new Vector2(1.3f, 1.3f);

                // Если летит на Запад, зеркалим сетку Востока ванильным методом
                Mesh mesh = (rotation == Rot4.West) ? MeshPool.GridPlaneFlip(drawSize) : MeshPool.GridPlane(drawSize);

                Graphics.DrawMesh(mesh, raven.DrawPos, Quaternion.identity, mat, 0);
            }
        }

        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_References.Look(ref casterOwner, "casterOwner");
        }
    }

    public class CompProperties_FireRavenController : CompProperties {
        public CompProperties_FireRavenController() {
            compClass = typeof(CompFireRavenController);
        }
    }

    /*
    public class JobGiver_FireRavenAttack : JobGiver_AIFightEnemy {
        protected override Thing FindAttackTarget(Pawn pawn) {
            if (pawn.Map == null) return null;

            CompFireRavenController controller = pawn.TryGetComp<CompFireRavenController>();
            if (controller == null || controller.casterOwner == null) return null;

            Pawn leader = controller.casterOwner;
            Thing targetToAttack = null;

            // Вариант А: Проверяем, кого хозяин УДАРИЛ последним (идеально для ближнего боя, работает с Бумалопами и животными)
            if (leader.mindState != null && leader.mindState.lastAttackedTarget.Thing != null) {
                targetToAttack = leader.mindState.lastAttackedTarget.Thing;
            }
            // Вариант Б: Проверяем, кто прямо сейчас бьет нашего хозяина в упор
            else if (leader.mindState != null && leader.mindState.meleeThreat != null) {
                targetToAttack = leader.mindState.meleeThreat;
            }
            // Вариант В: Проверяем, в кого хозяин ЦЕЛИТСЯ из дальнего боя (Drafted режим)
            else if (leader.stances != null && leader.stances.curStance is Stance_Busy stanceBusy && stanceBusy.focusTarg.Thing != null) {
                targetToAttack = stanceBusy.focusTarg.Thing;
            }
            // Вариант Г: Запасной ванильный флаг текущей работы (если пешка только бежит бить цель)
            else if (leader.CurJob != null && (leader.CurJob.def == JobDefOf.AttackMelee || leader.CurJob.def == JobDefOf.AttackStatic) && leader.CurJob.targetA.Thing != null) {
                targetToAttack = leader.CurJob.targetA.Thing;
            }

            if (targetToAttack != null && !targetToAttack.Destroyed) {
                if (targetToAttack != leader && targetToAttack.Faction != leader.Faction) {
                    if (targetToAttack.Position.DistanceToSquared(leader.Position) <= 225.0f) {
                        return targetToAttack;
                    }
                }
            }

            Thing autoThreat = (Thing)AttackTargetFinder.BestAttackTarget(
                pawn,
                TargetScanFlags.NeedReachable | TargetScanFlags.NeedThreat,
                t => t is Pawn enemy && enemy.Faction != null && enemy.Faction.HostileTo(pawn.Faction) && t.Position.DistanceToSquared(leader.Position) <= 36.0f,
                0f, 25f, default(IntVec3), float.MaxValue, false
            );

            if (autoThreat != null) {
                return autoThreat;
            }

            return null;
        }

        protected override Job MeleeAttackJob(Pawn pawn, Thing target) {
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.expiryInterval = 30; 
            return job;
        }

        protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null) {
            dest = pawn.Position;
            return false;
        }
    }*/

    public class JobGiver_FireRavenAI : ThinkNode_JobGiver {
        protected override Job TryGiveJob(Pawn pawn) {
            if (pawn.Map == null) return null;

            CompFireRavenController controller = pawn.TryGetComp<CompFireRavenController>();
            if (controller == null || controller.casterOwner == null) {
                return null;
            }

            Pawn leader = controller.casterOwner;

            // 1. ЛОГИКА АТАКИ: Ищем ближайшего врага
            Thing target = FindAttackTarget(pawn);

            // Если враг близко к хозяину (радиус 15 клеток) — плавно летим его атаковать
            if (target != null && target.Position.DistanceToSquared(leader.Position) <= 225.0f) {
                Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
                attackJob.expiryInterval = 30;
                return attackJob;
            }

            float distSq = pawn.Position.DistanceToSquared(leader.Position);
            if (distSq <= 2.1f) {
                return null;
            }

            IntVec3 targetCell = IntVec3.Invalid;
            foreach (IntVec3 adjacentCell in GenRadial.RadialCellsAround(leader.Position, 1.5f, false).InRandomOrder()) {
                if (adjacentCell.Walkable(pawn.Map) && !adjacentCell.Fogged(pawn.Map)) {
                    targetCell = adjacentCell;
                    break;
                }
            }

            if (targetCell.IsValid) {
                Job gotoJob = JobMaker.MakeJob(JobDefOf.Goto, targetCell);
                gotoJob.locomotionUrgency = LocomotionUrgency.Sprint;
                gotoJob.expiryInterval = 30;
                return gotoJob;
            }

            return null;
        }

        protected Thing FindAttackTarget(Pawn pawn) {
            CompFireRavenController controller = pawn.TryGetComp<CompFireRavenController>();

            Pawn leader = controller.casterOwner;
            Thing targetToAttack = null;

            // Вариант А: Проверяем, кого хозяин УДАРИЛ последним (идеально для ближнего боя, работает с Бумалопами и животными)
            if (leader.mindState != null && leader.mindState.lastAttackedTarget.Thing != null) {
                targetToAttack = leader.mindState.lastAttackedTarget.Thing;
            }
            // Вариант Б: Проверяем, кто прямо сейчас бьет нашего хозяина в упор
            else if (leader.mindState != null && leader.mindState.meleeThreat != null) {
                targetToAttack = leader.mindState.meleeThreat;
            }
            // Вариант В: Проверяем, в кого хозяин ЦЕЛИТСЯ из дальнего боя (Drafted режим)
            else if (leader.stances != null && leader.stances.curStance is Stance_Busy stanceBusy && stanceBusy.focusTarg.Thing != null) {
                targetToAttack = stanceBusy.focusTarg.Thing;
            }
            // Вариант Г: Запасной ванильный флаг текущей работы (если пешка только бежит бить цель)
            else if (leader.CurJob != null && (leader.CurJob.def == JobDefOf.AttackMelee || leader.CurJob.def == JobDefOf.AttackStatic) && leader.CurJob.targetA.Thing != null) {
                targetToAttack = leader.CurJob.targetA.Thing;
            }

            if (targetToAttack != null && !targetToAttack.Destroyed) {
                if (targetToAttack != leader && targetToAttack.Faction != leader.Faction) {
                    if (targetToAttack.Position.DistanceToSquared(leader.Position) <= 225.0f) {
                        return targetToAttack;
                    }
                }
            }

            Thing autoThreat = (Thing)AttackTargetFinder.BestAttackTarget(
                pawn,
                TargetScanFlags.NeedReachable | TargetScanFlags.NeedThreat,
                t => t is Pawn enemy && enemy.Faction != null && enemy.Faction.HostileTo(pawn.Faction) && t.Position.DistanceToSquared(leader.Position) <= 36.0f,
                0f, 25f, default(IntVec3), float.MaxValue, false
            );

            if (autoThreat != null) {
                return autoThreat;
            }

            return null;
        }
    }

}
