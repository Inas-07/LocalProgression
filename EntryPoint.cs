using BepInEx;
//using BepInEx.IL2CPP;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
namespace LocalProgression
{
    [BepInDependency("dev.gtfomodding.gtfo-api")]
    [BepInDependency("com.dak.MTFO")]
    [BepInPlugin("Inas.LocalProgression", "LocalProgression", "1.0.0")]
    
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

