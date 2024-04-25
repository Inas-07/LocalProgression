using CellMenu;
using UnityEngine;
using HarmonyLib;
using LevelGeneration;
using LocalProgression.Component;
using SNetwork;

namespace LocalProgression.Patches
{
    [HarmonyPatch]
    internal static class Patches_CM_ExpeditionWindow
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_ExpeditionWindow), nameof(CM_ExpeditionWindow.Setup))]
        private static void Post_Setup(CM_ExpeditionWindow __instance)
        {
            var w = __instance.gameObject.AddComponent<ExpeditionWindow_NoBoosterIcon>();
            w.m_window = __instance;
            w.InitialSetup();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_ExpeditionWindow), nameof(CM_ExpeditionWindow.SetExpeditionInfo))]
        private static void Post_SetExpeditionInfo(CM_ExpeditionWindow __instance)
        {
            var w = __instance.gameObject.GetComponent<ExpeditionWindow_NoBoosterIcon>();
            if (w == null) return;
            float x = 0f;
            float interval = 410f;
            __instance.m_sectorIconMain.SetPosition(new Vector2(x, 0f));
            x += interval;
            if (RundownManager.HasSecondaryLayer(__instance.m_data))
            {
                x += interval;
            }
            if (RundownManager.HasThirdLayer(__instance.m_data))
            {
                x += interval;
            }

            var LPData = LocalProgressionManager.Current.GetExpeditionLP(LocalProgressionManager.Current.ActiveRundownID(), __instance.m_tier, __instance.m_expIndex);
            if (RundownManager.HasAllCompletetionPossibility(__instance.m_data) && LPData.AllClearCount > 0)
            {
                x += interval;
            }

            w.SetIconPosition(new Vector2(x, 0f));
        }


        // patch for setup clear icon properly
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CM_ExpeditionWindow), nameof(CM_ExpeditionWindow.SetVisible))]
        private static bool Pre_SetVisible(CM_ExpeditionWindow __instance, bool visible, bool inMenuBar)
        {
            var noBoosterIcon = __instance.GetComponent<ExpeditionWindow_NoBoosterIcon>();

            if (!visible)
            {
                //noBoosterIcon.SetVisible(false);
                return true;
            }

            var expData = __instance.m_data;
            if (expData == null) return true;

            var RundownID = LocalProgressionManager.Current.ActiveRundownID();
            var LPData = LocalProgressionManager.Current.GetExpeditionLP(RundownID, __instance.m_tier, __instance.m_expIndex);

            __instance.gameObject.SetActive(visible);
            __instance.m_joinWindow.SetVisible(!inMenuBar && !SNet.IsInLobby);
            __instance.m_hostButton.SetVisible(!inMenuBar && !SNet.IsInLobby && __instance.m_status != eExpeditionIconStatus.TierLocked && __instance.m_status != eExpeditionIconStatus.LockedAndScrambled && __instance.m_status != eExpeditionIconStatus.TierLockedFinishedAnyway);
            __instance.m_changeExpeditionButton.SetVisible(SNet.IsMaster && !__instance.m_hostButton.IsVisible && !inMenuBar && SNet.IsInLobby && RundownManager.ActiveExpedition != null && __instance.m_status != eExpeditionIconStatus.TierLocked && __instance.m_status != eExpeditionIconStatus.LockedAndScrambled && __instance.m_status != eExpeditionIconStatus.TierLockedFinishedAnyway);
            __instance.m_bottomStripes.SetActive(!__instance.m_hostButton.IsVisible && !__instance.m_changeExpeditionButton.IsVisible);

            __instance.m_title.gameObject.SetActive(false);
            __instance.m_wardenObjective.gameObject.SetActive(false);
            __instance.m_wardenIntel.gameObject.SetActive(false);
            __instance.m_depthTitle.gameObject.SetActive(false);
            __instance.m_artifactHeatTitle.gameObject.SetActive(false);
            CoroutineManager.BlinkIn(__instance.m_title, 0.3f, null);
            CoroutineManager.BlinkIn(__instance.m_wardenObjective, 1.1f, null);
            CoroutineManager.BlinkIn(__instance.m_wardenIntel, 2.5f, null);
            CoroutineManager.BlinkIn(__instance.m_depthTitle, 3f, null);
            CoroutineManager.BlinkIn(__instance.m_artifactHeatTitle, 3.25f, null);

            float delay = 1.8f;
            float interval = 0.4f;
            __instance.m_sectorIconMain.Setup(LG_LayerType.MainLayer, __instance.m_root, true,
                LPData.MainCompletionCount > 0,
                true, 0.5f, 0.8f);
            __instance.m_sectorIconMain.SetVisible(false);
            __instance.m_sectorIconSecond.SetVisible(false);
            __instance.m_sectorIconThird.SetVisible(false);
            __instance.m_sectorIconAllCompleted.SetVisible(false);
            noBoosterIcon.SetVisible(false);
            __instance.m_sectorIconMain.StopBlink();
            __instance.m_sectorIconSecond.StopBlink();
            __instance.m_sectorIconThird.StopBlink();
            __instance.m_sectorIconAllCompleted.StopBlink();
            
            __instance.m_sectorIconMain.BlinkIn(delay);
            delay += interval;

            if (RundownManager.HasSecondaryLayer(__instance.m_data))
            {
                __instance.m_sectorIconSecond.Setup(LG_LayerType.SecondaryLayer, __instance.m_root, true,
                    LPData.SecondaryCompletionCount > 0,
                    true, 0.5f, 0.8f);
                __instance.m_sectorIconSecond.BlinkIn(delay);
                delay += interval;
            }
            if (RundownManager.HasThirdLayer(__instance.m_data))
            {
                __instance.m_sectorIconThird.Setup(LG_LayerType.ThirdLayer, __instance.m_root, true,
                    LPData.ThirdCompletionCount > 0,
                    true, 0.5f, 0.8f);
                __instance.m_sectorIconThird.BlinkIn(delay);
                delay += interval;
            }
            if (LPData.AllClearCount > 0)
            {
                __instance.m_sectorIconAllCompleted.BlinkIn(delay);
                delay += interval;
            }

            if(LocalProgressionManager.Current.TryGetRundownConfig(RundownID, out var rundownConf) && rundownConf.EnableNoBoosterUsedProgressionForRundown 
                || LocalProgressionManager.Current.TryGetExpeditionConfig(RundownID, __instance.m_tier, __instance.m_expIndex, out var conf) && conf.EnableNoBoosterUsedProgression)
            {
                noBoosterIcon.SetupNoBoosterUsedIcon(LPData.NoBoosterAllClearCount > 0);
                noBoosterIcon.BlinkIn(delay);
            }

            return false;
        }
    }
}
