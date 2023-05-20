using HarmonyLib;

namespace LocalProgression.Patches
{
    [HarmonyPatch]
    internal class GS_ExpeditionSuccess_Enter
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GS_ExpeditionSuccess), nameof(GS_ExpeditionSuccess.Enter))]
        private static void DoChangeState(GS_ExpeditionSuccess __instance)
        {
            var CompletedExpedtionData = RundownManager.GetActiveExpeditionData();

            string expeditionKey = RundownManager.GetRundownProgressionExpeditionKey(CompletedExpedtionData.tier, CompletedExpedtionData.expeditionIndex);
            bool mainLayerCleared = WardenObjectiveManager.CurrentState.main_status == eWardenObjectiveStatus.WardenObjectiveItemSolved;
            bool secondaryLayerCleared = WardenObjectiveManager.CurrentState.second_status == eWardenObjectiveStatus.WardenObjectiveItemSolved;
            bool thirdLayerCleared = WardenObjectiveManager.CurrentState.third_status == eWardenObjectiveStatus.WardenObjectiveItemSolved;

            LocalProgressionManager.Current.RecordExpeditionSuccessForCurrentRundown(expeditionKey, mainLayerCleared, secondaryLayerCleared, thirdLayerCleared);
        }
    }
}
