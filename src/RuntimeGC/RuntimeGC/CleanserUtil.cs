using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Reflection;
using RimWorld.Planet;

namespace RuntimeGC
{
    internal static class CleanserUtil
    {
        //Shared Reflection access.
        private static FieldInfo field = typeof(RimWorld.Pawn_RelationsTracker).GetField("pawnsWithDirectRelationsWithMe", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo battles = typeof(BattleLog).GetField("battles", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo activeEntries = typeof(BattleLog).GetField("activeEntries", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo archivables = typeof(Archive).GetField("archivables", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo pinnedArchivables = typeof(Archive).GetField("pinnedArchivables", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo forceNormalSpeedUntil=typeof(TimeSlower).GetField("forceNormalSpeedUntil", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo selMod = typeof(Dialog_ModSettings).GetField("selMod", BindingFlags.NonPublic | BindingFlags.Instance);

        public static IEnumerable<Pawn> getPawnsWithDirectRelationsWithMe(Pawn_RelationsTracker r)
        {
            return (HashSet<Pawn>)field.GetValue(r);
        }

        public static int RemoveAllBattleLogEntries()
        {
            int a = 0;
            foreach (Battle b in Find.BattleLog.Battles)
                a += b.Entries.CountAllowNull();
            battles.SetValue(Find.BattleLog, new List<Battle>(20));
            activeEntries.SetValue(Find.BattleLog, null);
            return a;
        }


        private static WorldPawnCleaner gcobject = new WorldPawnCleaner();

        public static WorldPawnCleaner GCObject
        {
            get
            {
                if (CleanserUtil.gcobject == null)
                    CleanserUtil.gcobject = new WorldPawnCleaner();
                return CleanserUtil.gcobject;
            }
        }


        /// <summary>
        /// Manual finalizer for WorldPawnCleaner.GC().
        /// Deconstruct all animal families on map and discard the redundant members.
        /// </summary>
        /// <returns>The count of discarded members.</returns>
        public static int DeconstructAnimalFamily()
        {
            List<Pawn> worldpawns = new List<Pawn>();
            foreach (Pawn p in Find.WorldPawns.AllPawnsAliveOrDead)
                worldpawns.Add(p);
            
            List<Pawn> queue = new List<Pawn>();
            List<Pawn> pawnlist = new List<Pawn>();
            foreach (Map map in Find.Maps)
                foreach (Pawn p in map.mapPawns.AllPawns)
                    if ((p.records.GetAsInt(RecordDefOf.TimeAsColonistOrColonyAnimal) > 0) && !(p.RaceProps.Humanlike))
                        pawnlist.Add(p);

            foreach (Pawn p in pawnlist)
                //Patch:null relationship on robots.
                if (p.relations != null)
                    foreach (Pawn p2 in expandRelation(p))
                    if (worldpawns.Contains(p2) && (!p2.Spawned) && (!p2.IsPlayerControlledCaravanMember()) && (!PawnUtility.IsTravelingInTransportPodWorldObject(p2))
                        //Patch:Corpses remained on maps.
                        && (p.Corpse == null)
                        )
                    {
                        queue.Add(p2);
                        worldpawns.Remove(p2);
                    }


            //Patch:2nd Pawn of a used Tale_DoublePawn will raise Scribe Warnings if discarded
            List<Pawn> allUsedTaleOwner;
            CleanserUtil.InitUsedTalePawns(out allUsedTaleOwner);
            foreach (Pawn pawn in allUsedTaleOwner)
                queue.Remove(pawn);

            int a = queue.Count;
            foreach(Pawn pawn in queue)
            {
                Find.WorldPawns.RemovePawn(pawn);
                if (!pawn.Destroyed)
                    pawn.Destroy(DestroyMode.Vanish);
                if (!pawn.Discarded)
                    pawn.Discard(true);
            }

            Find.WindowStack.WindowOfType<UserInterface>().Notify_PawnsCountDirty();
            return a;
        }

        private static IEnumerable<Pawn> expandRelation(Pawn p)
        {
            foreach (DirectPawnRelation r in p.relations.DirectRelations)
                yield return r.otherPawn;

            foreach (Pawn p2 in getPawnsWithDirectRelationsWithMe(p.relations))
                yield return p2;
        }

        /// <summary>
        /// Remove all filth in home area.Won't raise errors even the filth is queued up by a Toil.
        /// </summary>
        /// <returns>The count of filth being cleaned.</returns>
        public static int RemoveFilth(Map map, bool homearea)
        {
            if (map == null || map.listerFilthInHomeArea == null || map.listerThings == null) return 0;
            int a = 0;
            Filth f;
            List<Thing> filth;
            if (homearea)
                filth = map.listerFilthInHomeArea.FilthInHomeArea;
            else filth = map.listerThings.ThingsInGroup(ThingRequestGroup.Filth);
            a += filth.Count;

            for (int i = filth.Count - 1; i > -1; i--)
            {
                f = (Filth)filth[i];
                f.DeSpawn();
                if (!f.Destroyed)
                    f.Destroy(DestroyMode.Vanish);
                if (!f.Discarded)
                {
                    Verse.Log.Warning("A thing_filth_object destroyed before is not discarded!That\'s wierd.");
                    f.Discard();
                }
            }
            return a;
        }

        /// <summary>
        /// Fix faction relationships.This will fix the error "Dummy Relation".
        /// </summary>
        public static void FixFactionRelationships()
        {
            List<Faction> factions = Find.FactionManager.AllFactionsListForReading;
            foreach(Faction f in factions)
            {
                for(int i = 0; i < factions.Count; i++)
                {
                    if (factions[i] != f)
                        f.TryMakeInitialRelationsWith(factions[i]);
                }
            }
        }


        /// <summary>
        /// Please use FixFactionLeader_Wrapped instead.
        /// Generates a leader for null-leader factions.
        /// Spacers factions will also be generated,but they will remain null-leadered.
        /// Return value is inconclusive.
        /// </summary>
        /// <returns>Leaders generated,but it is inconclusive.</returns>
        public static int FixFactionLeader()
        {
            int a = 0;
            foreach(Faction f in Find.FactionManager.AllFactionsVisible)
            {
                if (f.leader == null || f.leader.Dead || f.leader.Destroyed)
                {
                    f.GenerateNewLeader();
                    a++;
                }
            }

            Find.WindowStack.WindowOfType<UserInterface>().Notify_PawnsCountDirty();
            return a;
        }

        /// <summary>
        /// Try to generate a leader for null-leader factions.
        /// Factions without leader slot will also be processed,but they will remain null-leadered.
        /// </summary>
        /// <returns>Leaders generated for null-leader factions.</returns>
        public static int FixFactionLeader_Wrapped()
        {
            int a = FixFactionLeader();
            int b = FixFactionLeader();
            return a - b;
        }

        /// <summary>
        /// Remove all corpses in currently-active map.
        /// Won't raise errors even the corpse is being burned.Corpses hauled by other pawns will be remained.
        /// </summary>
        /// <returns>The count of corpses being removed.</returns>
        public static int RemoveCorpses()
        {
            Map map = Find.CurrentMap;
            if (map == null) return 0;
            List<Thing> list= map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            Corpse c;
            int a = list.Count;
            for (int i = list.Count-1; i > -1; i--)
            {
                c = (Corpse)list[i];
                c.DeSpawn();
                if (!c.Destroyed)
                    c.Destroy(DestroyMode.Vanish);
                if (!c.Discarded)
                    c.Discard();
            }
            Find.WindowStack.WindowOfType<UserInterface>().Notify_PawnsCountDirty();
            return a;
        }
        
        /// <summary>
        /// Form a list containing all pawns concerned by used tales.
        /// </summary>
        public static void InitUsedTalePawns(out List<Pawn> concernedPawns)
        {
            //Patch:TaleType.PermanentHistorical
            List<Tale> usedTales = Find.TaleManager.AllTalesListForReading.FindAll(t => t.Uses > 0 || t.def.type == TaleType.PermanentHistorical);
            List<Pawn> pawnlist = new List<Pawn>();
            int count = usedTales.Count;
            int i = 0;
            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                try
                {
                    for (i = 0; i < count; i++)
                        if (usedTales[i].Concerns(pawn))
                        {
                            pawnlist.Add(pawn);
                            break;
                        }
                }catch(System.Exception e)
                {
                    Verse.Log.Error("Exception in InitUsedTalePawns with Tale id=" + usedTales[i].id + " :\n" + e.ToString());
                    pawnlist.Add(pawn);
                }
            }
            concernedPawns = pawnlist;
        }

        public static void RemoveSnow(Map map, bool homearea)
        {
            if (map == null || map.snowGrid == null || map.mapDrawer == null) return;
            if (homearea)
            {
                SnowGrid grid = map.snowGrid;
                foreach (IntVec3 c in map.areaManager.Home.ActiveCells)
                    grid.SetDepth(c, 0f);
            }
            else
            {
                map.snowGrid = new SnowGrid(map);
                map.mapDrawer.WholeMapChanged(MapMeshFlag.Snow);
                map.mapDrawer.WholeMapChanged(MapMeshFlag.Things);
                //Patch: Pathfinding cost update
                map.pathGrid.RecalculateAllPerceivedPathCosts();
            }
        }

        public static int RemoveIArchivable(bool removePinned)
        {
            if (Find.Archive == null) return 0;
            List<IArchivable> list = new List<IArchivable>();
            if (!removePinned)
            {
                list.AddRange((HashSet<IArchivable>)pinnedArchivables.GetValue(Find.Archive));
                list.SortBy(iA => iA.CreatedTicksGame);
            }
            int a = ((List<IArchivable>)archivables.GetValue(Find.Archive)).CountAllowNull() - list.Count;
            archivables.SetValue(Find.Archive, list);
            if (removePinned)
                pinnedArchivables.SetValue(Find.Archive, new HashSet<IArchivable>());
            return a;
        }

        public static void UnlockNormalSpeedLimit()
        {
            forceNormalSpeedUntil.SetValue(Find.TickManager.slower, Find.TickManager.TicksGame);
        }

        public static void OpenModSettingsPage()
        {
            Dialog_ModSettings dlg = new Dialog_ModSettings();
            Mod gc = LoadedModManager.ModHandles.FirstOrFallback((Mod m) => m is RuntimeGC);
            selMod.SetValue(dlg, gc);
            Find.WindowStack.Add(dlg);
        }
    }
}
