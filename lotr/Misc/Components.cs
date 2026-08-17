using System.Collections.Generic;
using System.Linq;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    // Компонент: при смерти уничтожает труп и объект
    public class CompProperties_DeathVanish : CompProperties {
        public CompProperties_DeathVanish() {
            compClass = typeof(Comp_DeathVanish);
        }
    }

    public class Comp_DeathVanish : ThingComp {
        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null) {
            base.Notify_Killed(prevMap, dinfo);

            Pawn pawn = parent as Pawn;
            if (pawn == null) return;

            // Уничтожаем труп, если он уже создан
            if (pawn.Corpse != null && !pawn.Corpse.Destroyed) {
                pawn.Corpse.Destroy(DestroyMode.Vanish);
            }

            // Уничтожаем самого родителя, если он ещё существует
            if (!parent.Destroyed) {
                parent.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
