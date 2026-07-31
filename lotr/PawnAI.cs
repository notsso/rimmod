using System.Collections.Generic;
using System.Linq;

using Verse;
using Verse.AI;

using RimWorld;

namespace lotr {
    public class JobGiver_MarshBoarAI : ThinkNode_JobGiver {
        // Радиус, в котором кабан чует пешек (нюх ищейки)
        private const float ScanRadius = 40f;

        // Радиус сплочения врагов (если рядом с целью в пределах 12 клеток стоят союзники, кабан их посчитает)
        private const float CrowdCheckRadius = 12f;

        protected override Job TryGiveJob(Pawn boar) {
            // Если кабан спит, горит, находится в ментальном состоянии или уже занят чем-то важным — не трогаем его
            if (boar.Downed || boar.Dead || boar.InMentalState || boar.jobs.curJob != null)
                return null;

            if (boar.needs?.food != null && boar.needs.food.CurLevelPercentage > 0.30f) {
                return null;
            }

            // 1. Рассчитываем силу кабана на основе его текущего здоровья
            // combatPower кабана = 120 (мы задали это в PawnKindDef). Умножаем на процент здоровья.
            float boarPower = boar.kindDef.combatPower * (boar.health.summaryHealth.SummaryHealthPercent);

            // 2. Ищем ближайшую живую пешку (пока только колонистов)
            Pawn target = FindNearestTarget(boar);
            if (target == null) return null;

            List<Thing> enemiesAround = GetEnemiesAround(target);

            // 3. Рассчитываем общую силу ЦЕЛИ и ВСЕХ её союзников рядом с ней!
            float totalEnemyPower = CalculateTotalEnemyPower(enemiesAround);

            // 4. СРАВНЕНИЕ СИЛЫ
            if (boarPower > totalEnemyPower) {
                if (boar.mindState.enemyTarget != target) {
                    boar.mindState.enemyTarget = target;
                }

                Job attackJob = JobMaker.MakeJob(JobDefOf.PredatorHunt, target);
                attackJob.expiryInterval = 200; // Пересчитывать каждые несколько секунд
                attackJob.checkOverrideOnExpire = true;
                return attackJob;
            } else {
                IntVec3 fleeLoc = CellFinderLoose.GetFleeDestAnimal(boar, enemiesAround, 25f);

                if (fleeLoc.IsValid && fleeLoc != boar.Position) {
                    Job fleeJob = JobMaker.MakeJob(JobDefOf.Flee, fleeLoc, target);
                    return fleeJob;
                }
            }

            return null;
        }

        private Pawn FindNearestTarget(Pawn boar) {
            // Ищем колонистов в радиусе видимости/нюха, которые не лежат при смерти
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

            // ЛОГИКА СОЮЗНИЧЕСТВА ТОЛПЫ:
            // 1. Если цель — человек, мы ищем других людей той же фракции рядом (считаем толпу колонистов).
            // 2. Если цель — ванильное животное, мы ищем животных того же вида рядом (считаем стаю оленей/волков).
            // 3. Если цель — ДРУГОЙ КАБАН НАШЕГО ВИДА, он считается абсолютным одиночкой. Метод вернет false, 
            //    поэтому другие кабаны не будут плюсовать свою силу к его защите!
            System.Func<Pawn, bool> belongsToSameGroup = (p) => {
                if (primaryTarget.def == DefDatabase<ThingDef>.GetNamed("lotr_MarshBoarRace")) {
                    return false; // Потусторонние кабаны не защищают друг друга и не объединяются в расчетную группу
                }
                if (primaryTarget.RaceProps.Animal) {
                    return p.def == primaryTarget.def; // Ванильная стая животных
                }
                return p.Faction == primaryTarget.Faction; // Фракция людей
            };

            IEnumerable<Pawn> alliesNearby = GenRadial.RadialCellsAround(primaryTarget.Position, CrowdCheckRadius, true)
                .SelectMany(c => c.GetThingList(map))
                .OfType<Pawn>()
                .Where(p => belongsToSameGroup(p) && !p.Downed && !p.Dead);

            foreach (var pawn in alliesNearby) {
                list.Add(pawn);
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

                    // Учет оружия для людей
                    if (enemy.equipment?.Primary != null) {
                        individualPower += (enemy.equipment.Primary.MarketValue / 10f);
                    }

                    // Учет брони
                    float armor = enemy.GetStatValue(StatDefOf.ArmorRating_Sharp) + enemy.GetStatValue(StatDefOf.ArmorRating_Blunt);
                    individualPower *= (1f + armor);

                    // Учет ранений
                    individualPower *= enemy.health.summaryHealth.SummaryHealthPercent;

                    totalPower += individualPower;
                }
            }

            return totalPower;
        }
    }
}