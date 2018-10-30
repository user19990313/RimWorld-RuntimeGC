using System;
using Verse;

namespace RuntimeGC
{
    public class RuntimeGCSettings:ModSettings
    {
        private bool EnableMemoryUsageBar;
        private int MemoryUsageBarLowerBoundMb;
        private int MemoryUsageBarUpperBoundMb;
        private int MemoryUsageUpdateInterval;
        private bool EnableMemoryUsageTip;

        public bool AutoCleanModMetaData;
        public bool AutoCleanLanguageData;
        public bool AutoCleanDefPackage;

        public bool DoMuteGC;
        public bool DoMuteBL;

        public bool ArchiveGCDialog;
        public bool ArchiveMessageGeneral;

        public bool DevOnScreenMemoryUsage;

        public bool EnableMemoryMonitorBar
        {
            get
            {
                return this.EnableMemoryUsageBar;
            }
            set
            {
                if (this.EnableMemoryUsageBar != value)
                {
                    this.EnableMemoryUsageBar = value;
                    this.UpdateCache();
                }
            }
        }
        public int MemoryMonitorBarLowerBoundMb
        {
            get
            {
                return this.MemoryUsageBarLowerBoundMb;
            }
            set
            {
                if (this.MemoryUsageBarLowerBoundMb != value)
                {
                    this.MemoryUsageBarLowerBoundMb = value;
                    this.UpdateCache();
                }
            }
        }
        public int MemoryMonitorBarUpperBoundMb
        {
            get
            {
                return this.MemoryUsageBarUpperBoundMb;
            }
            set
            {
                if (this.MemoryUsageBarUpperBoundMb != value)
                {
                    this.MemoryUsageBarUpperBoundMb = value;
                    this.UpdateCache();
                }
            }
        }
        public int MemoryMonitorUpdateInterval
        {
            get
            {
                return this.MemoryUsageUpdateInterval;
            }
            set
            {
                if (this.MemoryUsageUpdateInterval != value)
                {
                    this.MemoryUsageUpdateInterval = value;
                    MainButtonWorker_RuntimeGC.Notify_UpdateIntervalChanged(value);
                }
            }
        }
        public bool EnableMemoryMonitorTip
        {
            get
            {
                return this.EnableMemoryUsageTip;
            }
            set
            {
                if (this.EnableMemoryUsageTip != value)
                {
                    this.EnableMemoryUsageTip = value;
                    this.UpdateCache();
                }
            }
        }

        internal int restartFlags = 0;

        public Mod Mod
        {
            get
            {
                Verse.Log.Warning("RuntimeGCSettings.get_Mod() is called!");
                return LoadedModManager.GetMod<RuntimeGC>();
            }
            set { }
        }
        public RuntimeGCSettings()
        {
            this.Init();
            this.UpdateCache();
        }

        public void Init()
        {
            this.InitMemoryMonitor();

            this.AutoCleanModMetaData = true;
            this.AutoCleanLanguageData = true;
            this.AutoCleanDefPackage = false;

            this.DoMuteGC = true;
            this.DoMuteBL = false;

            this.ArchiveGCDialog = true;
            this.ArchiveMessageGeneral = false;

            this.DevOnScreenMemoryUsage = false;
        }

        public void InitMemoryMonitor()
        {
            this.EnableMemoryUsageBar = true;
            this.MemoryUsageBarLowerBoundMb = 0;
            this.MemoryUsageBarUpperBoundMb = 1024 * (IntPtr.Size == 4 ? 1 : 2);
            this.MemoryUsageUpdateInterval = (int)MemoryMonitorUpdateMode.Moderate;
            this.EnableMemoryUsageTip = true;
        }

        public void ResetToDefault()
        {
            if (this.AutoCleanModMetaData != true)
                this.restartFlags ^= 1 << 0;
            if (this.AutoCleanLanguageData != true)
                this.restartFlags ^= 1 << 1;
            if (this.AutoCleanDefPackage != false)
                this.restartFlags ^= 1 << 2;

            if (this.DoMuteGC != true)
                this.restartFlags ^= 1 << 3;
            if (this.DoMuteBL != false)
                this.restartFlags ^= 1 << 4;

            this.Init();
            this.UpdateCache();
            UIUtil.Notify_MMBtnLabelChanged();
        }

        public void UpdateCache()
        {
            MainButtonWorker_RuntimeGC.UpdateSettings(this);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref EnableMemoryUsageBar, "EnableMemoryUsageBar", true);
            Scribe_Values.Look<int>(ref MemoryUsageBarLowerBoundMb, "MemoryUsageBarLowerBoundMb", 0);
            Scribe_Values.Look<int>(ref MemoryUsageBarUpperBoundMb, "MemoryUsageBarUpperBoundMb", 1024 * (IntPtr.Size == 4 ? 1 : 2));
            Scribe_Values.Look<int>(ref MemoryUsageUpdateInterval, "MemoryUsageUpdateInterval", (int)MemoryMonitorUpdateMode.Moderate);
            Scribe_Values.Look<bool>(ref EnableMemoryUsageTip, "EnableMemoryUsageTip", true);

            Scribe_Values.Look<bool>(ref AutoCleanModMetaData, "AutoCleanModMetaData", true);
            Scribe_Values.Look<bool>(ref AutoCleanLanguageData, "AutoCleanLanguageData", true);
            Scribe_Values.Look<bool>(ref AutoCleanDefPackage, "AutoCleanDefPackage", false);

            Scribe_Values.Look<bool>(ref DoMuteGC, "DoMuteGC", true);
            Scribe_Values.Look<bool>(ref DoMuteBL, "DoMuteBL", false);

            Scribe_Values.Look<bool>(ref ArchiveGCDialog, "ArchiveGCDialog", true);
            Scribe_Values.Look<bool>(ref ArchiveMessageGeneral, "ArchiveMessageGeneral", false);

            Scribe_Values.Look<bool>(ref DevOnScreenMemoryUsage, "DevOnScreenMemoryUsage", false);
            
            if(Scribe.mode== LoadSaveMode.LoadingVars)
            {
                if (MemoryUsageBarLowerBoundMb < 0)
                    MemoryUsageBarLowerBoundMb = 0;
                if (MemoryUsageBarUpperBoundMb > 1024 * (IntPtr.Size == 4 ? 4 : 128))
                    MemoryUsageBarUpperBoundMb = 1024 * (IntPtr.Size == 4 ? 4 : 128);
                if (MemoryUsageBarUpperBoundMb <= MemoryUsageBarLowerBoundMb)
                {
                    MemoryUsageBarLowerBoundMb = 0;
                    MemoryUsageBarUpperBoundMb = 1024 * (IntPtr.Size == 4 ? 1 : 2);
                }
                this.UpdateCache();
            }
        }

        public bool RequiresRestart()
        {
            return this.restartFlags != 0;
        }
    }
}
