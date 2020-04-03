using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RuntimeGC
{
    public class UserInterface : MainTabWindow
    {
        private int pawnsAliveCount;
        private int pawnsDeadCount;
        private bool pawnsCountDirty = true;

        public override Vector2 RequestedTabSize
        {
            get
            {
                return new Vector2(350f, 500f);
            }
        }

        public int PawnsAliveCount
        {
            get
            {
                if (pawnsCountDirty)
                {
                    pawnsCountDirty = false;
                    pawnsAliveCount = Find.WorldPawns.AllPawnsAlive.Count();
                    pawnsDeadCount = Find.WorldPawns.AllPawnsDead.Count();
                }
                return pawnsAliveCount;
            }
        }

        public int PawnsDeadCount
        {
            get
            {
                if (pawnsCountDirty)
                {
                    pawnsCountDirty = false;
                    pawnsAliveCount = Find.WorldPawns.AllPawnsAlive.Count();
                    pawnsDeadCount = Find.WorldPawns.AllPawnsDead.Count();
                }
                return pawnsDeadCount;
            }
        }

        public override void DoWindowContents(Rect canvas)
        {
            UnityEngine.GUI.BeginGroup(canvas);
            Listing_Standard std = new Listing_Standard();
            std.Begin(canvas);
            Text.Font = GameFont.Medium;
            std.Label("RuntimeGCTitle".Translate());
            Text.Font = GameFont.Small;
            std.Label("RuntimeGCVer".Translate("1.1"));
            std.Label("By user19990313");
            std.Gap();
            std.Label(string.Concat(new object[]{"pawnsAlive:",
                                                PawnsAliveCount,
                                                " pawnsDead:",
                                                PawnsDeadCount
                                                }));
            std.Gap();
            float f = std.CurHeight;
            std.End();

            this.DoComponents(new Rect(canvas.x+35f, canvas.y + f, canvas.width, canvas.height - f));
            this.DoHelpContents(new Rect(canvas.x, canvas.y + f, canvas.width, canvas.height - f));

            UnityEngine.GUI.EndGroup();
        }

        public void DoComponents(Rect rect)
        {
            UnityEngine.GUI.BeginGroup(rect);
            Rect rect2 = new Rect(0f, 0f, 170f, rect.height);
            Text.Font = GameFont.Small;
            List<ListableOption> list = new List<ListableOption>();

            list.Add(new ListableOption("BtnTextGCWP".Translate(), delegate {
                GC_wrapped();
            }, null));

            list.Add(new ListableOption("BtnTextGCV".Translate(), delegate {
                GC_wrapped(true);
            }, null));

            list.Add(new ListableOption("BtnTextAddOn".Translate(), delegate {
                FloatMenuUtil.GenerateFloatMenuGroup(FloatMenuUtil.GroupTools);
            }, null));

            list.Add(new ListableOption("BtnTextFix".Translate(), delegate {
                FloatMenuUtil.GenerateFloatMenuGroup(FloatMenuUtil.GroupFix);
            }, null));

            list.Add(new ListableOption("BtnTextQB".Translate(), delegate {
                FloatMenuUtil.GenerateFloatMenuGroup(FloatMenuUtil.GroupQuickbar);
            }, null));

            list.Add(new ListableOption("BtnTextSysGC".Translate(), delegate {
                if (Event.current.shift)
                    FloatMenuUtil.GenerateMemoryReclaimOptions();
                CleanserUtil.GCObject.DisposeTmpForSystemGC();
                System.GC.Collect();
                Messages.Message("MsgTextSysGC".Translate(), MessageTypeDefOf.PositiveEvent);
            }, null));

            OptionListingUtility.DrawOptionListing(rect2, list);
            UnityEngine.GUI.EndGroup();
        }

        public void DoHelpContents(Rect rect)
        {
            UnityEngine.GUI.BeginGroup(rect);
            Rect rect2 = new Rect(0f, 0f, 35f, rect.height);
            Text.Font = GameFont.Small;
            List<ListableOption> list = new List<ListableOption>();

            list.Add(new ListableOption("?", delegate {
                //GC_wrapped();
                Find.WindowStack.Add(new Dialog_MessageBox("HelpTextGCWP".Translate()));
            }, null));

            list.Add(new ListableOption("?", delegate {
                //GC_wrapped(true);
                Find.WindowStack.Add(new Dialog_MessageBox("HelpTextGCV".Translate()));
            }, null));
            
            list.Add(new ListableOption("?", delegate {
                //FloatMenuUtil.GenerateFloatMenuGroup(FloatMenuUtil.GroupTools);
                Find.WindowStack.Add(new Dialog_MessageBox("HelpTextAddOn".Translate()));
            }, null));

            list.Add(new ListableOption("?", delegate {
                //FloatMenuUtil.GenerateFloatMenuGroup(FloatMenuUtil.GroupFix);
                Find.WindowStack.Add(new Dialog_MessageBox("HelpTextFix".Translate()));
            }, null));

            list.Add(new ListableOption("?", delegate {
                //FloatMenuUtil.GenerateFloatMenuGroup(FloatMenuUtil.GroupQuickAccess);
                Find.WindowStack.Add(new Dialog_MessageBox("HelpTextQB".Translate()));
            }, null));

            list.Add(new ListableOption("?", delegate {
                //System.GC.Collect();
                Find.WindowStack.Add(new Dialog_MessageBox("HelpTextSysGC".Translate()));
            }, null));

            OptionListingUtility.DrawOptionListing(rect2, list);
            UnityEngine.GUI.EndGroup();
        }

        public void GC_wrapped(bool verbose=false)
        {
            int a = PawnsAliveCount;
            int b = PawnsDeadCount;
            int i = CleanserUtil.GCObject.GC(verbose);
            Notify_PawnsCountDirty();
            int j = a + b - PawnsAliveCount - PawnsDeadCount;

            string str = "DlgTextGC".Translate(a, PawnsAliveCount,
                                               b, PawnsDeadCount,
                                               j);
            Find.WindowStack.Add(new Dialog_MessageBox(str + "\n\n" + (i == j ?
                                                                "DlgTextGCAdvice1".Translate() : "DlgTextGCAdvice2".Translate(i - j))
                                                               + (verbose ? "\n\n" + (string)"DlgTextGCV".Translate() : "")
            ));
            if(RuntimeGC.Settings.ArchiveGCDialog)
                Find.Archive.Add(new ArchivedDialog(str, "DlgArchiveTitle".Translate(), null));
        }

        public void Notify_PawnsCountDirty()
        {
            this.pawnsCountDirty = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            this.forcePause = true;
            this.Notify_PawnsCountDirty();
        }
    }
}
