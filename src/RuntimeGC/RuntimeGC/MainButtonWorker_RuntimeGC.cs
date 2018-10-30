using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace RuntimeGC
{
    internal enum MemoryMonitorUpdateMode
    {
        Debug_Flash=1,
        Debug_Realtime=15,
        PerSecond=60,
        UltraFrequent=150,
        Frequent=300,
        Moderate=600,
        PerMinuteQuarter=900,
        PerMinuteHalf=1800,
        PerMinute=3600,
        Debug_Lazy=18000,
        Debug_frozen=2147483647
    }

    public class MainButtonWorker_RuntimeGC:MainButtonWorker_ToggleTab
    {
        private static bool enableBar;
        private static int memoryBarLowerMb;
        private static int memoryBarStepMb;
        private static bool enableTip;
        
        public static int updateInterval = (int)MemoryMonitorUpdateMode.Moderate;
        public static bool onScreenMemUsage = false;

        internal static string TabDescriptionTranslated;
        internal static string MMTipTranslated;

        private static float progress = 0f;
        private static string tipCache = "";
        private static int updatetick = 32767;
        private static string labelCache = "";

        internal static void UpdateSettings(RuntimeGCSettings settings)
        {
            enableBar = settings.EnableMemoryMonitorBar;
            enableTip = settings.EnableMemoryMonitorTip;
            memoryBarLowerMb = settings.MemoryMonitorBarLowerBoundMb;
            memoryBarStepMb = settings.MemoryMonitorBarUpperBoundMb - memoryBarLowerMb;
            updateInterval = settings.MemoryMonitorUpdateInterval;
            onScreenMemUsage = settings.DevOnScreenMemoryUsage;
            progress = 0f;
            tipCache = "";
            updatetick = 32767;
        }

        internal static void Notify_UpdateIntervalChanged(int newint)
        {
            updateInterval = newint;
            updatetick = 32767;
        }

        public override void DoButton(Rect rect)
        {
            Text.Font = GameFont.Small;
            string text = def.LabelCap;
            float num = def.LabelCapWidth;
            if (num > rect.width - 2f)
            {
                text = def.ShortenedLabelCap;
                num = def.ShortenedLabelCapWidth;
            }
            
            if (enableBar||enableTip||onScreenMemUsage)
            {
                updatetick++;
                if (updatetick > updateInterval)
                {
                    updatetick = 0;

                    long mem = GC.GetTotalMemory(false) / 1024;
                    float memMb = mem / 1024f;
                    if (enableTip)
                    {
                        tipCache = string.Format(MMTipTranslated, memMb);
                    }
                    if (enableBar)
                    {
                        progress = Mathf.Clamp01((memMb - memoryBarLowerMb) / memoryBarStepMb);
                    }
                    if (onScreenMemUsage)
                    {
                        labelCache = string.Format("{0:F2} Mb\n{1} Kb", memMb, mem);
                    }
                }
            }

            bool flag = num > 0.85f * rect.width - 1f;
            string label = onScreenMemUsage ? labelCache : text;
            float textLeftMargin = (!flag) ? -1f : 2f;
            if (Widgets.ButtonTextSubtle(rect, label, progress, textLeftMargin, SoundDefOf.Mouseover_Category, default(Vector2)))
            {
                if(Current.ProgramState== ProgramState.Playing)
                    InterfaceTryActivate();
            }

            TooltipHandler.TipRegion(rect, TabDescriptionTranslated + tipCache);
        }
    }
}
