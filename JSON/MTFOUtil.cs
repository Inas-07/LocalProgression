using BepInEx.Unity.IL2CPP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ScanPosOverride.JSON
{
    public static class MTFOUtil
    {
        public const string PLUGIN_GUID = "com.dak.MTFO";
        public const BindingFlags PUBLIC_STATIC = BindingFlags.Public | BindingFlags.Static;
        public static string GameDataPath { get; private set; } = string.Empty;
        public static string CustomPath { get; private set; } = string.Empty;
        public static bool HasCustomContent { get; private set; } = false;
        public static bool IsLoaded { get; private set; } = false;

        static MTFOUtil()
        {
            if (!IL2CPPChainloader.Instance.Plugins.TryGetValue(PLUGIN_GUID, out var info))
                return;

            try
            {
                var ddAsm = info?.Instance?.GetType()?.Assembly ?? null;

                if (ddAsm is null)
                    throw new Exception("Assembly is Missing!");

                var types = ddAsm.GetTypes();
                var cfgManagerType = types.First(t => t.Name == "ConfigManager");

                if (cfgManagerType is null)
                    throw new Exception("Unable to Find ConfigManager Class");

                var dataPathField = cfgManagerType.GetField("GameDataPath", PUBLIC_STATIC);
                var customPathField = cfgManagerType.GetField("CustomPath", PUBLIC_STATIC);
                var hasCustomField = cfgManagerType.GetField("HasCustomContent", PUBLIC_STATIC);

                if (dataPathField is null)
                    throw new Exception("Unable to Find Field: GameDataPath");

                if (customPathField is null)
                    throw new Exception("Unable to Find Field: CustomPath");

                if (hasCustomField is null)
                    throw new Exception("Unable to Find Field: HasCustomContent");

                GameDataPath = (string)dataPathField.GetValue(null);
                CustomPath = (string)customPathField.GetValue(null);
                HasCustomContent = (bool)hasCustomField.GetValue(null);
                IsLoaded = true;
            }
            catch (Exception e)
            {
                Logger.Error($"Exception thrown while reading path from DataDumper (MTFO): \n{e}");
            }
        }
    }
}
