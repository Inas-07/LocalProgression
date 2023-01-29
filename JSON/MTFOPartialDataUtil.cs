﻿using BepInEx.Unity.IL2CPP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace ScanPosOverride.JSON
{
    public static class MTFOPartialDataUtil
    {
        public const string PLUGIN_GUID = "MTFO.Extension.PartialBlocks";

        public static JsonConverter PersistentIDConverter { get; private set; } = null;
        public static bool IsLoaded { get; private set; } = false;
        public static bool Initialized { get; private set; } = false;
        public static string PartialDataPath { get; private set; } = string.Empty;
        public static string ConfigPath { get; private set; } = string.Empty;

        static MTFOPartialDataUtil()
        {
            if (IL2CPPChainloader.Instance.Plugins.TryGetValue(PLUGIN_GUID, out var info))
            {
                try
                {
                    var ddAsm = info?.Instance?.GetType()?.Assembly ?? null;

                    if (ddAsm is null)
                        throw new Exception("Assembly is Missing!");

                    var types = ddAsm.GetTypes();
                    var converterType = types.First(t => t.Name == "PersistentIDConverter");
                    if (converterType is null)
                        throw new Exception("Unable to Find PersistentIDConverter Class");

                    var dataManager = types.First(t => t.Name == "PartialDataManager");
                    if (dataManager is null)
                        throw new Exception("Unable to Find PartialDataManager Class");

                    var initProp = dataManager.GetProperty("Initialized", BindingFlags.Public | BindingFlags.Static);
                    var dataPathProp = dataManager.GetProperty("PartialDataPath", BindingFlags.Public | BindingFlags.Static);
                    var configPathProp = dataManager.GetProperty("ConfigPath", BindingFlags.Public | BindingFlags.Static);

                    if (initProp is null)
                        throw new Exception("Unable to Find Property: Initialized");

                    if (dataPathProp is null)
                        throw new Exception("Unable to Find Property: PartialDataPath");

                    if (configPathProp is null)
                        throw new Exception("Unable to Find Field: ConfigPath");

                    Initialized = (bool)initProp.GetValue(null);
                    PartialDataPath = (string)dataPathProp.GetValue(null);
                    ConfigPath = (string)configPathProp.GetValue(null);

                    PersistentIDConverter = (JsonConverter)Activator.CreateInstance(converterType);
                    IsLoaded = true;
                }
                catch (Exception e)
                {
                    Logger.Error($"Exception thrown while reading data from MTFO_Extension_PartialData:\n{e}");
                }
            }
        }
    }
}
