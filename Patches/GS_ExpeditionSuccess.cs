using HarmonyLib;

namespace LocalProgression.Patches
{
    [HarmonyPatch]
    internal class Patches_GS_ExpeditionSuccess
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GS_ExpeditionSuccess), nameof(GS_ExpeditionSuccess.Enter))]
        private static void DoChangeState(GS_ExpeditionSuccess __instance)
        {
            var CompletedExpedtionData = RundownManager.GetActiveExpeditionData();

            string expeditionKey = LocalProgressionManager.Current.ExpeditionKey(CompletedExpedtionData.tier, CompletedExpedtionData.expeditionIndex);
            bool mainLayerCleared = WardenObjectiveManager.CurrentState.main_status == eWardenObjectiveStatus.WardenObjectiveItemSolved || WardenObjectiveManager.GetWinOnDeath();
            bool secondaryLayerCleared = WardenObjectiveManager.CurrentState.second_status == eWardenObjectiveStatus.WardenObjectiveItemSolved;
            bool thirdLayerCleared = WardenObjectiveManager.CurrentState.third_status == eWardenObjectiveStatus.WardenObjectiveItemSolved;
            bool clearedWithNoBooster = LocalProgressionManager.Current.AllSectorCompletedWithoutBoosterAndCheckpoint();

            LPLogger.Debug($"Level cleared, recording - {expeditionKey}");
            
            LocalProgressionManager.Current.RecordExpeditionSuccessForCurrentRundown(expeditionKey, mainLayerCleared, secondaryLayerCleared, thirdLayerCleared, clearedWithNoBooster);
        }
    }
}
