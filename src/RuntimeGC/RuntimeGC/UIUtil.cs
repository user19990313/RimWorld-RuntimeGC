using System;
using Verse;
using UnityEngine;
using RimWorld;

namespace RuntimeGC
{
    internal static class UIUtil
    {
        public const float MarginLarge = 0f;
        public const float MarginHorizontal = 5f;
        public const float MarginVertical = 5f;

        public static float DrawSectionLabel(float x, float y, string text, float xMax)
        {
            Text.Font = GameFont.Medium;
            Vector2 size = Text.CalcSize(text);
            Rect rectLabel = new Rect(x + MarginLarge, y + MarginLarge, size.x, size.y);
            Widgets.Label(rectLabel, text);
            Color color = GUI.color;
            GUI.color = Color.gray;
            Widgets.DrawLineHorizontal(rectLabel.xMax + MarginHorizontal, rectLabel.y + rectLabel.height / 2, xMax - rectLabel.xMax - MarginHorizontal);
            Text.Font = GameFont.Small;
            GUI.color = color;
            return rectLabel.yMax + MarginVertical;
        }

        private static int resetFlags = 0;
        private static int resetFlagPtr = 0;

        public static void BeginRestartCheck()
        {
            resetFlagPtr = 0;
        }

        public static void DrawCheckboxRestartIfApplied(Rect rect,string label,string tip,ref bool checkOn)
        {
            if ((resetFlags & (1 << resetFlagPtr)) > 0)
            {
                checkOn = !checkOn;
                resetFlags ^= 1 << resetFlagPtr;
            }
            resetFlagPtr++;
            bool flag = checkOn;
            Widgets.CheckboxLabeled(rect, label, ref checkOn);
            TooltipHandler.TipRegion(rect, tip);
            if (flag != checkOn)
            {
                int i = resetFlagPtr - 1;
                if ((RuntimeGC.Settings.restartFlags & (1 << i)) > 0)
                {
                    RuntimeGC.Settings.restartFlags ^= 1 << i;
                    return;
                }
                Dialog_MessageBox dlg = new Dialog_MessageBox("DlgTextRestartNotice".Translate(), "OK".Translate(), delegate
                {
                    RuntimeGC.Settings.restartFlags |= 1 << i;
                }, "UndoChange".Translate(), delegate
                {
                    resetFlags |= 1 << i;
                },  "DlgTitleRestart".Translate());
                Find.WindowStack.Add(dlg);
            }
        }

        private static MainButtonWorker worker = null;
        public static MainButtonWorker MainButtonWorker
        {
            get
            {
                if (worker == null)
                {
                    worker = DefDatabase<MainButtonDef>.GetNamed("RGC_UI").Worker;
                }
                return worker;
            }
        }

        public static string MMIntervalButtonLabelCache = null;
        public static void Notify_MMBtnLabelChanged()
        {
            int interval = RuntimeGC.Settings.MemoryMonitorUpdateInterval;
            MMIntervalButtonLabelCache = Enum.IsDefined(typeof(MemoryMonitorUpdateMode), interval) ?
                ("MMUpdate_" + ((MemoryMonitorUpdateMode)interval).ToString()).Translate() : "UITicks".Translate(interval);
        }
    }
}
