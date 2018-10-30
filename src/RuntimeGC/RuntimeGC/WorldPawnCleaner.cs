using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace RuntimeGC
{
    public class WorldPawnCleaner
    {
        static string CopyrightStr = "RuntimeGC for 1.0,user19990313,Baidu Tieba&Ludeon forum";

        ///Verbosity
        private Dictionary<Pawn, int> allPawnsCounter = new Dictionary<Pawn, int>();
        private Dictionary<Flags, int> allFlagsCounter = new Dictionary<Flags, int>();
        private bool verbose = false;
        ///Debug only.Well,useless.
        private bool debug = false;

        enum Flags
        {
            Colonist = 1,
            Prisoner = 2,
            FactionLeader = 8,
            KeptWorldPawn = 16,
            CorpseOwner = 4,
            RelationLvl0 = 32,
            RelationLvl1 = 64,
            RelationLvl2 = 128,
            TaleEntryOwner = 256,
            OnSale = 512,
            Animal = 1024,
            None = 0
        }
        private static int FlagsCountNotNull = Enum.GetNames(typeof(Flags)).Length - 1;

        List<Pawn> reference;
        Dictionary<Pawn, Flags> allFlags = new Dictionary<Pawn, Flags>();


        /// <summary>
        /// GC().The best way to shrink Rimworld saves,I think.
        /// </summary>
        /// <param name="verbose">Determine if GC() should log details very very verbosely</param>
        /// <returns>Count of disposed World pawns</returns>
        public int GC(bool verbose = false)
        {
            /*
              TODO Log
              1.talelog by interest             -X
              2.animal  - deconstruct relation  -Done
              3.deeperclean?remove hediffs      -X
              5.correct verbose log             -Done
              6.yield return "status"           -X
              7.UI compability                  -Done
              8.Filth cleaner                   -Done
              9.Fix:Faction Leader              -Done
              10.Fix:Faction Relations          -Done
              12.Warp->Wrap                     -Done
              */

            /*
              TODO A18
                4.adjustable GC depth           -X
                11.GC boostup                   -Done
                13.remake GC System             -Done
                13.Keyed in Float menu items    -Done
                14.help contents                -Done
                15.Optimize Cleanser frame      -Done
                16.Optimize Floatmenu System    -Done
                17.Debug only options           -Done
            */

            /*
              TODO 1.0
                18.Clean snow                   -Done
                19.whole-map clean              -Done
                20.remake log                   -Done
                21.Mod framework                -Done
                22.settings                     -Done
                23.MuteGC                       -Done
                24.MuteCL                       -Done
                25.remake FloatMenuUtil         -Done
                26.timer of gc                  -X
                27.toolbox integration          -Done
                28.MainButtonDef into xml       -Done
                29.try catch                    -Done
                30.Find.CurrentMap==null check  -Done
                31.MainButtonWorker             -Done
                32.Messages.Message(str,historical) settings -Done
                33.AvoidGrid rework             -Done
                34.Faction rework & cleanup     -Done
                35.Close letter stack           -Done
            */

            if (Current.ProgramState != ProgramState.Playing)
            {
                Verse.Log.Error("You must be kidding me...GC a save without loading one?");
                return 0;
            }

            /*Initialization*/
            Verse.Log.Message("[GC Log] Pre-Initializing GC...");
            this.reference = Find.WorldPawns.AllPawnsAliveOrDead.ToList();
            this.allFlags.Clear();
            this.verbose = verbose;
            if (verbose)
            {
                allFlagsCounter.Clear();
                allFlagsCounter.Add(Flags.None, 0);
                for (int j = 0; j < FlagsCountNotNull; j++)
                    allFlagsCounter.Add((Flags)(1 << j), 0);
            }

            /*Generate EntryPoints from Map Pawns*/
            Verse.Log.Message("[GC Log] Generating EntryPoints from Map Pawns...");
            List<Pawn> mapPawnEntryPoints;
            DiagnoseMapPawns(out mapPawnEntryPoints);
            if (verbose) Verse.Log.Message("[GC Log][Verbose] " + allPawnsCounter.Count().ToString() + " Map Pawns marked during diagnosis");

            /*Reset counters*/
            allPawnsCounter.Clear();
            if (verbose)
            {
                allFlagsCounter.Clear();
                allFlagsCounter.Add(Flags.None, 0);
                for (int j = 0; j < FlagsCountNotNull; j++)
                    allFlagsCounter.Add((Flags)(1 << j), 0);
            }

            /*Generate a list of pawns concerned by used Tales*/
            Verse.Log.Message("[GC Log] Collecting Pawns concerned by Used Tales...");
            List<Pawn> allUsedTalePawns;
            CleanserUtil.InitUsedTalePawns(out allUsedTalePawns);
            
            /*Diagnosis:marking entries on WorldPawns.*/
            Verse.Log.Message("[GC Log] Running diagnosis on WorldPawns...");
            foreach (Pawn p in reference)
            {
                if (p.IsColonist) addFlag(p, Flags.Colonist | Flags.RelationLvl2);
                if (p.IsPrisonerOfColony) addFlag(p, Flags.Prisoner | Flags.RelationLvl2);
                if (PawnUtility.IsFactionLeader(p)) addFlag(p, Flags.KeptWorldPawn | Flags.FactionLeader | Flags.RelationLvl1);
                if (PawnUtility.IsKidnappedPawn(p)) addFlag(p, Flags.KeptWorldPawn | Flags.RelationLvl2);
                if (p.Corpse != null) addFlag(p, Flags.CorpseOwner | Flags.RelationLvl1);
                if (allUsedTalePawns.Contains(p)) addFlag(p, Flags.TaleEntryOwner | Flags.RelationLvl0);

                if (p.InContainerEnclosed) addFlag(p, Flags.KeptWorldPawn | Flags.RelationLvl0);
                if (p.Spawned) addFlag(p, Flags.RelationLvl0);
                if (p.IsPlayerControlledCaravanMember()) addFlag(p, Flags.KeptWorldPawn | Flags.RelationLvl2);
                if (PawnUtility.IsTravelingInTransportPodWorldObject(p)) addFlag(p, Flags.KeptWorldPawn | Flags.RelationLvl2);

                //Patch:A18 new entry
                if (PawnUtility.ForSaleBySettlement(p)) addFlag(p, Flags.OnSale | Flags.RelationLvl0);

                if (verbose) Verse.Log.Message("[worldPawn] " + p.LabelShort + " [flag] " + markedFlagsString(p));
            }

            if (verbose) Verse.Log.Message("[GC Log][Verbose] "+allPawnsCounter.Count().ToString()+" World Pawns marked during diagnosis");
            

            int i;
            /*Expansion 1:Expand relation network from map pawns.*/
            Verse.Log.Message("[GC Log] Expanding Relation networks through Map Pawn Entry Points...");
            for (i = mapPawnEntryPoints.Count - 1; i > -1; i--)
                if (containsFlag(mapPawnEntryPoints[i], Flags.RelationLvl2))
                {
                    expandRelation(mapPawnEntryPoints[i], Flags.RelationLvl1);
                    mapPawnEntryPoints.RemoveAt(i);
                }

            for (i = mapPawnEntryPoints.Count - 1; i > -1; i--)
                if (containsFlag(mapPawnEntryPoints[i], Flags.RelationLvl1))
                {
                    expandRelation(mapPawnEntryPoints[i], Flags.RelationLvl0);
                    mapPawnEntryPoints.RemoveAt(i);
                }

            /*Its unnecessary to process RelationLvl0 in mapPawnEntryPoints,
              for they are not related to any world pawns.
              */

            /*Expansion 2:Expand relation network from world pawns.*/
            Verse.Log.Message("[GC Log] Expanding Relation networks on marked World Pawns...");
            for (i = reference.Count - 1; i > -1; i--)
                if (containsFlag(reference[i], Flags.RelationLvl2))
                {
                    expandRelation(reference[i], Flags.RelationLvl1);
                    reference.RemoveAt(i);
                }

            for (i = reference.Count - 1; i > -1; i--)
                if (containsFlag(reference[i], Flags.RelationLvl1))
                {
                    expandRelation(reference[i], Flags.RelationLvl0);
                    reference.RemoveAt(i);
                }

            for (i = reference.Count - 1; i > -1; i--)
                if (containsFlag(reference[i], Flags.RelationLvl0))
                    reference.RemoveAt(i);


            int a = 0;
            /*VerboseMode:counting addFlag() calls.*/
            if (verbose)
            {
                foreach (KeyValuePair<Pawn, int> p in allPawnsCounter)
                    a += p.Value;
                Verse.Log.Message("[GC Log][Verbose] " + allPawnsCounter.Count().ToString() + " World Pawns marked during Expanding");
                if(debug) Verse.Log.Message("addFlag() called " + a + " times");
            }


            /*Posfix:remove UsedTalePawns.*/
            Verse.Log.Message("[GC Log] Excluding Pawns concerned by Used Tales...");
            foreach (Pawn p in allUsedTalePawns)
                reference.Remove(p);


            /*GC Core:dispose all pawns left in reference list.*/
            Verse.Log.Message("[GC Log] Disposing World Pawns...");
            a = reference.Count;
            Pawn pawn;
            for (i = reference.Count - 1; i > -1; i--)
            {
                pawn = reference[i];
                //Patch:Mysterious WorldPawn.missing
                //Update:This patch is disabled due to safety concerns.
                //if(Find.WorldPawns.Contains(pawn))
                Find.WorldPawns.RemovePawn(pawn);
                if (!pawn.Destroyed)
                    pawn.Destroy(DestroyMode.Vanish);
                if (!pawn.Discarded)
                    pawn.Discard(true);
            }

            /*VerboseMode:Finalize output*/
            if (verbose)
            {
                string s = "[GC Log][Verbose] Flag calls stat:";
                allFlagsCounter.Remove(Flags.None);
                foreach (KeyValuePair<Flags, int> pair in allFlagsCounter)
                    s += "\n  " + pair.Key.ToString() + " : " + pair.Value;
                Verse.Log.Message(s);
            }

            Verse.Log.Message("[GC Log] GC() completed with "+a+" World Pawns disposed");

            return a;
        }

        private Flags getFlag(Pawn pawn)
        {
            return allFlags.ContainsKey(pawn) ? allFlags[pawn] : Flags.None;
        }

        private bool containsFlag(Pawn pawn, Flags flag)
        {
            return containsFlag(getFlag(pawn), flag);
        }

        private bool containsFlag(Flags f1, Flags f2)
        {
            return (f1 & f2) != Flags.None;
        }

        private IEnumerable<Flags> splitFlag(Flags flag)
        {
            int f = 1;
            for (int j = 0; j < FlagsCountNotNull; j++)
            {
                if (containsFlag(flag, (Flags)(f = f << 1)))
                    yield return (Flags)f;
            }
            yield return Flags.None;
        }

        private string markedFlagsString(Pawn p)
        {
            List<Flags> flags = splitFlag(getFlag(p)).ToList();
            string str = "";
            int i = 0;
            for (; i < flags.Count-2; i++)
            {
                str += flags[i].ToString();
                str += "|";
            }
            str += flags[i].ToString();
            return str;
        }

        private void addFlag(Pawn pawn, Flags flag)
        {
            if (!allFlags.ContainsKey(pawn))
                allFlags.Add(pawn, flag);
            else allFlags[pawn] |= flag;

            if (verbose)
            {
                if (!allPawnsCounter.ContainsKey(pawn))
                    allPawnsCounter.Add(pawn, 1);
                else allPawnsCounter[pawn] += 1;

                foreach (Flags f in splitFlag(flag))
                    allFlagsCounter[f] += 1;
            }
        }

        private void expandRelation(Pawn p, Flags flag)
        {
            //Patch:null Relation_tracker for mechanoids & insects.
            if (p.relations == null) return;

            if (debug)
            {
                foreach (Pawn p0 in p.relations.FamilyByBlood)
                    foreach (PawnRelationDef d in p.GetRelations(p0))
                        Verse.Log.Message("(Family) " + p.LabelShort + " <" + d.label + "> " + p0.LabelShort);
            }

            foreach (DirectPawnRelation r in p.relations.DirectRelations)
                if (reference.Contains(r.otherPawn))
                {
                    addFlag(r.otherPawn, flag);
                    if (verbose) Verse.Log.Message("(Relation) " + p.LabelShort + " <" + r.def.label + "> " + r.otherPawn.LabelShort);
                }

            foreach (Pawn p2 in CleanserUtil.getPawnsWithDirectRelationsWithMe(p.relations))
            {
                if (reference.Contains(p2) && p2.GetRelations(p).Count<PawnRelationDef>() > 0)
                {
                    addFlag(p2, flag);
                    if (verbose) Verse.Log.Message("(Reflexed) <" + p2.LabelShort + "," + p.LabelShort + ">");
                }
            }
        }

        private void DiagnoseMapPawns(out List<Pawn> mapPawnEntryPoints)
        {
            List<Pawn> pawnlist = new List<Pawn>();
            foreach (Map map in Find.Maps)
                foreach (Pawn p in map.mapPawns.AllPawns)
                {
                    if (p.IsColonist) addFlag(p, Flags.Colonist | Flags.RelationLvl2);
                    if (p.IsPrisonerOfColony) addFlag(p, Flags.Prisoner | Flags.RelationLvl2);
                    if (PawnUtility.IsFactionLeader(p)) addFlag(p, Flags.FactionLeader | Flags.RelationLvl1);
                    if (PawnUtility.IsKidnappedPawn(p)) addFlag(p, Flags.KeptWorldPawn | Flags.RelationLvl2);
                    if (p.Corpse != null) addFlag(p, Flags.CorpseOwner | Flags.RelationLvl1);

                    /*Outsider caravan member patch*/
                    if (p.RaceProps.Humanlike) addFlag(p, Flags.RelationLvl1);

                    if (allFlags.ContainsKey(p))
                    {
                        pawnlist.Add(p);
                        if (verbose) Verse.Log.Message("[mapPawn] " + p.LabelShort + " [flag] " + markedFlagsString(p));
                    }

                    ///Unused options
                    //if ((p.records.GetAsInt(RecordDefOf.TimeAsColonistOrColonyAnimal) > 0) && !(p.RaceProps.Humanlike)) addFlag(p, Flags.Animal | Flags.RelationLvl0);
                    //if (allUsedTaleOwner.Contains(p)) addFlag(p, Flags.TaleEntryOwner | Flags.RelationLvl0);
                    //if ((p.Name!=null) &&(!p.Name.Numerical)&&p.Name.ToStringFull.Contains("Serir")) Verse.Log.Message("Pawn:" + p.Name+",flag="+(allFlags.ContainsKey(p)? allFlags[p].ToString():"null"));
                    //if (p.InContainerEnclosed) addFlag(p, Flags.KeptWorldPawn | Flags.RelationLvl0);
                    //if (p.Spawned) addFlag(p, Flags.RelationLvl0);
                }
            mapPawnEntryPoints = pawnlist.ToList<Pawn>();
        }

        public void DisposeTmpForSystemGC()
        {
            this.reference = null;
            this.allPawnsCounter.Clear();
            this.allFlags.Clear();
            this.allFlagsCounter.Clear();
        }
    }
}
