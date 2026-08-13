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

    // класс для огненных ворон
    public class CompFireRavenController : ThingComp {
        public int lifetime = 3600;
        public Pawn casterOwner = null;
        private IntVec3 lastPosition = IntVec3.Invalid;

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed) {
            base.PostPreApplyDamage(ref dinfo, out absorbed);
            if (dinfo.Amount > 0 && parent is Pawn raven && !raven.Dead) {
                absorbed = true;
                raven.Destroy(DestroyMode.Vanish);
            }
        }

        public override void CompTick() {
            base.CompTick();

            if (!(parent is Pawn raven) || raven.Map == null)
                return;

            if (casterOwner == null || casterOwner.Dead || casterOwner.Downed || lifetime-- <= 0) {
                raven.Destroy(DestroyMode.Vanish);
                return;
            }

            if (casterOwner.Spawned && casterOwner.Map != raven.Map) {
                raven.Destroy(DestroyMode.Vanish);
                return;
            }

            if (parent.Spawned && parent.Position != lastPosition) {
                lastPosition = parent.Position;
                CompGlower glower = parent.GetComp<CompGlower>();
                if (glower != null && parent.Map != null) {
                    parent.Map.glowGrid.DeRegisterGlower(glower);
                    parent.Map.glowGrid.RegisterGlower(glower);
                }
            }
        }

        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_References.Look(ref casterOwner, "casterOwner");
            Scribe_Values.Look(ref lifetime, "lifetime", 3600); // на всякий случай сохраним и таймер
        }
    }

    public class CompProperties_MovingGlower : CompProperties_Glower {
        public CompProperties_MovingGlower() {
            compClass = typeof(CompMovingGlower);
        }
    }

    public class CompMovingGlower : CompGlower {
        private IntVec3 lastPosition = IntVec3.Invalid;

        public override void CompTick() {
            base.CompTick();

            if (parent.Spawned && parent.Position != lastPosition) {
                lastPosition = parent.Position;
                if (parent.Map != null) {
                    parent.Map.glowGrid.DeRegisterGlower(this);
                    parent.Map.glowGrid.RegisterGlower(this);
                }
            }
        }
    }
}
