using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public class Hunter7_Hediff : Hunter8_Hediff {
        public override float SpiritualityFactor => 5f;

        public Hunter7_Hediff() {
            maxProgressPerCategory = 0.4f;
        }
    }

    // Класс свойств для связи с XML
    public class CompProperties_AbilityLaunchFireRavens : CompProperties_AbilityEffect {
        public PawnKindDef ravenPawnKind;
        public int lifetime = 3600;
        public int maxCount = 3;

        public CompProperties_AbilityLaunchFireRavens() {
            compClass = typeof(CompAbilityEffect_LaunchFireRavens);
        }
    }

    // класс для способности "огненные вороны"
    public class CompAbilityEffect_LaunchFireRavens : CompAbilityEffect {
        public new CompProperties_AbilityLaunchFireRavens Props => (CompProperties_AbilityLaunchFireRavens)props;

        // создает вокруг заклинателя 3х ворон
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Map map = caster.Map;

            if (map == null) return;

            int existingRavensCount = 0;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned) {
                if (p.def == LotrDefOf.lotr_FireRavenRace) {
                    CompFireRavenController controller = p.TryGetComp<CompFireRavenController>();
                    if (controller != null && controller.casterOwner == caster) {
                        existingRavensCount++;
                    }
                }
            }

            int maxTotalRavens = Props.maxCount;

            int ravensToSpawn = Mathf.Min(1, maxTotalRavens - existingRavensCount);

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
                        controller.lifetime = Props.lifetime;
                    }

                    spawnedCount++;
                }
            }
        }
    }

    // класс для огненных ворон - прорисовка и пара особенностей
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

        private IntVec3 lastPosition = IntVec3.Invalid;

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

            if (this.parent.Spawned && this.parent.Position != lastPosition) {
                lastPosition = this.parent.Position;

                // Достаем ванильный компонент света из вороны
                CompGlower glower = this.parent.GetComp<CompGlower>();
                if (glower != null && this.parent.Map != null) {
                    // Перерегистрируем свет на новой клетке (как мы делали с файерболом)
                    this.parent.Map.glowGrid.DeRegisterGlower(glower);
                    this.parent.Map.glowGrid.RegisterGlower(glower);
                }
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

        // сохраняет владельца вороны при сохранении игры
        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_References.Look(ref casterOwner, "casterOwner");
        }
    }

    // Класс свойств для связи с XML?
    public class CompProperties_FireRavenController : CompProperties {
        public CompProperties_FireRavenController() {
            compClass = typeof(CompFireRavenController);
        }
    }

    // класс для ии ворон
    public class JobGiver_FireRavenAI : ThinkNode_JobGiver {
        protected override Job TryGiveJob(Pawn pawn) {
            if (pawn.Map == null) return null;

            CompFireRavenController controller = pawn.TryGetComp<CompFireRavenController>();
            if (controller == null || controller.casterOwner == null) {
                return null;
            }

            Pawn leader = controller.casterOwner;

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

    public class SummonedWeaponExtension : DefModExtension {
        public ThingDef weaponDef; // Поле, где в XML мы укажем Def меча
    }

    // класс для способности "огненный меч" - призывает в руках пешки оружие
    public class Ability_SummonBlazingSword : Ability_SpendSpirituality {
        public Ability_SummonBlazingSword() : base() { }

        public Ability_SummonBlazingSword(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest) {
            bool result = base.Activate(target, dest);

            Pawn caster = this.pawn;
            if (caster != null && caster.equipment != null) {
                ThingDef swordDef = this.def.GetModExtension<SummonedWeaponExtension>()?.weaponDef;

                if (swordDef == null) {
                    swordDef = DefDatabase<ThingDef>.GetNamed("Melee_BlazingSword", false);
                }

                if (swordDef != null) {
                    if (caster.equipment.Primary != null) {
                        ThingWithComps oldWeapon = caster.equipment.Primary;

                        if (caster.inventory != null) {
                            caster.equipment.Remove(oldWeapon);
                            caster.inventory.innerContainer.TryAdd(oldWeapon, true);
                        } else {
                            caster.equipment.TryDropEquipment(oldWeapon, out var _, caster.Position);
                        }
                    }

                    ThingWithComps summonedSword = (ThingWithComps)ThingMaker.MakeThing(swordDef);

                    caster.equipment.AddEquipment(summonedSword);

                    FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.MicroSparks, 2.0f);
                    FleckMaker.ThrowSmoke(caster.DrawPos, caster.Map, 1.2f);
                }
            }

            return true;
        }
    }

    public class SummonedWeapon : ThingWithComps { }

    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "EquipmentTrackerTick")]
    public static class Patch_SummonedWeapon_LifespanInHands {
        [HarmonyPostfix]
        public static void Postfix(Pawn_EquipmentTracker __instance) {
            if (__instance.Primary != null && __instance.Primary is SummonedWeapon summonedWeapon) {
                CompLifespan lifespanComp = summonedWeapon.GetComp<CompLifespan>();

                if (lifespanComp != null) {
                    lifespanComp.age += 1;

                    if (lifespanComp.age >= lifespanComp.Props.lifespanTicks) {
                        Pawn pawn = __instance.pawn;

                        summonedWeapon.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Bill_Production), "Notify_IterationCompleted")]
    public static class Patch_Pyromancer_Creation {
        [HarmonyPostfix]
        public static void Postfix(Bill_Production __instance, Pawn billDoer) {
            if (billDoer != null && billDoer.IsColonist) {
                if (__instance.billStack?.billGiver is Building_WorkTable table && table.def.defName == "Campfire") {
                    var hediff = billDoer.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter7_Hediff) as Hunter7_Hediff;

                    if (hediff != null) {
                        hediff.AddActingProgress(1, 0.01f, billDoer);
                    }
                }
            }
        }
    }

    // класс для hediff firearmor
    public class Hediff_FireArmor : HediffWithComps {
        // Храним ссылку на заспавненную невидимую лампочку
        private ThingWithComps lightSource = null;

        public override void PostAdd(DamageInfo? dinfo) {
            base.PostAdd(dinfo);
            SpawnLight();
        }

        public override void PostRemoved() {
            base.PostRemoved();
            DespawnLight();
        }

        // Каждый тик проверяем позицию пешки
        public override void Tick() {
            base.Tick();

            if (this.pawn == null || !this.pawn.Spawned || this.pawn.Map == null) {
                DespawnLight();
                return;
            }

            // Если лампочки нет — спавним
            if (lightSource == null || !lightSource.Spawned) {
                SpawnLight();
            }
            // ИСПРАВЛЕНО: Если пешка сделала шаг на новую клетку
            else if (lightSource.Position != this.pawn.Position) {
                // Вместо багнутой телепортации позиции, мы пересоздаем свет в новой точке.
                // Это заставляет движок RimWorld мгновенно перерисовать световое пятно на экране!
                DespawnLight();
                SpawnLight();
            }
        }

        private void SpawnLight() {
            if (this.pawn == null || !this.pawn.Spawned || this.pawn.Map == null) return;
            if (lightSource != null && lightSource.Spawned) return;

            ThingDef lightDef = LotrDefOf.lotr_FireLightSpawner;
            if (lightDef != null) {
                lightSource = GenSpawn.Spawn(lightDef, this.pawn.Position, this.pawn.Map) as ThingWithComps;
            }
        }

        private void DespawnLight() {
            if (lightSource != null && lightSource.Spawned) {
                lightSource.Destroy(DestroyMode.Vanish);
                lightSource = null;
            }
        }
    }

    public class SummonedFireWeapon : SummonedWeapon {
        // Просто пустой класс, чтобы игра знала тип предмета в XML
    }

    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "EquipmentTrackerTick")]
    public static class Patch_SummonedFireWeapon_GlowerController {
        // Словарь для отслеживания лампочек у разных пешек (чтобы не было конфликтов, если мечи призовут сразу 3 мага)
        private static Dictionary<Pawn, Thing> activeWeaponLights { get; } = new Dictionary<Pawn, Thing>();

        [HarmonyPostfix]
        public static void Postfix(Pawn_EquipmentTracker __instance) {
            Pawn pawn = __instance.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null) return;

            // 1. ПРОВЕРКА: Держит ли пешка наш Огненный меч прямо сейчас?
            if (__instance.Primary != null && __instance.Primary is SummonedFireWeapon) {
                // Принудительно крутим ванильный таймер Lifespan меча, пока он в руках
                CompLifespan lifespan = __instance.Primary.GetComp<CompLifespan>();
                if (lifespan != null) {
                    lifespan.age += 1;
                    if (lifespan.age >= lifespan.Props.lifespanTicks) {
                        // Время вышло — уничтожаем меч
                        __instance.Primary.Destroy(DestroyMode.Vanish);

                        // Удаляем свет
                        if (activeWeaponLights.TryGetValue(pawn, out Thing oldLight) && oldLight.Spawned) {
                            oldLight.Destroy(DestroyMode.Vanish);
                        }
                        activeWeaponLights.Remove(pawn);
                        return;
                    }
                }

                // 2. УПРАВЛЕНИЕ СВЕТОМ МЕЧА
                if (!activeWeaponLights.TryGetValue(pawn, out Thing light) || light == null || !light.Spawned) {
                    // Спавним свет, если его еще нет
                    ThingDef lightDef = LotrDefOf.lotr_FireLightSpawner;
                    if (lightDef != null) {
                        activeWeaponLights[pawn] = GenSpawn.Spawn(lightDef, pawn.Position, pawn.Map);
                    }
                } else if (light.Position != pawn.Position) {
                    // Если пешка сделала шаг — пересоздаем свет в новой точке для обновления графики Unity
                    light.Destroy(DestroyMode.Vanish);
                    ThingDef lightDef = LotrDefOf.lotr_FireLightSpawner;
                    activeWeaponLights[pawn] = GenSpawn.Spawn(lightDef, pawn.Position, pawn.Map);
                }
            } else {
                // Если меча в руках больше нет (бросил, убрал или он испарился) — гасим свет
                if (activeWeaponLights.TryGetValue(pawn, out Thing light) && light != null && light.Spawned) {
                    light.Destroy(DestroyMode.Vanish);
                }
                activeWeaponLights.Remove(pawn);
            }
        }
    }

    // Harmony patch - отслеживает 'анти-действие' пироманта: получение урона от огня
    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Patch_Pyromancer_FireDamage_SanityLoss {
        [HarmonyPostfix]
        public static void Postfix(Thing __instance, DamageInfo dinfo) {
            if (__instance is Pawn pawn && pawn.IsColonist && !pawn.Destroyed) {
                if (dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.Burn) {
                    var hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(LotrDefOf.Hunter7_Hediff) as Hunter7_Hediff;

                    if (hediff == null) return;

                    float sanityPenalty = 0.05f;

                    BeyonderUtility.AddSanityLoss(pawn, sanityPenalty, "Пламя ранило пироманта!");
                }
            }
        }
    }
}
