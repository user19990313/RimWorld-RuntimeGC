using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using Toolbox;

namespace RuntimeGC
{
    public static class FloatMenuUtil
    {
        private static Dictionary<string, List<string>> groups;
        private static Dictionary<string, Action> items;
        private static Dictionary<string, bool> devOnly;

        public static readonly string GroupTools = "tools";
        public static readonly string GroupFix = "fix";
        public static readonly string GroupQuickbar = "quickbar";

        public static readonly string GroupMMUpdateMode = "mmupdate";

        static FloatMenuUtil()
        {
            groups = new Dictionary<string, List<string>>();
            items = new Dictionary<string, Action>();
            devOnly = new Dictionary<string, bool>();

            string group;

            group = GroupFix;
            Add(group, "FloatDebugLog".Translate(), delegate {
                if (!Find.WindowStack.TryRemove(typeof(EditWindow_Log), true))
                {
                    Find.WindowStack.Add(new EditWindow_Log());
                }
            });
            Add(group, "FloatAGRegen".Translate(), delegate {
                foreach (Map map in Find.Maps)
                {
                    map.avoidGrid.Regenerate();
                }
                Message("MsgTextAGR".Translate(), MessageTypeDefOf.PositiveEvent);
            });
            Add(group, "FloatFactionFixItems1".Translate(), delegate {
                CleanserUtil.FixFactionRelationships();
                Message("MsgTextFFR".Translate(), MessageTypeDefOf.PositiveEvent);
                });
            Add(group, "FloatFactionFixItems2".Translate(), delegate {
                Message("MsgTextFFL".Translate(CleanserUtil.FixFactionLeader_Wrapped()), MessageTypeDefOf.PositiveEvent);
                });

            group = GroupTools;
            Add(group, "FloatToolsItems1".Translate(), delegate {
                int a = CleanserUtil.DeconstructAnimalFamily();
                Verse.Log.Message("CleanserUtil.DeconstructAnimalFamily():Round 1 completed.");
                CleanserUtil.DeconstructAnimalFamily();
                Verse.Log.Message("CleanserUtil.DeconstructAnimalFamily():Round 2 completed.");
                Message("MsgTextAFT".Translate(a), MessageTypeDefOf.PositiveEvent);
                });

            Add(group, "FloatToolsItems2".Translate(), delegate {
                Message("MsgTextRFH".Translate(CleanserUtil.RemoveFilth(Find.CurrentMap, true)), MessageTypeDefOf.PositiveEvent);
                });
            Add(group, "FloatToolsItems2Dev1".Translate(), delegate {
                Message("MsgTextRFM".Translate(CleanserUtil.RemoveFilth(Find.CurrentMap, false)), MessageTypeDefOf.PositiveEvent);
            }, true);
            Add(group, "FloatToolsItems2Dev2".Translate(), delegate {
                int i = 0;
                foreach (Map m in Find.Maps)
                    i += CleanserUtil.RemoveFilth(m, false);
                Message("MsgTextRFW".Translate(i), MessageTypeDefOf.PositiveEvent);
            }, true);

            Add(group, "FloatToolsItems2Snow".Translate(), delegate {
                CleanserUtil.RemoveSnow(Find.CurrentMap, true);
                Message("MsgTextRSH".Translate(), MessageTypeDefOf.PositiveEvent);
            });
            Add(group, "FloatToolsItems2SnowDev1".Translate(), delegate {
                CleanserUtil.RemoveSnow(Find.CurrentMap, false);
                Message("MsgTextRSM".Translate(), MessageTypeDefOf.PositiveEvent);
            }, true);
            Add(group, "FloatToolsItems2SnowDev2".Translate(), delegate {
                foreach (Map m in Find.Maps)
                    CleanserUtil.RemoveSnow(m, false);
                Message("MsgTextRSW".Translate(), MessageTypeDefOf.PositiveEvent);
            }, true);

            Add(group, "FloatToolsItems3".Translate(), delegate {
                Message("MsgTextRCM".Translate(CleanserUtil.RemoveCorpses()), MessageTypeDefOf.NeutralEvent);
                });
            Add(group, "FloatToolsItems4".Translate(), delegate {
                Message("MsgTextRBL".Translate(CleanserUtil.RemoveAllBattleLogEntries()), MessageTypeDefOf.PositiveEvent);
                });
            
            Add(group, "FloatToolsItems5".Translate(), delegate {
                Message("MsgTextRAM".Translate(CleanserUtil.RemoveIArchivable(false)), MessageTypeDefOf.PositiveEvent);
            });
            Add(group, "FloatToolsItems5Dev".Translate(), delegate {
                Message("MsgTextRAM".Translate(CleanserUtil.RemoveIArchivable(true)), MessageTypeDefOf.PositiveEvent);
            },true);


            

            group = GroupQuickbar;
            Add(group, "QuickCloseLetStack".Translate(), delegate {
                LetterStack ls = Find.LetterStack;
                if (ls == null) return;
                for (int i = ls.LettersListForReading.Count - 1; i > -1; i--)
                    ls.RemoveLetter(ls.LettersListForReading[i]);
            });
            Add(group, "QuickUnlockSpeedLimit".Translate(), delegate {
                CleanserUtil.UnlockNormalSpeedLimit();
                Find.WindowStack.TryRemove(typeof(UserInterface));
            });
            Add(group, "QuickOpenSettings".Translate(), delegate {
                CleanserUtil.OpenModSettingsPage();
            });


            group = GroupMMUpdateMode;
            foreach (MemoryMonitorUpdateMode m in Enum.GetValues(typeof(MemoryMonitorUpdateMode)))
                Add(group, ("MMUpdate_" + m.ToString()).Translate(), delegate
                {
                    RuntimeGC.Settings.MemoryMonitorUpdateInterval = (int)m;
                    UIUtil.Notify_MMBtnLabelChanged();
                }, m.ToString().Contains("Debug_"));
        }

        public static void Add(string group,string label,Action action,bool devonly = false)
        {
            if (!groups.ContainsKey(group))
                groups.Add(group, new List<string>());
            groups[group].Add(label);
            items.Add(label, action);
            devOnly.Add(label, devonly);
        }

        public static void GenerateFloatMenuGroup(string group)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach(string label in groups[group])
            {
                if (devOnly[label] && (!Prefs.DevMode)) continue;
                list.Add(new FloatMenuOption(label, items[label]));
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        public static void GenerateMemoryReclaimOptions()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            FloatMenuOption option;
            option = new FloatMenuOption("FloatACModMetaData".Translate(), ModMetaDataCleaner.CleanModMetaData);
            if (ModMetaDataCleaner.Cleaned)
            {
                option.Label = "FloatACModMetaDataCleared".Translate();
                option.Disabled = true;
            }
            list.Add(option);
            option = new FloatMenuOption("FloatACLanguageData".Translate(), LanguageDataCleaner.CleanLanguageData);
            if (LanguageDataCleaner.Cleaned)
            {
                option.Label = "FloatACLanguageDataCleared".Translate();
                option.Disabled = true;
            }
            list.Add(option);
            option = new FloatMenuOption("FloatACDefPackage".Translate(), DefPackageCleaner.CleanDefPackage);
            if (DefPackageCleaner.Cleaned)
            {
                option.Label = "FloatACDefPackageCleared".Translate();
                option.Disabled = true;
            }
            list.Add(option);
            Find.WindowStack.Add(new FloatMenu(list));
        }

        public static void Message(string str,MessageTypeDef type)
        {
            Messages.Message(str,type,RuntimeGC.Settings.ArchiveMessageGeneral);
        }
    }
}
