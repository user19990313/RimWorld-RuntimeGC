using System;
using Verse;
using UnityEngine;

namespace RuntimeGC
{
    public class RuntimeGC:Mod
    {
        public static RuntimeGCSettings Settings;

        public RuntimeGC(ModContentPack pack) : base(pack)
        {
            Settings = LoadedModManager.ReadModSettings<RuntimeGCSettings>(Content.PackageId, this.GetType().Name);
            Toolbox.Launcher.Launch(Settings.AutoCleanModMetaData, Settings.AutoCleanLanguageData, Settings.AutoCleanDefPackage);
            Mute.Launcher.Launch(Settings.DoMuteGC, Settings.DoMuteBL);
            Verse.Log.Message("[RuntimeGC] Mod settings loaded.");
        }

        public override void WriteSettings()
        {
            Settings.UpdateCache();
            LoadedModManager.WriteModSettings(Content.PackageId, "RuntimeGC", Settings);
            if (Settings.RequiresRestart())
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("DlgTextRestartRequest".Translate(), delegate
                                                                        {
                                                                            GenCommandLine.Restart();
                                                                        }, false, "DlgTitleRestart".Translate()));
            }
        }

        public override string SettingsCategory()
        {
            return "RuntimeGC";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);

            float ymax = UIUtil.DrawSectionLabel(inRect.x, inRect.y, "SettingsMMCategory".Translate(), inRect.xMax);
            
            string text= "SettingsMMTipLabel".Translate();
            float width = inRect.width/2 - UIUtil.MarginLarge - UIUtil.MarginHorizontal * 4;
            float height = Text.CalcHeight(text, width);

            #region MemMon

            Rect rectMemMon = new Rect(inRect.x + UIUtil.MarginLarge + UIUtil.MarginHorizontal, ymax, inRect.width - UIUtil.MarginLarge*2 - UIUtil.MarginHorizontal*2, height * 4 + UIUtil.MarginVertical * 3 + (Prefs.DevMode ? height + UIUtil.MarginVertical : 0));
            GUI.BeginGroup(rectMemMon);

            #region MMLeft
            Rect rectCheck = new Rect(0f, 0f, width + UIUtil.MarginHorizontal, height);
            bool flag = Settings.EnableMemoryMonitorTip;
            Widgets.CheckboxLabeled(rectCheck, text, ref flag);
            Settings.EnableMemoryMonitorTip = flag;
            TooltipHandler.TipRegion(rectCheck, "SettingsMMTipTip".Translate());
            rectCheck = new Rect(rectCheck.x, rectCheck.yMax + UIUtil.MarginVertical, width+UIUtil.MarginHorizontal, height);
            flag = Settings.EnableMemoryMonitorBar;
            Widgets.CheckboxLabeled(rectCheck, "SettingsMMBarLabel".Translate(), ref flag);
            Settings.EnableMemoryMonitorBar = flag;
            TooltipHandler.TipRegion(rectCheck, "SettingsMMBarTip".Translate());

            if (Settings.EnableMemoryMonitorBar)
            {
                Rect rectRangeLabel = new Rect(UIUtil.MarginHorizontal, rectCheck.yMax + UIUtil.MarginVertical, width, height);
                Widgets.Label(rectRangeLabel, "SettingsMMRangeLabel".Translate());
                TooltipHandler.TipRegion(rectRangeLabel, "SettingsMMRangeTip".Translate());

                float w = Text.CalcSize("x32").x;
                Rect rectx32 = new Rect(rectRangeLabel.xMax - w * 2 - 5f - UIUtil.MarginHorizontal, rectRangeLabel.y, w, height);
                if (Mouse.IsOver(rectx32))
                    GUI.color = Color.cyan;
                else GUI.color = Color.gray;
                Widgets.Label(rectx32,"x32");
                Widgets.DrawLineHorizontal(rectx32.x, rectx32.yMax - 1f, rectx32.width);
                GUI.color = Color.white;
                if (Widgets.ButtonInvisible(rectx32, true))
                {
                    Settings.MemoryMonitorBarLowerBoundMb = 0;
                    Settings.MemoryMonitorBarUpperBoundMb = 1024;
                }

                Rect rectx64 = new Rect(rectRangeLabel.xMax - w - UIUtil.MarginHorizontal, rectRangeLabel.y, w, height);
                if (Mouse.IsOver(rectx64) && IntPtr.Size == 8)
                    GUI.color = Color.cyan;
                else GUI.color = Color.gray;
                Widgets.Label(rectx64, "x64");
                if (IntPtr.Size == 8)
                {
                    Widgets.DrawLineHorizontal(rectx64.x, rectx64.yMax - 1f, rectx64.width);
                    
                    if (Widgets.ButtonInvisible(rectx64, true))
                    {
                        Settings.MemoryMonitorBarLowerBoundMb = 0;
                        Settings.MemoryMonitorBarUpperBoundMb = 2048;
                    }
                }
                else
                {
                    Widgets.DrawLine(new Vector2(rectx64.x, rectx64.y), new Vector2(rectx64.xMax, rectx64.yMax), Color.gray, 1f);
                }
                GUI.color = Color.white;
                
                Rect rectDualSlider = new Rect(rectRangeLabel.x, rectRangeLabel.yMax + UIUtil.MarginVertical, width, height);
                IntRange range = new IntRange(Settings.MemoryMonitorBarLowerBoundMb, Settings.MemoryMonitorBarUpperBoundMb);
                Widgets.IntRange(rectDualSlider, 233, ref range, 0, 1024 * 8);
                Settings.MemoryMonitorBarLowerBoundMb = range.min;
                Settings.MemoryMonitorBarUpperBoundMb = range.max;
            }
            #endregion

            Rect rectMainButton = new Rect(rectMemMon.width / 2 + (rectMemMon.width / 2 - 130f) / 2, (height * 2 + UIUtil.MarginVertical - 35f) / 2, 130f, 35f);
            UIUtil.MainButtonWorker.DoButton(rectMainButton);

            Rect rectIntervalLabel = new Rect(rectMemMon.width / 2 + UIUtil.MarginHorizontal*2, height * 2 + UIUtil.MarginVertical * 2+(35f-height)/2, rectMemMon.width / 2 -UIUtil.MarginHorizontal- 125f, height);
            Widgets.Label(rectIntervalLabel, "SettingsMMIntervalLabel".Translate());
            TooltipHandler.TipRegion(rectIntervalLabel, "SettingsMMIntervalTip".Translate());

            Rect rectIntervalButton = new Rect(rectMemMon.xMax-125f-UIUtil.MarginHorizontal*2, height * 2 + UIUtil.MarginVertical * 2, 125f, 35f);
            
            if (Widgets.ButtonText(rectIntervalButton, UIUtil.MMIntervalButtonLabelCache))
            {
                FloatMenuUtil.GenerateFloatMenuGroup(FloatMenuUtil.GroupMMUpdateMode);
            }

            if (Prefs.DevMode)
            {
                Rect rectDevOnScreenMem = new Rect(rectMemMon.width / 2 + UIUtil.MarginHorizontal*2, rectIntervalButton.yMax + UIUtil.MarginVertical, rectMemMon.width / 2 - UIUtil.MarginHorizontal*3, height);
                flag = RuntimeGC.Settings.DevOnScreenMemoryUsage;
                Widgets.CheckboxLabeled(rectDevOnScreenMem, "SettingsDevOnScreenMemoryUsageLabel".Translate(), ref flag);
                if(flag!= RuntimeGC.Settings.DevOnScreenMemoryUsage)
                {
                    RuntimeGC.Settings.DevOnScreenMemoryUsage = flag;
                    RuntimeGC.Settings.UpdateCache();
                }
            }
            
            GUI.EndGroup();

            #endregion

            UIUtil.BeginRestartCheck();

            ymax = UIUtil.DrawSectionLabel(inRect.x,rectMemMon.yMax+height, "SettingsAutoCleanupCategory".Translate(),inRect.width/2-UIUtil.MarginHorizontal);
            
            Rect rectAutoCleanup = new Rect(rectMemMon.x, ymax, width, height * 3 + UIUtil.MarginVertical * 2);
            GUI.BeginGroup(rectAutoCleanup);
            rectCheck = new Rect(0, 0, width, height);
            UIUtil.DrawCheckboxRestartIfApplied(rectCheck, "SettingsACModMetaDataLabel".Translate(), "SettingsACModMetaDataTip".Translate(), ref Settings.AutoCleanModMetaData);
            rectCheck = new Rect(0, height + UIUtil.MarginVertical, width, height);
            UIUtil.DrawCheckboxRestartIfApplied(rectCheck, "SettingsACLanguageDataLabel".Translate(), "SettingsACLanguageDataTip".Translate(),ref Settings.AutoCleanLanguageData);
            rectCheck = new Rect(0, height*2 + UIUtil.MarginVertical*2, width, height);
            UIUtil.DrawCheckboxRestartIfApplied(rectCheck, "SettingsACDefPackageLabel".Translate(), "SettingsACDefPackageTip".Translate(), ref Settings.AutoCleanDefPackage);
            GUI.EndGroup();

            ymax = UIUtil.DrawSectionLabel(rectMemMon.x+rectMemMon.width/2+UIUtil.MarginHorizontal, rectMemMon.yMax + height, "SettingsMuteCategory".Translate(), inRect.xMax);

            Rect rectMute = new Rect(rectMemMon.x + rectMemMon.width / 2+UIUtil.MarginHorizontal*2, ymax, width, height * 2 + UIUtil.MarginVertical);
            GUI.BeginGroup(rectMute);
            rectCheck = new Rect(0, 0, width, height);
            UIUtil.DrawCheckboxRestartIfApplied(rectCheck, "SettingsMuteGCLabel".Translate(), "SettingsMuteGCTip".Translate(), ref Settings.DoMuteGC);
            rectCheck = new Rect(0, height + UIUtil.MarginVertical, width, height);
            UIUtil.DrawCheckboxRestartIfApplied(rectCheck, "SettingsMuteBLLabel".Translate(), "SettingsMuteBLTip".Translate(), ref Settings.DoMuteBL);
            GUI.EndGroup();


            ymax = UIUtil.DrawSectionLabel(inRect.x, rectAutoCleanup.yMax + height+ (Prefs.DevMode ? height + UIUtil.MarginVertical : 0), "SettingsGeneralCategory".Translate(),  inRect.width / 2 - UIUtil.MarginHorizontal);

            Rect rectGeneral = new Rect(rectMemMon.x, ymax, width, height * 3 + UIUtil.MarginVertical * 2);
            GUI.BeginGroup(rectGeneral);
            rectCheck = new Rect(0, 0, width, height);
            Widgets.CheckboxLabeled(rectCheck, "SettingsArchiveGCLabel".Translate(), ref Settings.ArchiveGCDialog);
            TooltipHandler.TipRegion(rectCheck, "SettingsArchiveGCTip".Translate());
            rectCheck = new Rect(0, height + UIUtil.MarginVertical, width, height);
            Widgets.CheckboxLabeled(rectCheck, "SettingsArchiveGeneralLabel".Translate(), ref Settings.ArchiveMessageGeneral);
            TooltipHandler.TipRegion(rectCheck, "SettingsArchiveGeneralTip".Translate());
            GUI.EndGroup();


            Rect rectReset = new Rect(inRect.xMax - UIUtil.MarginHorizontal - 135f-50f, inRect.yMax - 35f-65f, 135f, 35f);
            if(Widgets.ButtonText(rectReset,"SettingsReset".Translate()))
            {
                Settings.ResetToDefault();
            }


            GUI.EndGroup();
        }
    }
}
