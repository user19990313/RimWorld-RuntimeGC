using Verse;
using UnityEngine;

namespace RuntimeGC
{
    [StaticConstructorOnStartup]
    class StaticConstructor
    {
        static StaticConstructor()
        {
            /*RimWorld.MainButtonDef def = new RimWorld.MainButtonDef();
            def.defName = "RGC_UI";
            def.label = "RuntimeGC";
            def.description = "MainTabWindowDescription".Translate();
            def.tabWindowClass = typeof(RuntimeGC.UserInterface);
            def.order = 177;
            Verse.DefDatabase<RimWorld.MainButtonDef>.Add(def);

            Verse.Log.Message("["+StaticConstructor.AssemblyName + "] MainTabWindow inserted.");*/

            if ((UnityEngine.Object)GameObject.Find("RuntimeGCInstance") != (UnityEngine.Object)null)
            {
                Verse.Log.Warning("[RuntimeGC] More than one RuntimeGC instance is running!");
            }
            else
            {
                GameObject gameObject = new GameObject("RuntimeGCInstance");
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
            }

            MainButtonWorker_RuntimeGC.TabDescriptionTranslated = "MainTabWindowDescription".Translate();
            MainButtonWorker_RuntimeGC.MMTipTranslated = "MMTip".Translate();

            UIUtil.Notify_MMBtnLabelChanged();
        }
    }
}
