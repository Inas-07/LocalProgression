using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
namespace LocalProgression
{
    [BepInDependency("dev.gtfomodding.gtfo-api")]
    [BepInDependency("com.dak.MTFO")]
    [BepInPlugin("Inas.LocalProgression", "LocalProgression", "0.0.1")]
    
    public class EntryPoint: BasePlugin
    {
        private Harmony m_Harmony;

        public override void Load()
        {
            m_Harmony = new Harmony("LocalProgression");
            m_Harmony.PatchAll();
        }
    }
}

