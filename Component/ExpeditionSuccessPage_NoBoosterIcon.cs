using CellMenu;
using GTFO.API;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using BoosterImplants;
using TMPro;
using GameData;

namespace LocalProgression.Component
{
    // not working, no icon
    public class ExpeditionSuccessPage_NoBoosterIcon : MonoBehaviour
    {
        //public GameObject GameObject { get; private set; } = null;

        internal CM_PageExpeditionSuccess m_page = null;
        private CM_ExpeditionSectorIcon m_completeWithNoBoosterIcon = null;
        private SpriteRenderer m_icon;
        private SpriteRenderer m_bg;
        private TextMeshPro m_title;
        private TextMeshPro m_rightSideText;

        private NoBoosterIconGOWrapper Wrapper;

        internal void Setup()
        {
            if(m_page == null)
            {
                LPLogger.Error("ExpeditionSuccess_NoBooster: Assign the page instance before setup");
                return;
            }

            if (m_completeWithNoBoosterIcon != null) return;

            if(Assets.NoBoosterIcon == null)
            {
                AssetAPI.OnAssetBundlesLoaded += () => {
                    LoadAsset();
                    AssetAPI.OnAssetBundlesLoaded -= LoadAsset;
                };
            }
            else
            {
                LoadAsset();
            }
        }

        private void LoadAsset()
        {
            if (Assets.NoBoosterIcon == null)
            {
                LPLogger.Error("ExpeditionSuccess_NoBooster.Setup: cannot instantiate NoBooster icon...");
                return;
            }

            m_completeWithNoBoosterIcon = m_page.m_guiLayer.AddRectComp(Assets.NoBoosterIcon, GuiAnchor.TopLeft,
                new Vector2(1200f, 0f), m_page.m_sectorIconAlign).Cast<CM_ExpeditionSectorIcon>();

            Wrapper = new(m_completeWithNoBoosterIcon.gameObject);

            m_bg = Wrapper.BGGO.GetComponent<SpriteRenderer>();
            m_icon = Wrapper.IconGO.GetComponent<SpriteRenderer>();

            m_title = Object.Instantiate(m_page.m_sectorIconMain.m_title);
            m_title.transform.SetParent(Wrapper.ObjectiveIcon.transform, false);
            m_rightSideText = Object.Instantiate(m_page.m_sectorIconMain.m_rightSideText);
            m_rightSideText.transform.SetParent(Wrapper.RightSideText.transform, false);

            m_completeWithNoBoosterIcon.m_title = m_title;
            m_completeWithNoBoosterIcon.m_rightSideText = m_rightSideText;

            m_completeWithNoBoosterIcon.SetVisible(false);
        }

        private void OnEnable()
        {
            m_completeWithNoBoosterIcon.SetVisible(false);

            uint rundownID = LocalProgressionManager.Current.ActiveRundownID();
            var expData = RundownManager.GetActiveExpeditionData();
            if (!(
                LocalProgressionManager.Current.TryGetRundownConfig(rundownID, out var rundownConf) 
                && rundownConf.EnableNoBoosterUsedProgressionForRundown 
                ||
                LocalProgressionManager.Current.TryGetExpeditionConfig(rundownID, expData.tier, expData.expeditionIndex, out var conf) 
                && conf.EnableNoBoosterUsedProgression))
            {
                return;
            }

            bool isClearedWithNoBooster = LocalProgressionManager.Current.AllSectorCompletedWithoutBoosterAndCheckpoint();

            const float width = 400f;
            int positionIndex = 1;
            bool hasSecondary = RundownManager.HasSecondaryLayer(RundownManager.ActiveExpedition);
            bool hasThird = RundownManager.HasThirdLayer(RundownManager.ActiveExpedition);
            positionIndex += hasSecondary ? 1 : 0;
            positionIndex += hasThird ? 1 : 0;
            positionIndex += 
                hasSecondary && WardenObjectiveManager.CurrentState.second_status == eWardenObjectiveStatus.WardenObjectiveItemSolved 
                && hasThird && WardenObjectiveManager.CurrentState.second_status == eWardenObjectiveStatus.WardenObjectiveItemSolved ? 1 : 0;

            float delay = m_page.m_time_sectorIcon + positionIndex * 0.7f;

            SetupNoBoosterUsedIcon(isClearedWithNoBooster);
            m_completeWithNoBoosterIcon.SetPosition(new Vector2(positionIndex * width, 0f));

            m_completeWithNoBoosterIcon.BlinkIn(delay);
        }

        private void SetupNoBoosterUsedIcon(bool boosterUnused)
        {
            var icon = m_completeWithNoBoosterIcon;
            icon.m_isFinishedAll = true;
            icon.SetupIcon(icon.m_iconMainSkull, icon.m_iconMainBG, false);
            icon.SetupIcon(icon.m_iconSecondarySkull, icon.m_iconSecondaryBG, false);
            icon.SetupIcon(icon.m_iconThirdSkull, icon.m_iconThirdBG, false);
            icon.SetupIcon(icon.m_iconFinishedAllSkull, icon.m_iconFinishedAllBG, false, false, 0.5f);
            var cIcon = m_icon.color;
            var cBg = m_bg.color;
            m_icon.color = new(cIcon.r, cIcon.g, cIcon.b, boosterUnused ? 1.0f : 0.4f);
            m_bg.color = new(cBg.r, cBg.g, cBg.b, boosterUnused ? 1.0f : 0.3f);
            m_title.alpha = (boosterUnused ? 1f : 0.2f);

            icon.m_titleVisible = true;
            icon.m_isCleared = boosterUnused;

            // blink in sound control
            if (boosterUnused)
            {
                icon.m_isFinishedAll = true;
            }
            else
            {
                icon.m_isFinishedAll = false;
                icon.m_type = LevelGeneration.LG_LayerType.MainLayer;
            }

            icon.m_rightSideText.gameObject.SetActive(false);

            var text_db = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.ExpeditionSuccessPage.AllClearWithNoBoosterUsed");
            if(text_db != null)
            {
                icon.m_title.SetText(Localization.Text.Get(text_db.persistentID));
            }
            else
            {
                icon.m_title.SetText("<color=orange>OMNIPOTENT</color>");
            }

            //sector_icon.m_title.fontSize = m_page.m_sectorIconMain.m_title.fontSize;
            icon.m_title.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            m_icon = m_bg = null;
            m_completeWithNoBoosterIcon = null;
            Wrapper.Destory();
        }

        static ExpeditionSuccessPage_NoBoosterIcon()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ExpeditionSuccessPage_NoBoosterIcon>();
        }
    }
}
