using CellMenu;
using HarmonyLib;

namespace LocalProgression.Patches
{
    [HarmonyPatch]
    internal class Patches_CM_PageRundown_New
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_PageRundown_New), nameof(CM_PageRundown_New.Setup))]
        private static void Post_Setup(CM_PageRundown_New __instance)
        {
            LocalProgressionManager.Current.SetCurrentRundownPageInstance(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_PageRundown_New), nameof(CM_PageRundown_New.PlaceRundown))]
        private static void Post_PlaceRundown(CM_PageRundown_New __instance)
        {
            LocalProgressionManager.Current.OnNativeRundownProgressionUpdated();
        }
    }
}
