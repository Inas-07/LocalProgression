using CellMenu;
using HarmonyLib;
using LocalProgression.Component;

namespace LocalProgression.Patches
{
    [HarmonyPatch]
    internal class Patches_CM_RundownTierMarker
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_RundownTierMarker), nameof(CM_RundownTierMarker.Setup))]
        private static void Post_Setup(CM_RundownTierMarker __instance)
        {
            var p = __instance.gameObject.AddComponent<RundownTierMarker_NoBoosterIcon>();
            p.m_tierMarker = __instance;
            p.Setup();
        }
    }
}
