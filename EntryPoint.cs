using BepInEx;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using HarmonyLib;

namespace LocalProgression
{
    [BepInPlugin("Inas.LocalProgression", "LocalProgression", "1.4.0")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.dak.MTFO", BepInDependency.DependencyFlags.HardDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        private Harmony m_Harmony = null!;

        public override void Load()
        {
            LPLogger.Logger = Log;
            m_Harmony = new Harmony("LocalProgression");
            m_Harmony.PatchAll();

            EventAPI.OnManagersSetup += LocalProgressionManager.Current.Init;
            AssetAPI.OnAssetBundlesLoaded += Assets.Init;
            LPLogger.Log("LocalProgression is done loading!");
        }
    }
}
