using System.Collections.Generic;
using System.Linq;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public class JobGiver_MarshBoarAI : ThinkNode_JobGiver {
        // Радиус, в котором кабан чует пешек (нюх ищейки)
        private const float ScanRadius = 40f;

        // Радиус сплочения врагов (если рядом с целью в пределах 12 клеток стоят союзники, кабан их посчитает)
        private const float CrowdCheckRadius = 12f;

        // Дистанция «срыва»: если человек подошел ближе, кабан реагирует (атака/побег)
        private const float AggroRadius = 15f;

        protected override Job TryGiveJob(Pawn boar) {
            if (boar.needs?.food != null) {
                // Кабан всегда голоден
                boar.needs.food.CurLevelPercentage = 0.10f;
            }

            if (boar.Downed || boar.Dead || boar.IsBurning()) return null;

            // Проверяем голод
            bool isHungry = (boar.needs?.food != null && boar.needs.food.CurLevelPercentage <= 0.30f);

            // 3. ПОИСК БЛИЖАЙШЕГО ЧЕЛОВЕКА
            Pawn target = FindNearestTarget(boar);
            if (target == null) {
                // Если людей нет, а кабан все еще бежит или охотится — сбрасываем задачу
                if (boar.jobs?.curJob != null && (boar.jobs.curJob.def == JobDefOf.PredatorHunt || boar.jobs.curJob.def == JobDefOf.Flee)) {
                    boar.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
                return null;
            }

            float distance = boar.Position.DistanceTo(target.Position);
            bool shouldEvaluateCombat = isHungry || (distance <= AggroRadius);

            if (shouldEvaluateCombat) {
                // 4. ЧЕСТНЫЙ ДИНАМИЧЕСКИЙ ПОДСЧЕТ СИЛ
                float boarPower = boar.kindDef.combatPower * boar.health.summaryHealth.SummaryHealthPercent;

                List<Thing> enemiesAround = GetEnemiesAround(target);
                float totalEnemyPower = CalculateTotalEnemyPower(enemiesAround);

                // 5. ПРИНЯТИЕ РЕШЕНИЯ НА ОСНОВЕ РЕАЛЬНЫХ СИЛ
                if (boarPower > totalEnemyPower) {
                    // КАБАН СИЛЬНЕЕ: Должен атаковать.
                    // Если он УЖЕ охотится ИМЕННО на эту цель, просто не мешаем ему продолжать бег
                    if (boar.jobs?.curJob != null && boar.jobs.curJob.def == JobDefOf.PredatorHunt && boar.jobs.curJob.targetA.Pawn == target) {
                        return null;
                    }

                    // Если он до этого убегал, но враг остался один и ослаб — прерываем бегство и атакуем
                    if (boar.mindState.enemyTarget != target) {
                        boar.mindState.enemyTarget = target;
                    }

                    Job attackJob = JobMaker.MakeJob(JobDefOf.PredatorHunt, target);
                    attackJob.expiryInterval = 60; // Проверка каждую секунду
                    attackJob.checkOverrideOnExpire = true;
                    return attackJob;
                } else {
                    if (boar.jobs?.curJob != null && boar.jobs.curJob.def == JobDefOf.Flee) {
                        // Если до конечной точки бегства еще далеко — пусть просто продолжает бежать туда
                        if (boar.Position.DistanceToSquared(boar.jobs.curJob.targetA.Cell) > 9) {
                            return null;
                        }
                    }

                    // Ищем точку для отступления
                    IntVec3 fleeLoc = CellFinderLoose.GetFleeDestAnimal(boar, enemiesAround, 30f); // Увеличили дистанцию до 30
                    if (!fleeLoc.IsValid || fleeLoc == boar.Position) {
                        // Запасной навигатор
                        fleeLoc = CellFinder.RandomClosewalkCellNear(boar.Position, boar.Map, 20,
                            c => c.Walkable(boar.Map) && c.DistanceTo(target.Position) > distance);
                    }

                    if (fleeLoc.IsValid && fleeLoc != boar.Position) {
                        Job fleeJob = JobMaker.MakeJob(JobDefOf.Flee, fleeLoc, target);

                        // Настраиваем задачу так, чтобы она не сбрасывалась ванильными триггерами
                        fleeJob.expiryInterval = 300; // Ставим большой запас времени, мы всё равно перепишем вектор раньше
                        fleeJob.checkOverrideOnExpire = true;
                        return fleeJob;
                    }
                }
            } else {
                // Если кабан сыт, а люди вышли за радиус 12 клеток, но он всё еще бежал за ними — останавливаем
                if (boar.jobs?.curJob != null && (boar.jobs.curJob.def == JobDefOf.PredatorHunt || boar.jobs.curJob.def == JobDefOf.Flee)) {
                    boar.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }

            return null;
        }

        private Pawn FindNearestTarget(Pawn boar) {
            return (Pawn)GenClosest.ClosestThingReachable(
                boar.Position,
                boar.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                PathEndMode.OnCell,
                TraverseParms.For(boar),
                ScanRadius,
                x => x is Pawn p && p != boar && !p.Downed && !p.Dead
            );
        }

        private List<Thing> GetEnemiesAround(Pawn primaryTarget) {
            List<Thing> list = new List<Thing>();
            Map map = primaryTarget.Map;
            if (map == null) return list;

            var allPawnsOnMap = map.mapPawns.AllPawnsSpawned;

            for (int i = 0; i < allPawnsOnMap.Count; i++) {
                Pawn p = allPawnsOnMap[i];

                if (p == null || p.Downed || p.Dead) continue;

                if (p.Position.DistanceToSquared(primaryTarget.Position) <= CrowdCheckRadius * CrowdCheckRadius) {
                    if (p.RaceProps.Humanlike) {
                        list.Add(p);
                    } else if (primaryTarget.RaceProps.Animal && p.def == primaryTarget.def) {
                        list.Add(p);
                    }
                }
            }

            if (!list.Contains(primaryTarget)) {
                list.Add(primaryTarget);
            }

            return list;
        }

        private float CalculateTotalEnemyPower(List<Thing> enemies) {
            float totalPower = 0f;

            foreach (Thing thing in enemies) {
                if (thing is Pawn enemy) {
                    float individualPower = enemy.kindDef.combatPower;
                    if (enemy.RaceProps.Humanlike && individualPower < 45f) {
                        individualPower = 45f;
                    }

                    if (enemy.equipment?.Primary != null) {
                        individualPower += (enemy.equipment.Primary.MarketValue / 10f);
                    }

                    float armor = enemy.GetStatValue(StatDefOf.ArmorRating_Sharp) + enemy.GetStatValue(StatDefOf.ArmorRating_Blunt);
                    individualPower += (armor * 30f);

                    individualPower *= enemy.health.summaryHealth.SummaryHealthPercent;

                    if (enemies.Count > 1) {
                        totalPower += (individualPower * 1.2f);
                    } else {
                        totalPower += individualPower;
                    }
                }
            }

            return totalPower;
        }
    }

    public class CompProperties_FireRavenController : CompProperties {
        public CompProperties_FireRavenController() {
            compClass = typeof(CompFireRavenController);
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

            if (casterOwner == null || casterOwner.Dead || casterOwner.Downed || (lifetime-- <= 0)) {
                raven.Destroy(DestroyMode.Vanish);
                return;
            }

            // Если хозяин заспавнен, то его карта должна совпадать с картой вороны
            if (casterOwner.Spawned && casterOwner.Map != raven.Map) {
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
}
