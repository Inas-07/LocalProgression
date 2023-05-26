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
        [HarmonyPatch(typeof(CM_PageRundown_New), nameof(CM_PageRundown_New.UpdateTierIconsWithProgression))]
        private static void Post_UpdateTierIconsWithProgression(CM_PageRundown_New __instance)
        {
            LocalProgressionManager.Current.UpdateRundownPageExpeditionIconProgression();
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(CM_PageRundown_New), nameof(CM_PageRundown_New.OnEnable))]
        //private static void Post_OnEnable(CM_PageRundown_New __instance)
        //{
        //    LocalProgressionManager.Current.UpdateRundownPageExpeditionIconProgression();
        //}
    }
}
