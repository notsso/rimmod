using System.Collections.Generic;
using System.Linq;
using System;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
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

    public class Ability_SummonWeapon : Ability_SpendSpirituality {
        public Ability_SummonWeapon() : base() { }

        public Ability_SummonWeapon(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest) {
            bool result = base.Activate(target, dest);
            if (!result) return false;

            Pawn caster = this.pawn;
            if (caster == null || caster.equipment == null) return false;

            SummonedWeaponExtension ext = this.def.GetModExtension<SummonedWeaponExtension>();
            if (ext == null || ext.weaponDef == null) {
                Log.Warning($"Ability {this.def.defName} has no SummonedWeaponExtension or weaponDef!");
                return false;
            }

            ThingDef weaponDef = ext.weaponDef;

            // Сохраняем текущее оружие (если есть) в инвентарь или выкидываем
            if (caster.equipment.Primary != null) {
                ThingWithComps oldWeapon = caster.equipment.Primary;
                caster.equipment.Remove(oldWeapon);
                if (caster.inventory != null)
                    caster.inventory.innerContainer.TryAdd(oldWeapon, true);
                else
                    caster.equipment.TryDropEquipment(oldWeapon, out var _, caster.Position);
            }

            // Создаём и экипируем новое оружие
            SummonedFireWeapon summonedWeapon = (SummonedFireWeapon)ThingMaker.MakeThing(weaponDef);
            summonedWeapon.ticksLeft = ext.lifespan;

            caster.equipment.AddEquipment(summonedWeapon);

            // Визуальные эффекты при призыве
            OnSummon(caster);

            return true;
        }

        // Виртуальный метод для эффектов при призыве
        protected virtual void OnSummon(Pawn caster) { }
    }

    public class Ability_SummonWeaponFire : Ability_SummonWeapon {
        public Ability_SummonWeaponFire() : base() { }

        public Ability_SummonWeaponFire(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        protected override void OnSummon(Pawn caster) {
            FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.MicroSparks, 2.0f);
            FleckMaker.ThrowSmoke(caster.DrawPos, caster.Map, 1.2f);
        }
    }

    // Подсветка зоны поражения для всех способностей с радиусом
    public class Verb_ExplosionZone : Verb_CastAbility {
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

            // Пытаемся получить projectileDef и его explosionRadius
            float explosionRadius = 0f;
            if (this.ability != null) {
                var comp = this.ability.comps
                    .OfType<CompAbilityEffect_LaunchProjectile>()
                    .FirstOrDefault();
                if (comp != null && comp.Props.projectileDef != null) {
                    explosionRadius = comp.Props.projectileDef.projectile.explosionRadius;
                }
            }

            if (explosionRadius > 0f) {
                Vector3 center = target.Cell.ToVector3Shifted();
                GenDraw.DrawRadiusRing(center.ToIntVec3(), explosionRadius, new Color(1f, 0.8f, 0.2f)); // жёлто-оранжевый
            }
        }
    }

    // Способность hunter8 (провокатор): провокация
    public class CompProperties_AbilityProvoke : CompProperties_AbilityEffect {
        public float baseSuccessChance = 50.0f;

        public CompProperties_AbilityProvoke() {
            compClass = typeof(CompAbilityEffect_Provoke);
        }
    }

    public class CompAbilityEffect_Provoke : CompAbilityEffect {
        public new CompProperties_AbilityProvoke Props => (CompProperties_AbilityProvoke)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            base.Apply(target, dest);

            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn == null || targetPawn.Dead || targetPawn.Downed) {
                return;
            } else if (targetPawn.Faction == caster.Faction) {
                return;
            }

            float victimPsychicSensitivity = targetPawn.GetStatValue(StatDefOf.PsychicSensitivity, true);
            float baseSuccessChance = Props.baseSuccessChance;
            float finalSuccessChance = baseSuccessChance * victimPsychicSensitivity;

            var hediff = caster.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter8_Hediff) as Hunter8_Hediff;
            int sequence = BeyonderUtility.GetBeyonderSequence(caster);
            bool isHunter8 = (hediff != null && sequence == 8);

            if (Rand.Value <= finalSuccessChance) {
                ProvokePawn(targetPawn, caster);
                if (targetPawn.RaceProps.ToolUser || targetPawn.RaceProps.IsMechanoid) {
                    if (isHunter8) {
                        float severityIncrement = 0.05f;
                        hediff.AddActingProgress(1, severityIncrement, caster);
                    }
                }
            } else {
                if (isHunter8) {
                    float sanityPenalty = 0.10f;
                    BeyonderUtility.AdjustSanityLoss(caster, sanityPenalty, "Провокация провалена!");
                }
            }
        }

        // Провоцирует цель - дает ей задачу на ближний бой с провокатором на 10 секунд
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

        // Способность hunter7 (pyromaniac): огненные вороны
    public class CompProperties_AbilityLaunchFireRavens : CompProperties_AbilityEffect {
        public PawnKindDef ravenPawnKind;
        public int lifetime = 3600;
        public int maxCount = 3;

        public CompProperties_AbilityLaunchFireRavens() {
            compClass = typeof(CompAbilityEffect_LaunchFireRavens);
        }
    }

    public class CompAbilityEffect_LaunchFireRavens : CompAbilityEffect {
        public new CompProperties_AbilityLaunchFireRavens Props => (CompProperties_AbilityLaunchFireRavens)props;

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

                    raven.training.Train(TrainableDefOf.Obedience, caster, complete: true);
                    raven.training.Train(TrainableDefOf.Release, caster, complete: true);

                    raven.playerSettings.Master = caster;

                    raven.playerSettings.followDrafted = true;
                    raven.playerSettings.followFieldwork = true;

                    spawnedCount++;
                }
            }
        }
    }

    // Способность hunter7 (pyromaniac): Огненная броня
    public class CompProperties_AbilityGiveHediff : CompProperties_AbilityEffect {
        public HediffDef hediffDef;
        public float severity = 0f;
        public bool applyToCaster = true;
        public bool showFleck = true;

        public CompProperties_AbilityGiveHediff() {
            compClass = typeof(CompAbilityEffect_GiveHediff);
        }
    }

    // Класс для всех призываемых оружий - есть время жизни
    public class SummonedWeapon : ThingWithComps {
        public int ticksLeft = -1;

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", -1);
        }
    }

    // Класс для всех огненных призываемых оружий
    public class SummonedFireWeapon : SummonedWeapon { }

    // Класс для hediff, которые дают создают свет вокруг пешки
    public class Hediff_FireLight : HediffWithComps {
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

        // Каждый тик проверяем позицию пешки и перемещаем свет
        public override void Tick() {
            base.Tick();

            if (this.pawn == null || !this.pawn.Spawned || this.pawn.Map == null) {
                DespawnLight();
                return;
            }

            if (lightSource == null || !lightSource.Spawned) {
                SpawnLight();
            } else if (lightSource.Position != this.pawn.Position) {
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

    // Способность hunter7 (pyromaniac): тушение огня
    public class CompProperties_AbilityExtinguishFire : CompProperties_AbilityEffect {
        public float radius = 1f;
        public bool extinguishCaster = true;
        public bool extinguishAllies = true;
        public float spiritCostMultiplier = 1f;

        public CompProperties_AbilityExtinguishFire() {
            compClass = typeof(CompAbilityEffect_ExtinguishFire);
        }
    }

    public class CompAbilityEffect_ExtinguishFire : CompAbilityEffect {
        public new CompProperties_AbilityExtinguishFire Props => (CompProperties_AbilityExtinguishFire)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            Pawn caster = parent.pawn;
            if (caster == null || caster.Map == null) return;

            Map map = caster.Map;
            float radius = Props.radius;
            IntVec3 center = target.Cell;

            // 1. Тушение пожаров на клетках
            List<Thing> fireThings = map.listerThings.ThingsOfDef(ThingDefOf.Fire);
            foreach (Thing fire in fireThings.ToList()) {
                if (fire.Position.DistanceToSquared(center) <= radius * radius) {
                    fire.Destroy(DestroyMode.Vanish);
                }
            }

            // 2. Тушение горящих существ
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            foreach (Pawn pawn in pawns) {
                if (pawn == null || pawn.Dead) continue;
                if (pawn.Position.DistanceToSquared(center) > radius * radius) continue;

                if (pawn == caster && !Props.extinguishCaster) continue;
                if (pawn.Faction == caster.Faction && !Props.extinguishAllies) continue;

                if (pawn.IsBurning()) {
                    foreach (Thing thing in map.thingGrid.ThingsListAt(pawn.Position).ToList()) {
                        if (thing.def == ThingDefOf.Fire) {
                            thing.Destroy(DestroyMode.Vanish);
                        }
                    }
                    FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, 1.5f);
                }
            }

            // 3. Тушение горящих зданий
            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            foreach (Thing building in buildings) {
                if (building == null || building.Destroyed) continue;
                if (building.Position.DistanceToSquared(center) > radius * radius) continue;

                bool hasFire = false;
                foreach (Thing thing in map.thingGrid.ThingsListAt(building.Position).ToList()) {
                    if (thing.def == ThingDefOf.Fire) {
                        thing.Destroy(DestroyMode.Vanish);
                        hasFire = true;
                    }
                }
                if (hasFire) {
                    FleckMaker.ThrowSmoke(building.DrawPos, building.Map, 1.5f);
                }
            }

            /*
            // Визуальный эффект в центре
            for (int i = 0; i < 5; i++) {
                IntVec3 offset = new IntVec3(Rand.Range(-2, 2), 0, Rand.Range(-2, 2));
                Vector3 pos = (center + offset).ToVector3Shifted();
                FleckMaker.ThrowSmoke(pos, map, 0.8f);
            }
            */
        }
    }

    // Как подсветка для взрыва, но радиус достается из самой абилки, а не из снаряда
    public class Verb_ExtinguishFire : Verb_CastAbility {
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

            // Достаём радиус из CompProperties_AbilityExtinguishFire
            float radius = 1f; // по умолчанию
            if (this.ability != null) {
                var comp = this.ability.comps
                    .OfType<CompAbilityEffect_ExtinguishFire>()
                    .FirstOrDefault();
                if (comp != null)
                    radius = comp.Props.radius;
            }

            // Рисуем круг вокруг точки прицеливания
            Vector3 center = target.Cell.ToVector3Shifted();
            GenDraw.DrawRadiusRing(center.ToIntVec3(), radius, new Color(0.4f, 0.6f, 1f)); // голубоватый — цвет воды/тушения
        }
    }

        // Способность hunter6 (conspirator): огненное слияние (телепорт)
    public class CompProperties_FireTeleport : CompProperties_AbilityEffect {
        public ThingDef projectileDef;
        public float teleportSpeed = 50f;

        public CompProperties_FireTeleport() {
            compClass = typeof(CompAbilityEffect_FireTeleport);
        }
    }

    public class CompAbilityEffect_FireTeleport : CompAbilityEffect {
        public new CompProperties_FireTeleport Props => (CompProperties_FireTeleport)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            Pawn caster = parent.pawn;
            if (caster == null || !target.Cell.IsValid) return;

            Map map = caster.Map;
            IntVec3 startPos = caster.Position;

            ThingDef projectileDef = Props.projectileDef;
            if (projectileDef == null) return;

            Projectile_FireTeleport projectile = ThingMaker.MakeThing(projectileDef) as Projectile_FireTeleport;
            if (projectile == null) return;

            projectile.wasDrafted = caster.drafter.Drafted;
            projectile.teleportPawn = caster;

            projectile.Launch(
                launcher: caster,
                origin: startPos.ToVector3Shifted(),
                usedTarget: target,
                intendedTarget: target,
                hitFlags: ProjectileHitFlags.All
            );

            caster.DeSpawn();

            FleckMaker.ThrowSmoke(startPos.ToVector3(), map, 0.8f);
            FleckMaker.Static(startPos, map, FleckDefOf.MicroSparks, 1.0f);
        }
    }

    // Способность hunter6 (conspirator): подстрекание
    public class CompProperties_AbilityIncite : CompProperties_AbilityEffect {
        public float baseSuccessChance = 0.75f;
        public bool affectAllies = false;

        public CompProperties_AbilityIncite() {
            compClass = typeof(CompAbilityEffect_Incite);
        }
    }

    public class CompAbilityEffect_Incite : CompAbilityEffect {
        public new CompProperties_AbilityIncite Props => (CompProperties_AbilityIncite)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest) {
            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn == null || targetPawn.Dead) return;

            if (!Props.affectAllies && targetPawn.Faction == parent.pawn.Faction)
                return;

            float psychicSensitivity = targetPawn.GetStatValue(StatDefOf.PsychicSensitivity, true);
            float finalChance = Props.baseSuccessChance * psychicSensitivity;

            if (Rand.Chance(finalChance)) {
                Patch_RecordBerserkCaster.CurrentCaster = caster;

                targetPawn.mindState.mentalStateHandler.TryStartMentalState(
                    MentalStateDefOf.Berserk,
                    "Подстрекательство",
                    true
                );
                MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "Бунт!", 3f);

                if (targetPawn.RaceProps.ToolUser || targetPawn.RaceProps.IsMechanoid) {
                    var hediff = caster.health.hediffSet.GetFirstHediffOfDef(LotrDefOf.Hunter6_Hediff) as Hunter6_Hediff;

                    if (hediff != null) {
                        float severityIncrement = 0.01f;

                        hediff.AddActingProgress(1, severityIncrement, caster);
                    }
                }
            } else {
                float sanityPenalty = 0.10f;

                BeyonderUtility.AdjustSanityLoss(caster, sanityPenalty, "Подстрекательство провалено!");
            }
        }
    }

    // Способность hunter6 (conspirator): замешательство
    public class Hediff_Confusion : HediffWithComps {
        public override void PostAdd(DamageInfo? dinfo) {
            base.PostAdd(dinfo);

            if (pawn != null && pawn.Spawned && !pawn.Dead) {
                pawn.jobs.StopAll();

                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    MentalStateDefOf.Wander_Psychotic,
                    reason: "Эффект Замешательства",
                    forceWake: true,
                    transitionSilently: false
                );

                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "???", 3f);
            }
        }

        public override void PostRemoved() {
            base.PostRemoved();

            if (pawn != null && pawn.Spawned && pawn.InMentalState) {
                if (pawn.MentalStateDef == MentalStateDefOf.Wander_Psychotic) {
                    pawn.MentalState.RecoverFromState();
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Рассудок вернулся", 2.5f);
                }
            }
        }
    }

    // Способность hunter5 (reaper): жнец
    public class Hediff_ReaperState : HediffWithComps {
        public bool isReserved = false;
        public bool isExpended = false;

        public void ExpendCharge() {
            if (isExpended) return;
            isExpended = true;
        }

        // Автоматически удаляем хедифф в конце кадра/тика, когда все фазы урона прошли
        public override void Tick() {
            base.Tick();
            if (isExpended) {
                pawn.health.RemoveHediff(this);
            }
        }
    }

    // Способность hunter5 (reaper): уязвимость 
    public class Hediff_Vulnerable : HediffWithComps { }

}