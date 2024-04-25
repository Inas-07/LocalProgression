using CellMenu;
using HarmonyLib;
using LocalProgression.Component;

namespace LocalProgression.Patches
{
    [HarmonyPatch]
    internal static class Patch_CM_PageExpeditionSuccess
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_PageExpeditionSuccess), nameof(CM_PageExpeditionSuccess.Setup))]
        private static void Post_Setup(CM_PageExpeditionSuccess __instance)
        {
            if (__instance.GetComponent<ExpeditionSuccessPage_NoBoosterIcon>() == null)
            {
                var p = __instance.gameObject.AddComponent<ExpeditionSuccessPage_NoBoosterIcon>();
                p.m_page = __instance;
                p.Setup();
            }
        }
    }
}
