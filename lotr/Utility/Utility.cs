using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Verse;
using Verse.AI;

using RimWorld;

using UnityEngine;

namespace lotr {
    public static class Utility {

        public static List<Pawn> GetPawnsInRadius(Pawn centerPawn, float radius) {

            if (centerPawn == null || centerPawn.Map == null) {
                return new List<Pawn>();
            }

            List<Pawn> foundPawns = new List<Pawn>();
            Map map = centerPawn.Map;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(centerPawn.Position, radius, true)) {

                if (cell.InBounds(map)) {

                    List<Thing> thingList = map.thingGrid.ThingsListAt(cell);
                    for (int i = 0; i < thingList.Count; i++) {
                        if (thingList[i] is Pawn pawn && pawn != centerPawn) {
                            foundPawns.Add(pawn);
                        }
                    }
                }
            }

            return foundPawns;
            
        }

    }

}