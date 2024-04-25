using BoosterImplants;
using CellMenu;
using GameData;
using GTFO.API;
using Il2CppInterop.Runtime.Injection;
using MS.Internal.Xml.XPath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements.UIR;

namespace LocalProgression.Component
{
    internal class ExpeditionWindow_NoBoosterIcon: MonoBehaviour
    {
        internal CM_ExpeditionWindow m_window;
        private CM_ExpeditionSectorIcon m_completeWithNoBoosterIcon = null;
        private NoBoosterIconGOWrapper Wrapper;
        private SpriteRenderer m_icon;
        private SpriteRenderer m_bg;
        private TextMeshPro m_title;
        private TextMeshPro m_rightSideText;

        internal void InitialSetup()
        {
            if (m_window == null)
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

            m_completeWithNoBoosterIcon = GOUtil.SpawnChildAndGetComp<CM_ExpeditionSectorIcon>(Assets.NoBoosterIcon, m_window.m_sectorIconAlign);
            Wrapper = new(m_completeWithNoBoosterIcon.gameObject);

            m_bg = Wrapper.BGGO.GetComponent<SpriteRenderer>();
            m_icon = Wrapper.IconGO.GetComponent<SpriteRenderer>();

            m_title = Instantiate(m_window.m_sectorIconMain.m_title);
            m_title.transform.SetParent(Wrapper.ObjectiveIcon.transform, false);
            m_rightSideText = Instantiate(m_window.m_sectorIconMain.m_rightSideText);
            m_rightSideText.transform.SetParent(Wrapper.RightSideText.transform, false);

            m_completeWithNoBoosterIcon.m_title = m_title;
            m_completeWithNoBoosterIcon.m_rightSideText = m_rightSideText;

            m_completeWithNoBoosterIcon.SetAnchor(GuiAnchor.TopLeft, true);
            m_completeWithNoBoosterIcon.SetVisible(false);
            m_completeWithNoBoosterIcon.SortAsPopupLayer();

            m_completeWithNoBoosterIcon.m_root = m_window.m_root;


        }

        internal void SetVisible(bool visible)
        {
            m_completeWithNoBoosterIcon.SetVisible(visible);
        }

        internal void SetIconPosition(Vector2 position) => m_completeWithNoBoosterIcon.SetPosition(position);


        internal void BlinkIn(float delay)
        {
            m_completeWithNoBoosterIcon.BlinkIn(delay);
        }

        internal void SetupNoBoosterUsedIcon(bool boosterUnused)
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
            if(boosterUnused)
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

        private void OnDestroy()
        {
            m_icon = m_bg = null;
            m_completeWithNoBoosterIcon = null;
            Wrapper.Destory();
        }

        static ExpeditionWindow_NoBoosterIcon()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ExpeditionWindow_NoBoosterIcon>();
        }
    }
}
