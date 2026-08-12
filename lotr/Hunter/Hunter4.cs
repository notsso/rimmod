using System.Collections.Generic;
using System.Linq;
using System;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public class Hunter4_Hediff : Beyonder_Hediff {
        public override float SpiritualityOffset => 40f;

        public Hunter4_Hediff() {
            maxProgressPerCategory = 1f;
        }

        private int tickCounter = 0;
        public int checkInterval = 600;
        public float healAmount = 0.1f;
        private const int TicksToRegenPart = 6000;
        private int missingPartRegenTracker = 0;


        public override void Tick() {
            base.Tick();

            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

            tickCounter++;
            if (tickCounter >= checkInterval) {
                tickCounter = 0;
                HealInjuries();
                RegenerateMissingParts();
            }
        }

        private void HealInjuries() {
            List<Hediff_Injury> injuries = new List<Hediff_Injury>();
            pawn.health.hediffSet.GetHediffs(ref injuries);

            foreach (var injury in injuries) {
                if (injury.Severity > 0) {
                    injury.Severity -= healAmount;
                }
            }
        }

        private void RegenerateMissingParts() {
            var missingParts = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
            if (missingParts.Count == 0) {
                missingPartRegenTracker = 0;
                return;
            }

            missingPartRegenTracker += checkInterval;

            if (missingPartRegenTracker >= TicksToRegenPart) {
                missingPartRegenTracker = 0;

                Hediff_MissingPart partToRegen = missingParts[0];
                BodyPartRecord partRecord = partToRegen.Part;

                pawn.health.RemoveHediff(partToRegen);
                pawn.health.RestorePart(partRecord, null, true);

                HediffDef growthDef = DefDatabase<HediffDef>.GetNamed("FragileRegeneratedPart");

                Hediff_BodyPartGrowth injury = (Hediff_BodyPartGrowth)HediffMaker.MakeHediff(growthDef, pawn, partRecord);
                float maxHealth = partRecord.def.GetMaxHealth(pawn);
                injury.Severity = maxHealth - 1f;  // остаётся 1 HP
                pawn.health.AddHediff(injury, partRecord);
                injury.Tended(1f, 1f);

                if (PawnUtility.ShouldSendNotificationAbout(pawn)) {
                    Messages.Message(pawn.LabelShort + " regenerated their " + partRecord.Label + "!", pawn,
                        MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref missingPartRegenTracker, "missingPartRegenTracker", 0);
        }
    }

    public class Hediff_BodyPartGrowth : Hediff_Injury {
        public override void Heal(float amount) { }
        public override float BleedRate => 0f;
        public override float PainOffset => 0f;
    }


    public class Firestorm : Tornado {
        private int ticksToNextFlame = 0;
        public Pawn instigator;
        public int lifeTicks;
        public float minRadius;
        public float maxRadius;
        public int flameInterval;
        public float areaRadius;
        private int buildingDamageTicks = 0;
        private const int BuildingDamageInterval = 30;
        private IntVec3 moveTarget = IntVec3.Invalid;

        public override void SpawnSetup(Map map, bool respawningAfterLoad) {
            base.SpawnSetup(map, respawningAfterLoad);
            ticksToNextFlame = flameInterval;
        }

        protected override void Tick() {
            if (!this.Spawned) return;

            lifeTicks--;
            if (lifeTicks <= 0) {
                this.Destroy();
                return;
            }

            buildingDamageTicks--;
            if (buildingDamageTicks <= 0) {
                buildingDamageTicks = BuildingDamageInterval;
                DamageBuildingsInRadius(8f);
            }

            // Генерация огненных вспышек
            ticksToNextFlame--;
            if (ticksToNextFlame <= 0) {
                ticksToNextFlame = flameInterval;

                float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Rand.Range(2f, areaRadius);
                int x = Mathf.RoundToInt(Mathf.Cos(angle) * distance);
                int z = Mathf.RoundToInt(Mathf.Sin(angle) * distance);
                IntVec3 randomCell = this.Position + new IntVec3(x, 0, z);

                if (randomCell.InBounds(this.Map)) {
                    GenExplosion.DoExplosion(
                        center: randomCell,
                        map: this.Map,
                        radius: Rand.Range(minRadius, maxRadius),
                        damType: DamageDefOf.Flame,
                        instigator: this.instigator,
                        damAmount: 10,
                        armorPenetration: 3.0f,
                        explosionSound: null,
                        weapon: null,
                        projectile: null,
                        intendedTarget: null,
                        postExplosionSpawnThingDef: ThingDefOf.Filth_Ash,
                        postExplosionSpawnChance: 0.4f,
                        postExplosionSpawnThingCount: 1,
                        applyDamageToExplosionCellsNeighbors: true,
                        preExplosionSpawnThingDef: null,
                        preExplosionSpawnChance: 0f,
                        preExplosionSpawnThingCount: 1,
                        chanceToStartFire: 0.8f,
                        damageFalloff: false,
                        ignoredThings: new List<Thing> { this }
                    );
                }
            }
        }

        private void DamageBuildingsInRadius(float radius) {
            if (!this.Spawned) return;

            float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Rand.Range(0f, radius);
            int x = Mathf.RoundToInt(Mathf.Cos(angle) * distance);
            int z = Mathf.RoundToInt(Mathf.Sin(angle) * distance);
            IntVec3 center = this.Position + new IntVec3(x, 0, z);

            if (!center.InBounds(this.Map)) return;

            float blastRadius = 4.0f;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, blastRadius, true)) {
                if (!cell.InBounds(this.Map)) continue;
                Building building = cell.GetEdifice(this.Map);
                if (building != null && building.def?.building != null) {
                    DamageInfo dinfo = new DamageInfo(
                        DamageDefOf.Bomb,
                        amount: 50,
                        armorPenetration: 2.0f,
                        instigator: this.instigator
                    );
                    building.TakeDamage(dinfo);
                }
            }

            GenExplosion.DoExplosion(
                center: center,
                map: this.Map,
                radius: blastRadius,
                damType: DamageDefOf.Flame,
                instigator: this.instigator,
                damAmount: 0,
                armorPenetration: 0f,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 0,
                applyDamageToExplosionCellsNeighbors: false,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 0,
                chanceToStartFire: 0f,
                damageFalloff: false
            );
        }
    }

    public class CompProperties_Firestorm : CompProperties_AbilityEffect {
        public int lifeTicks = 300;          // время жизни в тиках
        public float minExplosionRadius = 1.5f;
        public float maxExplosionRadius = 2.9f;
        public int flameInterval = 10;
        public float areaRadius = 5;

        public CompProperties_Firestorm() {
            compClass = typeof(CompAbilityEffect_CastFirestorm);
        }
    }

    public class CompAbilityEffect_CastFirestorm : CompAbilityEffect {
        public new CompProperties_Firestorm Props => (CompProperties_Firestorm)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);
            Map map = this.parent.pawn.Map;

            if (map != null && target.Cell.InBounds(map)) {
                ThingDef tornadoDef = ThingDef.Named("FirestormThing");
                if (tornadoDef != null) {
                    Firestorm firestorm = (Firestorm)GenSpawn.Spawn(tornadoDef, target.Cell, map, WipeMode.Vanish);
                    firestorm.instigator = this.parent.pawn;

                    firestorm.lifeTicks = Props.lifeTicks;
                    firestorm.minRadius = Props.minExplosionRadius;
                    firestorm.maxRadius = Props.maxExplosionRadius;
                    firestorm.flameInterval = Props.flameInterval;
                    firestorm.areaRadius = Props.areaRadius;
                }
            }
        }
    }

    public class Verb_Firestorm : Verb_CastAbility {
        protected override bool TryCastShot() {
            // Активируем стандартную способность
            if (this.ability != null) {
                this.ability.Activate(this.currentTarget, this.currentDestination);
                return true;
            }
            return false;
        }

        public override void DrawHighlight(LocalTargetInfo target) {
            base.DrawHighlight(target);

            if (!target.IsValid || CasterPawn == null)
                return;

            // Пытаемся достать параметры из CompProperties_Firestorm
            float areaRadius = 5f; // значение по умолчанию

            CompAbilityEffect_CastFirestorm comp = null;

            if (this.ability != null) {
                comp = this.ability.comps
                    .OfType<CompAbilityEffect_CastFirestorm>()
                    .FirstOrDefault();

                if (comp != null) {
                    areaRadius = comp.Props.areaRadius;
                }
            }

            Vector3 center = target.Cell.ToVector3Shifted();

            // круг взрывов
            GenDraw.DrawRadiusRing(center.ToIntVec3(), areaRadius, Color.red);

            // круг поражения
            float explosionZone = comp.Props.maxExplosionRadius + areaRadius;
            GenDraw.DrawRadiusRing(center.ToIntVec3(), explosionZone, new Color(1f, 0.4f, 0f, 0.6f));
        }
    }


    public class CompProperties_Firewave : CompProperties_AbilityEffect {
        public float angleDegrees = 60f;    // полный угол конуса
        public float range = 50f;
        public int damageAmount = 15;
        public float armorPenetration = 1.0f;
        public float fireChance = 0.8f;
        public float speed = 20f;           // клеток в секунду
        public int explosionInterval = 5;   // тиков между взрывами
        public float explosionRadius = 1.5f;

        public CompProperties_Firewave() {
            compClass = typeof(CompAbilityEffect_Firewave);
        }
    }

    public class CompAbilityEffect_Firewave : CompAbilityEffect {
        public new CompProperties_Firewave Props => (CompProperties_Firewave)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);

            Pawn caster = this.parent.pawn;
            if (caster == null || !caster.Spawned) return;

            Map map = caster.Map;
            IntVec3 origin = caster.Position;
            IntVec3 targetCell = target.Cell;

            // Создаём временный объект волны
            ThingDef waveDef = ThingDef.Named("FirewaveEffect");

            FirewaveEffect wave = (FirewaveEffect)GenSpawn.Spawn(waveDef, origin, map, WipeMode.Vanish);
            wave.instigator = caster;
            wave.targetCell = targetCell;
            wave.range = Props.range;
            wave.angleDegrees = Props.angleDegrees;
            wave.fireChance = Props.fireChance;
            wave.speed = Props.speed;
            wave.damageAmount = Props.damageAmount;
            wave.armorPenetration = Props.armorPenetration;

            wave.InitDirection();
        }
    }

    public class FirewaveEffect : ThingWithComps {
        public Pawn instigator;
        public IntVec3 targetCell;
        public float range;
        public float angleDegrees;
        public float fireChance = 0.8f;
        public float speed = 20f;

        public int damageAmount = 100;
        public float armorPenetration = 1f;

        private Vector3 direction;
        private float traveledDistance = 0f;
        private float halfAngleRad;
        public HashSet<IntVec3> ignitedCells = new HashSet<IntVec3>();
        public HashSet<Pawn> damagedPawns = new HashSet<Pawn>();   // <-- для однократного урона
        private bool finished = false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad) {
            base.SpawnSetup(map, respawningAfterLoad);
            // больше ничего не делаем – halfAngleRad ещё не готов
        }

        public void InitDirection() {
            Vector3 casterPos = this.Position.ToVector3();
            if (targetCell.IsValid) {
                Vector3 toTarget = targetCell.ToVector3() - casterPos;
                if (toTarget.sqrMagnitude > 0.001f)
                    direction = toTarget.normalized;
                else
                    direction = Vector3.forward;
            } else {
                direction = Vector3.forward;
            }
            direction.y = 0f;
            direction.Normalize();

            // ВОТ ЗДЕСЬ вычисляем halfAngleRad, когда angleDegrees уже известен
            halfAngleRad = angleDegrees * 0.5f * Mathf.Deg2Rad;
        }

        protected override void Tick() {
            if (!this.Spawned || finished) return;

            float step = speed / 60f;
            float newDistance = Mathf.Min(traveledDistance + step, range);


            List<IntVec3> allConeCells = ProjectileUtility.GetVisibleConeCells(
                origin: this.Position,
                direction: direction,
                maxDist: newDistance,
                halfAngleRad: halfAngleRad,
                map: this.Map
            );

            int cellCount = 0;

            foreach (IntVec3 cell in allConeCells) {
                if (ignitedCells.Add(cell)) {
                    cellCount += 1;
                    // Поджог
                    if (Rand.Value < fireChance)
                        FireUtility.TryStartFireIn(cell, this.Map, Rand.Range(0.2f, 0.5f), instigator);

                    // Визуал
                    FleckMaker.Static(cell, this.Map, FleckDefOf.ExplosionFlash, 4.0f);

                    // ОГРОМНЫЙ УРОН по живым существам в этой клетке
                    List<Thing> things = new List<Thing>(cell.GetThingList(this.Map));
                    foreach (Thing t in things) {
                        if (t is Pawn pawn && pawn != instigator && !pawn.Dead && damagedPawns.Add(pawn)) {
                            DamageInfo dinfo = new DamageInfo(
                                DamageDefOf.Flame,
                                damageAmount,
                                armorPenetration,
                                instigator: instigator
                            );
                            pawn.TakeDamage(dinfo);
                        }

                        if (t is Building building && building.def.useHitPoints) {
                            // Наносим урон, аналогичный урону пешкам
                            DamageInfo dinfo = new DamageInfo(
                                DamageDefOf.Crush,
                                damageAmount,
                                armorPenetration,
                                instigator: instigator
                            );
                            building.TakeDamage(dinfo);
                        }
                    }
                }
            }

            traveledDistance = newDistance;

            if (traveledDistance >= range) {
                finished = true;
                this.Destroy();
            }
        }

        public static void SpawnCellEffect(IntVec3 cell, Map map, ThingDef moteDef, float size = 1f) {
            if (map == null || !cell.InBounds(map)) return;

            Mote mote = (Mote)ThingMaker.MakeThing(moteDef, null);

            mote.Scale = size;
            mote.exactPosition = cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead);

            // 3. Хак времени жизни: 1 тик = 0.01666 секунды. 
            // Заставляем мот думать, что его лимит существования — это время двух тиков.
            mote.exactRotation = Rand.Range(0f, 360f); // Случайный поворот для разнообразия

            // В зависимости от версии RimWorld, можно напрямую переписать внутреннее время:
            // Мы рассчитываем длительность: 2 * 0.0166f = ~0.033 секунды
            // Задаем очень высокую скорость деградации через его параметры, если это поддерживается,
            // ЛИБО используем кастомный класс (см. Способ 2).

            // Спавним мот в мир
            GenSpawn.Spawn(mote, cell, map, WipeMode.Vanish);
        }

        public class IntVec3Comparer : IEqualityComparer<IntVec3> {
            public bool Equals(IntVec3 a, IntVec3 b) {
                if (ReferenceEquals(a, b)) return true;
                return a.x == b.x && a.y == b.y && a.z == b.z;
            }

            public int GetHashCode(IntVec3 v) => (v.x, v.y, v.z).GetHashCode();
        }
    }

    public class Mote_Firewave : Mote {
        private int lifeTicks = 0;
        private const int MaxLifeTicks = 2; // Ровно 2 тика!

        protected override void Tick() {
            // Не вызываем base.Tick(), чтобы ванильная секундная логика не ломала тайминг
            if (!this.Spawned) return;

            lifeTicks++;
            if (lifeTicks >= MaxLifeTicks) {
                // На второй тик объект полностью удаляется из игры
                this.Destroy(DestroyMode.Vanish);
            }
        }
    }

    public class Verb_CastFirewave : Verb_CastAbility {
        protected override bool TryCastShot() {
            if (this.ability != null) {
                this.ability.Activate(this.currentTarget, this.currentDestination);
                return true;
            }
            return false;
        }

        public override void DrawHighlight(LocalTargetInfo target) {
            base.DrawHighlight(target);

            if (!target.IsValid || CasterPawn == null)
                return;

            // Получаем список клеток, которые будут задеты (с учётом препятствий)
            float range = verbProps.range;  // или из CompProperties, если нужно
            float angle = 60f;              // полный угол конуса, можно брать из параметров способности
            Map map = CasterPawn.Map;
            IntVec3 origin = CasterPawn.Position;

            List<IntVec3> affectedCells = ProjectileUtility.CalculateWaveCells(origin, map, target.Cell, range, angle);

            // Рисуем границу клеток (яркая обводка)
            GenDraw.DrawFieldEdges(affectedCells, new Color(1f, 0.5f, 0f, 0.9f));

            // Опционально: лёгкая заливка самих клеток
            Color fillColor = new Color(1f, 0.5f, 0f, 0.2f);
            Material fillMat = SolidColorMaterials.SimpleSolidColorMaterial(fillColor, false);
            foreach (IntVec3 cell in affectedCells) {
                Vector3 center = cell.ToVector3Shifted();
                Graphics.DrawMesh(MeshPool.plane10, center, Quaternion.identity, fillMat, 0);
            }
        }
    }

    // Способность hunter4 (knight): бафф союзников
    public class CompProperties_AbilityGiveHediffArea : CompProperties_AbilityEffect {
        public float radius = 5f;
        public HediffDef hediffDef;

        public CompProperties_AbilityGiveHediffArea() {
            compClass = typeof(CompAbilityEffect_GiveHediffArea);
        }
    }

    public class CompAbilityEffect_GiveHediffArea : CompAbilityEffect {
        public new CompProperties_AbilityGiveHediffArea Props => (CompProperties_AbilityGiveHediffArea)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            Pawn caster = parent.pawn;
            if (caster == null || caster.Map == null) return;

            Map map = caster.Map;
            float radius = Props.radius;
            IntVec3 center = target.Cell;

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            foreach (Pawn pawn in pawns) {
                if (pawn == null || pawn.Dead) continue;
                if (pawn.Position.DistanceToSquared(center) > radius * radius) continue;

                if (pawn == caster) continue;
                if (pawn.Faction != caster.Faction) continue;

                Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, pawn);
                pawn.health.AddHediff(hediff);

                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.MicroSparks, 1.5f);
            }
        }
    }

    public class Verb_IronBloodArmy : Verb_CastAbility {
        protected override bool TryCastShot() {
            if (this.ability != null) {
                this.ability.Activate(this.currentTarget, this.currentDestination);
                return true;
            }
            return false;
        }

        public override void DrawHighlight(LocalTargetInfo target) {
            base.DrawHighlight(target);

            if (CasterPawn == null)
                return;

            // Достаём радиус из CompProperties_AbilityGiveHediffArea
            float radius = 5f; // значение по умолчанию
            if (this.ability != null) {
                var comp = this.ability.comps
                    .OfType<CompAbilityEffect_GiveHediffArea>()
                    .FirstOrDefault();
                if (comp != null)
                    radius = comp.Props.radius;
            }

            // Рисуем круг вокруг кастера (а не вокруг цели)
            Vector3 casterPos = CasterPawn.DrawPos;
            GenDraw.DrawRadiusRing(casterPos.ToIntVec3(), radius, Color.cyan);
        }
    }

    public class ProjectileUtility {

        public static List<IntVec3> GetVisibleConeCells(IntVec3 origin, Vector3 direction, float maxDist, float halfAngleRad, Map map) {
            HashSet<IntVec3> cells = new HashSet<IntVec3>();
            float angleStep = Mathf.Rad2Deg / maxDist; // примерно 1 клетка между лучами на краю
            float halfAngleDeg = halfAngleRad * Mathf.Rad2Deg;
            int rayCount = Mathf.CeilToInt(2 * halfAngleDeg / angleStep);

            for (int i = 0; i <= rayCount; i++) {
                float angle = -halfAngleDeg + i * angleStep;
                Vector3 rayDir = Quaternion.Euler(0f, angle, 0f) * direction;

                float dist = 0f;
                IntVec3 prevCell = origin;
                while (dist < maxDist) {
                    dist += 0.5f;
                    Vector3 point = origin.ToVector3Shifted() + rayDir * dist;
                    IntVec3 cell = point.ToIntVec3();

                    if (cell == prevCell) continue;
                    prevCell = cell;

                    if (!cell.InBounds(map)) break;

                    // Стена или гора – останавливаем луч
                    Building edifice = cell.GetEdifice(map);
                    if (edifice != null && edifice.def.passability == Traversability.Impassable) {
                        cells.Add(cell);
                        break;
                    }

                    cells.Add(cell);
                }
            }
            return cells.ToList();
        }

        public static List<IntVec3> CalculateWaveCells(IntVec3 origin, Map map, IntVec3 targetCell, float range, float angleDegrees) {
            Vector3 direction;
            Vector3 casterPos = origin.ToVector3();
            if (targetCell.IsValid) {
                Vector3 toTarget = targetCell.ToVector3() - casterPos;
                direction = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector3.forward;
            } else {
                direction = Vector3.forward;
            }
            direction.y = 0f;
            direction.Normalize();

            float halfAngleRad = angleDegrees * 0.5f * Mathf.Deg2Rad;
            return GetVisibleConeCells(origin, direction, range, halfAngleRad, map);
        }
    }
}
