using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using GTFO.API;

namespace LocalProgression
{
    [BepInDependency("dev.gtfomodding.gtfo-api")]
    [BepInDependency("com.dak.MTFO", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("Inas.LocalProgression", "LocalProgression", "1.1.7")]
    
    public class EntryPoint: BasePlugin
    {
        private Harmony m_Harmony;

        public override void Load()
        {
            m_Harmony = new Harmony("LocalProgression");
            m_Harmony.PatchAll();

            EventAPI.OnManagersSetup += LocalProgressionManager.Current.Init;
        }
    }
}

