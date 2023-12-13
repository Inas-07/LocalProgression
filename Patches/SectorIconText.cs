using HarmonyLib;

namespace LocalProgression.Patches
{
    [HarmonyPatch]
    internal class Patches_SectorIconText
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CM_RundownTierMarker), nameof(CM_RundownTierMarker.SetSectorIconTextForMain))]
        private static void Pre_SetSectorIconTextForMain(CM_RundownTierMarker __instance, ref string text)
        {
            var data = LocalProgressionManager.Current.nativeLocalProgData;
            text = $"<color=orange>[{data.clearedMain}/{data.totalMain}]</color>";
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CM_RundownTierMarker), nameof(CM_RundownTierMarker.SetSectorIconTextForSecondary))]
        private static void Pre_SetSectorIconTextForSecondary(CM_RundownTierMarker __instance, ref string text)
        {
            var data = LocalProgressionManager.Current.nativeLocalProgData;
            text = $"<color=orange>[{data.clearedSecondary}/{data.totalSecondary}]</color>";
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CM_RundownTierMarker), nameof(CM_RundownTierMarker.SetSectorIconTextForThird))]
        private static void Pre_SetSectorIconTextForThird(CM_RundownTierMarker __instance, ref string text)
        {
            var data = LocalProgressionManager.Current.nativeLocalProgData;
            text = $"<color=orange>[{data.clearedThird}/{data.totalThird}]</color>";
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CM_RundownTierMarker), nameof(CM_RundownTierMarker.SetSectorIconTextForAllCleared))]
        private static void Pre_SetSectorIconTextForAllCleared(CM_RundownTierMarker __instance, ref string text)
        {
            var data = LocalProgressionManager.Current.nativeLocalProgData;
            text = $"<color=orange>[{data.clearedAllClear}/{data.totalAllClear}]</color>";
        }
    }
}
