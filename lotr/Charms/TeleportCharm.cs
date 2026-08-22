using RimWorld;

using Verse;
using Verse.AI;

namespace lotr {
    public class Verb_TeleportCharm : Verb_CastBase {
        protected override bool TryCastShot() {
            Pawn pawn = caster as Pawn;
            if (pawn == null || !pawn.Spawned || !currentTarget.IsValid)
                return false;

            Map map = pawn.Map;
            IntVec3 dest = currentTarget.Cell;

            // Если клетка заблокирована, ищем ближайшую свободную
            if (!IsCellTeleportable(dest, map, pawn)) {
                dest = FindTeleportableCellNear(dest, map, pawn);
                if (!dest.IsValid) {
                    Messages.Message("CannotTeleportToBlockedCell".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }

            // Выполняем телепортацию
            pawn.Position = dest;
            pawn.Notify_Teleported(true, true);

            // Можно добавить визуальный эффект (по желанию)
            // MoteMaker.MakeStaticMote(pawn.Position.ToVector3Shifted(), pawn.Map, ThingDefOf.Mote_Flash, 1f);

            CompApparelReloadable reloadableCompSource = base.ReloadableCompSource;
            if (reloadableCompSource != null)
                reloadableCompSource.UsedOnce();

            return true;
        }

        private bool IsCellTeleportable(IntVec3 cell, Map map, Pawn pawn) {
            if (!cell.InBounds(map))
                return false;
            if (!cell.Walkable(map))
                return false;
            if (cell.GetFirstPawn(map) != null)
                return false;
            return true;
        }

        private IntVec3 FindTeleportableCellNear(IntVec3 center, Map map, Pawn pawn, int maxRadius = 5) {
            // Обходим клетки по спирали от центра, возвращаем первую доступную
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, maxRadius, true)) {
                if (IsCellTeleportable(cell, map, pawn))
                    return cell;
            }
            return IntVec3.Invalid;
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter) {
            needLOSToCenter = false;
            return 1f; // подсветка одной клетки
        }
    }
}
