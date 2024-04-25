using CellMenu;
using GameData;
using GTFO.API;
using Il2CppInterop.Runtime.Injection;
using System.Drawing;
using TMPro;
using UnityEngine;

namespace LocalProgression.Component
{
    internal class RundownTierMarker_NoBoosterIcon: MonoBehaviour
    {
        internal CM_RundownTierMarker m_tierMarker;

        private CM_ExpeditionSectorIcon m_completeWithNoBoosterIcon = null;
        private SpriteRenderer m_icon;
        private SpriteRenderer m_bg;
        private TextMeshPro m_title;
        private TextMeshPro m_rightSideText;

        private NoBoosterIconGOWrapper Wrapper;

        internal void Setup()
        {
            if (m_tierMarker == null)
            {
                LPLogger.Error("ExpeditionSuccess_NoBooster: Assign the page instance before setup");
                return;
            }

            if (m_completeWithNoBoosterIcon != null) return;

            if (Assets.NoBoosterIcon == null)
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

            m_completeWithNoBoosterIcon = GOUtil.SpawnChildAndGetComp<CM_ExpeditionSectorIcon>(
                Assets.NoBoosterIcon, m_tierMarker.m_sectorIconAlign_main);

            Wrapper = new(m_completeWithNoBoosterIcon.gameObject);

            m_bg = Wrapper.BGGO.GetComponent<SpriteRenderer>();
            m_icon = Wrapper.IconGO.GetComponent<SpriteRenderer>();

            m_title = Instantiate(m_tierMarker.m_sectorIconSummaryMain.m_title);
            m_title.transform.SetParent(Wrapper.ObjectiveIcon.transform, false);
            m_rightSideText = Instantiate(m_tierMarker.m_sectorIconSummaryMain.m_rightSideText);
            m_rightSideText.transform.SetParent(Wrapper.RightSideText.transform, false);

            m_completeWithNoBoosterIcon.m_title = m_title;
            m_completeWithNoBoosterIcon.m_rightSideText = m_rightSideText;
            
            SetupNoBoosterUsedIcon(true);

            float scale = 0.16f;
            Vector3 localScale = new Vector3(scale, scale, scale);
            float num = scale / 0.16f;

            m_completeWithNoBoosterIcon.transform.localScale = localScale;
            var diff = m_tierMarker.m_sectorIconSummarySecondary.GetPosition() - m_tierMarker.m_sectorIconSummaryMain.GetPosition();

            m_completeWithNoBoosterIcon.SetPosition(diff * 4);
            m_completeWithNoBoosterIcon.SetVisible(false);
        }

        internal void SetVisible(bool visible) => m_completeWithNoBoosterIcon.SetVisible(visible);

        internal void SetSectorIconText(string text)
        {
            m_completeWithNoBoosterIcon.SetVisible(true);
            m_completeWithNoBoosterIcon.SetRightSideText(text);
        }

        private void SetupNoBoosterUsedIcon(bool boosterUnused)
        {
            var icon = m_completeWithNoBoosterIcon;
            icon.m_isFinishedAll = true;
            icon.SetupIcon(icon.m_iconMainSkull, icon.m_iconMainBG, false);
            icon.SetupIcon(icon.m_iconSecondarySkull, icon.m_iconSecondaryBG, false);
            icon.SetupIcon(icon.m_iconThirdSkull, icon.m_iconThirdBG, false);
            icon.SetupIcon(icon.m_iconFinishedAllSkull, icon.m_iconFinishedAllBG, false, false, 0.5f);
            //icon.SetupIcon(m_icon, m_bg, true, boosterUnused, 1.0f, 1.0f);
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
            if (text_db != null)
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

        static RundownTierMarker_NoBoosterIcon()
        {
            ClassInjector.RegisterTypeInIl2Cpp<RundownTierMarker_NoBoosterIcon>();
        }
    }
}
