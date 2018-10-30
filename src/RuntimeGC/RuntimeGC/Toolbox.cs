using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace Toolbox
{
    public static class ModMetaDataCleaner
    {
        private static int cacheMetaDataCount = -1;
        public static bool Cleaned => cacheMetaDataCount >= ModLister.AllInstalledMods.Count();

        public static void CleanModMetaData()
        {
            FieldInfo mods = typeof(ModLister).GetField("mods", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
            List<ModMetaData> list = (List<ModMetaData>)mods.GetValue(null);
            int a = 0, b = 0;
            StringBuilder s1 = new StringBuilder(), s2 = new StringBuilder();
            for (int i = list.Count - 1; i > -1; i--)
            {
                if (!list[i].Active)
                {
                    s1.AppendWithComma(list[i].Name);
                    list.RemoveAt(i);
                    a++;
                }
                else
                {
                    s2.AppendWithComma(list[i].Name);
                    list[i].previewImage = new Texture2D(0, 0);
                    b++;
                }
            }
            Verse.Log.Message("[ModMetaDataCleaner] Removed " + a + " Metadata and cleaned " + b + " PreviewImage.\nRemoved: " + s1.ToString() + "\nCleaned: " + s2.ToString());
            if (Current.ProgramState == ProgramState.Playing)
                Messages.Message("MsgModMetaDataCleaned".Translate(a, b), MessageTypeDefOf.PositiveEvent, false);
            mods.SetValue(null, list);
            cacheMetaDataCount = list.Count;
            s1 = s2 = null;
            System.GC.Collect(2);
        }
    }

    public static class LanguageDataCleaner
    {
        private static int cacheLanguageDataCount = -1;
        public static bool Cleaned => cacheLanguageDataCount >= LanguageDatabase.AllLoadedLanguages.Count();

        public static void CleanLanguageData()
        {
            FieldInfo languages = typeof(LanguageDatabase).GetField("languages", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
            List<LoadedLanguage> list = (List<LoadedLanguage>)languages.GetValue(null);
            int a = 0, b = 0;
            StringBuilder s1 = new StringBuilder();
            for (int i = list.Count - 1; i > -1; i--)
                if (list[i] != LanguageDatabase.activeLanguage && list[i] != LanguageDatabase.defaultLanguage)
                {
                    s1.AppendWithComma(list[i].FriendlyNameNative);
                    list.RemoveAt(i);
                    a++;
                }
                else
                {
                    b += list[i].defInjections.Count;
                    list[i].defInjections = new List<DefInjectionPackage>();
                }
            Verse.Log.Message("[LanguageDataCleaner] Removed " + a + " LoadedLanguages and cleaned " + b + " DefInjectionPackages.\nRemoved Languages: " + s1.ToString());
            if (Current.ProgramState == ProgramState.Playing)
                Messages.Message("MsgLanguageDataCleaned".Translate(a, b), MessageTypeDefOf.PositiveEvent, false);
            languages.SetValue(null, list);
            cacheLanguageDataCount = list.Count;
            s1 = null;
            System.GC.Collect(2);
        }
    }

    public static class DefPackageCleaner
    {
        private static ModContentPack coreMod = null;
        public static bool Cleaned => coreMod != null && coreMod.AllDefs.Count() == 0;

        public static void CleanDefPackage()
        {
            FieldInfo defPackages = typeof(ModContentPack).GetField("defPackages", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
            int a = 0;
            foreach (ModContentPack pack in LoadedModManager.RunningMods)
            {
                if (pack.IsCoreMod) coreMod = pack;
                a += ((List<DefPackage>)defPackages.GetValue(pack)).Count;
                defPackages.SetValue(pack, new List<DefPackage>());
            }

            Verse.Log.Message("[DefPackageCleaner] Cleaned " + a + " DefPackages.");
            if (Current.ProgramState == ProgramState.Playing)
                Messages.Message("MsgDefPackageCleaned".Translate(a), MessageTypeDefOf.PositiveEvent, false);
            System.GC.Collect(2);
        }
    }

    public static class Launcher
    {
        public static void Launch(bool modMetaData,bool languageData,bool defPackage)
        {
            if (modMetaData)
                LongEventHandler.QueueLongEvent(ModMetaDataCleaner.CleanModMetaData, "Reclaiming Memory", false, null);
            if (languageData)
                LongEventHandler.QueueLongEvent(LanguageDataCleaner.CleanLanguageData, "Reclaiming Memory", false, null);
            if (defPackage)
                LongEventHandler.QueueLongEvent(DefPackageCleaner.CleanDefPackage, "Reclaiming Memory", false, null);
        }
    }
}
