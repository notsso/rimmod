using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // определение для способности, которая тратит духовность
    public class Ability_SpendSpirituality : Ability {
        public Ability_SpendSpirituality() : base() { }

        public Ability_SpendSpirituality(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        // высчитывает сколько духовности тратит способность 
        public float AbilityCost() {
            float finalCost = 10f;

            SpiritualityCostExtension extension = this.def.GetModExtension<SpiritualityCostExtension>();

            if (extension != null) {
                finalCost = extension.cost;
            }

            // различные баффы/дебаффы к цене

            return finalCost;
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest) {
            bool result = base.Activate(target, dest);

            if (result) {
                Pawn caster = this.pawn;
                if (caster?.health != null) {
                    Need_Spirituality spirituality = this.pawn.needs.TryGetNeed(LotrDefOf.lotr_SpiritualityNeed) as Need_Spirituality; ;

                    if (spirituality != null) {
                        float cost = AbilityCost();

                        spirituality.CurLevel -= cost * 0.01f;

                        string textPct = $"-{(cost).ToString("F0")} Духовности";
                        if (caster.Spawned && caster.Map != null) {
                            MoteMaker.ThrowText(caster.DrawPos, caster.Map, textPct, 3f);
                        }
                    }
                }
            }

            return result;
        }

        // определяет, когда кнопка отвечающая за способность должна быть выключена
        public override bool GizmoDisabled(out string reason) {
            if (base.GizmoDisabled(out reason)) {
                return true;
            }

            Need_Spirituality spirituality = this.pawn.needs.TryGetNeed(LotrDefOf.lotr_SpiritualityNeed) as Need_Spirituality;

            if (spirituality == null) {
                reason = "Нет духовной энергии.";
                return true;
            }

            float cost = AbilityCost();

            if (spirituality.CurLevel < cost * 0.01f) {
                reason = $"Недостаточно духовности (Нужно {(cost).ToString("F0")}).";
                return true;
            }

            reason = null;
            return false;
        }
    }

    // Способность hunter7 (pyromaniac): огненный меч
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
}