using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Mute
{
    public static class Launcher
    {
        #region Replacement
        public static void WorldPawnGCTick()
        {
        }

        public static void Add(LogEntry entry)
        {
        }
        public static void ExposeData()
        {
            List<Battle> emptylist = new List<Battle>();
            Scribe_Collections.Look(ref emptylist, "battles", LookMode.Deep);
        }
        #endregion

        public static void Launch(bool doGC,bool doBL)
        {
            BindingFlags bflag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
            BindingFlags bflag2 = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;
            if (doGC)
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    DoDetour(typeof(RimWorld.Planet.WorldPawnGC).GetMethod("WorldPawnGCTick", bflag), typeof(Launcher).GetMethod("WorldPawnGCTick", bflag2));
                    Verse.Log.Message("[RuntimeGC] Detour completed: MuteGC");
                }, "Initializing", false, null);
            }
            if (doBL)
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    DoDetour(typeof(Verse.BattleLog).GetMethod("Add", bflag), typeof(Launcher).GetMethod("Add", bflag2));
                    DoDetour(typeof(Verse.BattleLog).GetMethod("ExposeData", bflag), typeof(Launcher).GetMethod("ExposeData", bflag2));
                    Verse.Log.Message("[RuntimeGC] Detour completed: MuteBL");
                }, "Initializing", false, null);
            }
        }

        public unsafe static bool DoDetour(MethodInfo source, MethodInfo destination)
        {
            if (IntPtr.Size == 8)
            {
                byte* arg_136_0 = (byte*)source.MethodHandle.GetFunctionPointer().ToInt64();
                long num = destination.MethodHandle.GetFunctionPointer().ToInt64();
                byte* ptr = arg_136_0;
                long* ptr2 = (long*)(ptr + 2);
                *ptr = 72;
                ptr[1] = 184;
                *ptr2 = num;
                ptr[10] = 255;
                ptr[11] = 224;
            }
            else
            {
                int num2 = source.MethodHandle.GetFunctionPointer().ToInt32();
                int arg_1A6_0 = destination.MethodHandle.GetFunctionPointer().ToInt32();
                byte* ptr3 = (byte*)num2;
                int* ptr4 = (int*)(ptr3 + 1);
                int num3 = arg_1A6_0 - num2 - 5;
                *ptr3 = 233;
                *ptr4 = num3;
            }
            return true;
        }
    }
}