using RimWorld;

using Verse;

namespace lotr {
    public class Plant_BloodRedChestnut : Plant {
        public override void PlantCollected(Pawn by, PlantDestructionMode plantDestructionMode) {
            // Спавним дополнительный ингредиент
            Thing ingredient = ThingMaker.MakeThing(ThingDef.Named("lotr_BloodRedChestnut"));
            ingredient.stackCount = 1;
            GenPlace.TryPlaceThing(ingredient, Position, Map, ThingPlaceMode.Near);

            // Уничтожаем дерево (после того, как древесина уже получена)
            this.Destroy(DestroyMode.KillFinalizeLeavingsOnly);
        }
    }
}
