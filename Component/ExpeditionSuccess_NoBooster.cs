using CellMenu;
using GTFO.API;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using BoosterImplants;
using TMPro;
using UnityEngine.UI;

namespace LocalProgression.Component
{
    // not working, no icon
    public class ExpeditionSuccess_NoBooster : MonoBehaviour
    {
        private CM_PageExpeditionSuccess expeditionSuccessPage = null;

        private CM_ExpeditionSectorIcon m_completeWithNoBoosterIcon = null;

        private float m_time_clearedWithNoBooster = 7.5f;

        public void Setup()
        {
            if (expeditionSuccessPage != null && m_completeWithNoBoosterIcon != null) return;

            EventAPI.OnAssetsLoaded += () =>
            {
                expeditionSuccessPage = gameObject.GetComponent<CM_PageExpeditionSuccess>();
                if (expeditionSuccessPage == null)
                {
                    LPLogger.Error("ExpeditionSuccess_NoBooster.Setup: cannot find CM_PageExpeditionSuccess in parent.. Add `PageExpeditionSuccessWithNoBooster` to CM_ExpeditionSuccessPage as component first!");
                    return;
                }

                m_completeWithNoBoosterIcon = Instantiate(expeditionSuccessPage.m_sectorIconAllCompleted);
                if (m_completeWithNoBoosterIcon == null)
                {
                    LPLogger.Error("ExpeditionSuccess_NoBooster.Setup: cannot instantiate NoBooster icon...");
                }
            };

            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            LPLogger.Warning("ExpeditionSuccess_NoBooster: OnEnable");
            gameObject.SetActive(true);

            bool isClearedWithNoBosster = true;
            
            foreach(var playerBoosterImplantState in BoosterImplantManager.Current.m_boosterPlayers)
            {
                if (playerBoosterImplantState == null) continue;
                foreach (var boosterData in playerBoosterImplantState.BoosterImplantDatas)
                {
                    if (boosterData.BoosterImplantID > 0)
                    {
                        isClearedWithNoBosster = false;
                        break;
                    }
                }
            }
            const float width = 400f;
            int positionIndex = 0;
            bool hasSecondary = RundownManager.HasSecondaryLayer(RundownManager.ActiveExpedition);
            bool hasThird = RundownManager.HasThirdLayer(RundownManager.ActiveExpedition);
            positionIndex += hasSecondary ? 1 : 0;
            positionIndex += hasThird ? 1 : 0;
            positionIndex += 
                hasSecondary && WardenObjectiveManager.CurrentState.second_status == eWardenObjectiveStatus.WardenObjectiveItemSolved 
                && hasThird && WardenObjectiveManager.CurrentState.second_status == eWardenObjectiveStatus.WardenObjectiveItemSolved ? 1 : 0;

            SetupNoBoosterUsedIcon(isClearedWithNoBosster);

            m_completeWithNoBoosterIcon.SetPosition(new Vector2(positionIndex * width, 0f));

            m_completeWithNoBoosterIcon.BlinkIn(m_time_clearedWithNoBooster);
        }

        private void SetupNoBoosterUsedIcon(bool boosterUnused)
        {
            var icon = m_completeWithNoBoosterIcon;
            icon.m_isFinishedAll = true;
            icon.m_root = expeditionSuccessPage.transform;
            icon.SetupIcon(icon.m_iconMainSkull, icon.m_iconMainBG, false);
            icon.SetupIcon(icon.m_iconSecondarySkull, icon.m_iconSecondaryBG, false);
            icon.SetupIcon(icon.m_iconThirdSkull, icon.m_iconThirdBG, false);
            icon.SetupIcon(icon.m_iconFinishedAllSkull, icon.m_iconFinishedAllBG, true, true, 0.5f);
            icon.m_titleVisible = true;
            icon.m_isCleared = boosterUnused;
            icon.m_rightSideText.gameObject.SetActive(false);
            icon.m_title.text = "<color=orange>NO BOOSTER</color>";

            icon.m_title.Rebuild(CanvasUpdate.PreRender);
            icon.m_title.gameObject.SetActive(icon.m_titleVisible);
        }

        static ExpeditionSuccess_NoBooster()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ExpeditionSuccess_NoBooster>();
        }
    }
}
