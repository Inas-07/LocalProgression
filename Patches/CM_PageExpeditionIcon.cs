using CellMenu;
using HarmonyLib;
using UnityEngine;

namespace LocalProgression.Patches
{
    [HarmonyPatch]
    internal static class Patches_CM_ExpeditionIcon
    {
        private static readonly Color BORDER_COLOR = new Color(0f, 1f, 246f / 255f, 1f);
        private static readonly Color TEXT_COLOR = new Color(0f, 1f, 150f / 255f, 1f);


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_ExpeditionIcon_New), nameof(CM_ExpeditionIcon_New.UpdateBorderColor))]
        private static void Post_UpdateBorderColor(CM_ExpeditionIcon_New __instance)
        {
            if (__instance.Status == eExpeditionIconStatus.LockedAndScrambled) return;

            var rundownID = LocalProgressionManager.Current.ActiveRundownID();
            if (LocalProgressionManager.Current.TryGetRundownConfig(rundownID, out var rundownDef) 
                && rundownDef.EnableNoBoosterUsedProgressionForRundown 
                || 
                LocalProgressionManager.Current.TryGetExpeditionConfig(rundownID, __instance.Tier, __instance.ExpIndex, out var expDef) 
                && expDef.EnableNoBoosterUsedProgression)
            {
                var lpData = LocalProgressionManager.Current.GetExpeditionLP(rundownID, __instance.Tier, __instance.ExpIndex);
                if (lpData.NoBoosterAllClearCount > 0)
                {
                    __instance.SetBorderColor(BORDER_COLOR);
                }
            }
        }
    }
}
