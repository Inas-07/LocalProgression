using GameData;
using DropServer;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using CellMenu;
using LocalProgression.Data;
using Globals;

namespace LocalProgression
{
    public class LocalProgressionManager
    {
        public static readonly LocalProgressionManager Current = new();

        public static readonly string DirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GTFO-Modding", "LocalProgression");

        private RundownProgressionData CurrentRundownProgressionData { get; } = new RundownProgressionData();

        internal RundownManager.RundownProgData nativeProgData { get; private set; } = default;

        private CM_PageRundown_New rundownPage = null;

        private static string RundownLocalProgressionFilePath(string rundownName)
        {
            char[] invalidPathChars = Path.GetInvalidPathChars();

            foreach (char c in invalidPathChars)
            {
                rundownName = rundownName.Replace(c, '_');
            }

            return Path.Combine(DirPath, rundownName);
        }

        private static Dictionary<string, ExpeditionProgressionData> ReadRundownLocalProgressionData(string rundownName)
        {
            string filepath = RundownLocalProgressionFilePath(rundownName);
            var dataDict = new Dictionary<string, ExpeditionProgressionData>();

            if (File.Exists(filepath))
            {
                using (var stream = File.Open(filepath, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                    {
                        int LocalProgDict_Count = 0;
                        LocalProgDict_Count = reader.ReadInt32();
                        for (int cnt = 0; cnt < LocalProgDict_Count; cnt++)
                        {
                            ExpeditionProgressionData data = new ExpeditionProgressionData();
                            data.ExpeditionKey = reader.ReadString();
                            data.MainCompletionCount = reader.ReadInt32();
                            data.SecondaryCompletionCount = reader.ReadInt32();
                            data.ThirdCompletionCount = reader.ReadInt32();
                            data.AllClearCount = reader.ReadInt32();

                            dataDict.Add(data.ExpeditionKey, data);
                        }
                    }
                }
            }

            return dataDict;
        }

        internal RundownProgressionData GetLocalProgressionDataForCurrentRundown() => CurrentRundownProgressionData;

        private void SaveRundownProgressionDataToDisk()
        {
            string filepath = RundownLocalProgressionFilePath(CurrentRundownProgressionData.RundownName);

            LPLogger.Warning($"SaveData: saving to {filepath}");

            using (var stream = File.Open(filepath, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    var dataDict = CurrentRundownProgressionData.LocalProgressionDict;
                    writer.Write(dataDict.Count);
                    foreach (string expKey in CurrentRundownProgressionData.LocalProgressionDict.Keys)
                    {
                        ExpeditionProgressionData data = dataDict[expKey];
                        writer.Write(expKey);
                        writer.Write(data.MainCompletionCount);
                        writer.Write(data.SecondaryCompletionCount);
                        writer.Write(data.ThirdCompletionCount);
                        writer.Write(data.AllClearCount);
                    }
                }
            }
        }

        public void RecordExpeditionSuccessForCurrentRundown(string expeditionKey, bool mainLayerCleared, bool secondaryLayerCleared, bool thirdLayerCleared)
        {
            if (RundownManager.ActiveExpedition.ExcludeFromProgression) return;

            bool allLayerCleared = mainLayerCleared && secondaryLayerCleared && thirdLayerCleared;
            var dataDict = CurrentRundownProgressionData.LocalProgressionDict;

            if (!dataDict.ContainsKey(expeditionKey)) // first clear for this expedition
            {
                dataDict[expeditionKey] = new ExpeditionProgressionData()
                {
                    ExpeditionKey = expeditionKey,
                    MainCompletionCount = mainLayerCleared ? 1 : 0,
                    SecondaryCompletionCount = secondaryLayerCleared ? 1 : 0,
                    ThirdCompletionCount = thirdLayerCleared ? 1 : 0,
                    AllClearCount = allLayerCleared ? 1 : 0
                };

                CurrentRundownProgressionData.MainClearCount += mainLayerCleared ? 1 : 0;
                CurrentRundownProgressionData.SecondaryClearCount += secondaryLayerCleared ? 1 : 0;
                CurrentRundownProgressionData.ThirdClearCount += thirdLayerCleared ? 1 : 0;
                CurrentRundownProgressionData.AllClearCount += allLayerCleared ? 1 : 0;
            }

            else
            {
                ExpeditionProgressionData progData = dataDict[expeditionKey];

                if (progData.MainCompletionCount == 0 && mainLayerCleared) CurrentRundownProgressionData.MainClearCount += 1;
                if (progData.SecondaryCompletionCount == 0 && secondaryLayerCleared) CurrentRundownProgressionData.SecondaryClearCount += 1;
                if (progData.ThirdCompletionCount == 0 && thirdLayerCleared) CurrentRundownProgressionData.ThirdClearCount += 1;
                if (progData.AllClearCount == 0 && allLayerCleared) CurrentRundownProgressionData.AllClearCount += 1;

                progData.MainCompletionCount += mainLayerCleared ? 1 : 0;
                progData.SecondaryCompletionCount += secondaryLayerCleared ? 1 : 0;
                progData.ThirdCompletionCount += thirdLayerCleared ? 1 : 0;
                progData.AllClearCount += allLayerCleared ? 1 : 0;
            }

            SaveRundownProgressionDataToDisk();
        }

        public void UpdateLocalProgressionDataToActiveRundown()
        {
            var rundownKey = RundownManager.ActiveRundownKey;

            if (!RundownManager.TryGetIdFromLocalRundownKey(rundownKey, out uint rundownID) || rundownID == 0u)
            {
                LPLogger.Debug($"OnRundownProgressionUpdated: cannot find rundown with rundown key `{rundownKey}`!");
                return;
            }

            LPLogger.Warning($"Update LPData to rundown_id: {rundownID}");
            CurrentRundownProgressionData.Reset();

            RundownDataBlock rundownDB = GameDataBlockBase<RundownDataBlock>.GetBlock(rundownID);
            if (rundownDB == null)
            {
                LPLogger.Error($"Didn't find Rundown Datablock with rundown id {rundownID}");
                return;
            }

            var localProgressionDataDict = ReadRundownLocalProgressionData(rundownDB.name);
            CurrentRundownProgressionData.RundownID = rundownID;
            CurrentRundownProgressionData.RundownName = rundownDB.name;
            CurrentRundownProgressionData.LocalProgressionDict = localProgressionDataDict;

            foreach (var progressionData in localProgressionDataDict.Values)
            {
                CurrentRundownProgressionData.MainClearCount += progressionData.MainCompletionCount > 0 ? 1 : 0;
                CurrentRundownProgressionData.SecondaryClearCount += progressionData.SecondaryCompletionCount > 0 ? 1 : 0;
                CurrentRundownProgressionData.ThirdClearCount += progressionData.ThirdCompletionCount > 0 ? 1 : 0;
                CurrentRundownProgressionData.AllClearCount += progressionData.AllClearCount > 0 ? 1 : 0;
            }
        }

        internal void Init()
        {
            if (!Directory.Exists(DirPath))
            {
                Directory.CreateDirectory(DirPath);
            }
            
            RundownManager.OnRundownProgressionUpdated += new Action(OnNativeRundownProgressionUpdated);
        }

        internal void OnNativeRundownProgressionUpdated()
        {
            UpdateLocalProgressionDataToActiveRundown();

            if (rundownPage == null || !rundownPage.m_isActive)
            {
                // recompute on patch - CurrentRundownPage.OnActive
                //LPLogger.Debug("SetLocalProgressionDataToRundownPage: page is not active. Will set progression data to rundown page when page is active.");
                return;
            }

            UpdateRundownPageExpeditionIconProgression();
        }

        internal void SetCurrentRundownPageInstance(CM_PageRundown_New __instance) => rundownPage = __instance;

        private void UpdateRundownPageExpeditionIconProgression()
        {
            if (rundownPage == null) return;

            uint rundownID = CurrentRundownProgressionData.RundownID;
            if (rundownID == 0)
            {
                LPLogger.Warning($"UpdateRundownPageExpeditionIconProgression: rundown_id == 0!");
                return;
            }

            RundownDataBlock block = GameDataBlockBase<RundownDataBlock>.GetBlock(rundownID);
            LPLogger.Log($"CM_PageRundown_New.UpdateRundownExpeditionProgression, overwrite with LocalProgression Data, RundownID {CurrentRundownProgressionData.RundownID}");
            
            nativeProgData = ComputeLocalProgressionDataToRundownProgData();

            if(rundownPage.m_tierMarker1 != null)
            {
                rundownPage.m_tierMarker1.SetProgression(nativeProgData, new RundownTierProgressionData());
                if (rundownPage.m_expIconsTier1 != null)
                    UpdateTierIconsWithProgression(rundownPage.m_expIconsTier1, rundownPage.m_tierMarker1, true);
            }

            if (rundownPage.m_tierMarker2 != null)
            {
                rundownPage.m_tierMarker2.SetProgression(nativeProgData, block.ReqToReachTierB);
                if (rundownPage.m_expIconsTier2 != null)
                    UpdateTierIconsWithProgression(rundownPage.m_expIconsTier2, rundownPage.m_tierMarker2, nativeProgData.tierBUnlocked && block.UseTierUnlockRequirements);
            }

            if (rundownPage.m_tierMarker3 != null)
            {
                rundownPage.m_tierMarker3.SetProgression(nativeProgData, block.ReqToReachTierC);
                if (rundownPage.m_expIconsTier3 != null)
                    UpdateTierIconsWithProgression(rundownPage.m_expIconsTier3, rundownPage.m_tierMarker3, nativeProgData.tierCUnlocked && block.UseTierUnlockRequirements);
            }

            if (rundownPage.m_tierMarker4 != null)
            {
                rundownPage.m_tierMarker4.SetProgression(nativeProgData, block.ReqToReachTierD);
                if (rundownPage.m_expIconsTier4 != null)
                    UpdateTierIconsWithProgression(rundownPage.m_expIconsTier4, rundownPage.m_tierMarker4, nativeProgData.tierDUnlocked && block.UseTierUnlockRequirements);
            }

            if (rundownPage.m_tierMarker5 != null)
            {
                rundownPage.m_tierMarker5.SetProgression(nativeProgData, block.ReqToReachTierE);
                if (rundownPage.m_expIconsTier5 != null)
                    UpdateTierIconsWithProgression(rundownPage.m_expIconsTier5, rundownPage.m_tierMarker5, nativeProgData.tierEUnlocked && block.UseTierUnlockRequirements);
            }

            if(rundownPage.m_tierMarkerSectorSummary != null)
            {
                rundownPage.m_tierMarkerSectorSummary.SetSectorIconTextForMain($"<color=orange>[{nativeProgData.clearedMain}/{nativeProgData.totalMain}]</color>");
                rundownPage.m_tierMarkerSectorSummary.SetSectorIconTextForSecondary($"<color=orange>[{nativeProgData.clearedSecondary}/{nativeProgData.totalSecondary}]</color>");
                rundownPage.m_tierMarkerSectorSummary.SetSectorIconTextForThird($"<color=orange>[{nativeProgData.clearedThird}/{nativeProgData.totalThird}]</color>");
                rundownPage.m_tierMarkerSectorSummary.SetSectorIconTextForAllCleared($"<color=orange>[{nativeProgData.clearedAllClear}/{nativeProgData.totalAllClear}]</color>");
            }
        }

        private void UpdateTierIconsWithProgression(Il2CppSystem.Collections.Generic.List<CM_ExpeditionIcon_New> tierIcons, CM_RundownTierMarker tierMarker, bool thisTierUnlocked) 
        {
            if (tierIcons == null || tierIcons.Count == 0)
            {
                tierMarker?.SetVisible(false);
                return;
            }

            for (int index = 0; index < tierIcons.Count; ++index)
            {
                CM_ExpeditionIcon_New tierIcon = tierIcons[index];
                string progressionExpeditionKey = RundownManager.GetRundownProgressionExpeditionKey(tierIcons[index].Tier, tierIcons[index].ExpIndex);

                bool hasClearanceData = CurrentRundownProgressionData.LocalProgressionDict.TryGetValue(progressionExpeditionKey, out var expeditionProgression);

                string mainFinishCount = "0";
                string secondFinishCount = RundownManager.HasSecondaryLayer(tierIcons[index].DataBlock) ? "0" : "-";
                string thirdFinishCount = RundownManager.HasThirdLayer(tierIcons[index].DataBlock) ? "0" : "-";
                string allFinishedCount = RundownManager.HasAllCompletetionPossibility(tierIcons[index].DataBlock) ? "0" : "-";

                if (hasClearanceData)
                {
                    if (expeditionProgression.MainCompletionCount > 0)
                        mainFinishCount = expeditionProgression.MainCompletionCount.ToString();
                    if (expeditionProgression.SecondaryCompletionCount > 0)
                        secondFinishCount = expeditionProgression.SecondaryCompletionCount.ToString();
                    if (expeditionProgression.ThirdCompletionCount > 0)
                        thirdFinishCount = expeditionProgression.ThirdCompletionCount.ToString();
                    if (expeditionProgression.AllClearCount > 0)
                        allFinishedCount = expeditionProgression.AllClearCount.ToString();
                }

                bool expUnlocked = CheckExpeditionUnlocked(tierIcon.DataBlock, tierIcon.Tier);
                if (expUnlocked)
                {
                    if (hasClearanceData)
                    {
                        rundownPage.SetIconStatus(tierIcon, eExpeditionIconStatus.PlayedAndFinished, mainFinishCount, secondFinishCount, thirdFinishCount, allFinishedCount);
                    }
                    else
                    {
                        rundownPage.SetIconStatus(tierIcon, eExpeditionIconStatus.NotPlayed);
                    }
                }
                else if (hasClearanceData)
                {
                    rundownPage.SetIconStatus(tierIcon, eExpeditionIconStatus.TierLockedFinishedAnyway, mainFinishCount, secondFinishCount, thirdFinishCount, allFinishedCount);
                }
                else
                {
                    if (tierIcon.DataBlock.HideOnLocked)
                    {
                        tierIcon.SetVisible(false);
                    }
                    else
                    {
                        rundownPage.SetIconStatus(tierIcon, eExpeditionIconStatus.TierLocked);
                    }
                }
            }

            if (thisTierUnlocked)
            {
                tierMarker?.SetStatus(eRundownTierMarkerStatus.Unlocked);
            }
            else
            {
                tierMarker?.SetStatus(eRundownTierMarkerStatus.Locked);
            }
        }

        private RundownManager.RundownProgData ComputeLocalProgressionDataToRundownProgData()
        {
            RundownManager.RundownProgData rundownProgData = new RundownManager.RundownProgData();

            if(CurrentRundownProgressionData.RundownID == 0)
            {
                LPLogger.Error($"ComputeLocalProgressionDataToRundownProgData: rundown_id == 0...");
                return rundownProgData;
            }

            RundownDataBlock block = GameDataBlockBase<RundownDataBlock>.GetBlock(CurrentRundownProgressionData.RundownID);
            if (block == null)
            {
                LPLogger.Error($"ComputeLocalProgressionDataToRundownProgData: cannot get rundown datablock with rundown_id: {CurrentRundownProgressionData.RundownID}");
                return rundownProgData;
            }

            rundownProgData.clearedMain = CurrentRundownProgressionData.MainClearCount;
            rundownProgData.clearedSecondary = CurrentRundownProgressionData.SecondaryClearCount;
            rundownProgData.clearedThird = CurrentRundownProgressionData.ThirdClearCount;
            rundownProgData.clearedAllClear = CurrentRundownProgressionData.AllClearCount;

            AccumulateTierClearance(block, eRundownTier.TierA, ref rundownProgData);
            AccumulateTierClearance(block, eRundownTier.TierB, ref rundownProgData);
            AccumulateTierClearance(block, eRundownTier.TierC, ref rundownProgData);
            AccumulateTierClearance(block, eRundownTier.TierD, ref rundownProgData);
            AccumulateTierClearance(block, eRundownTier.TierE, ref rundownProgData);

            return rundownProgData;
        }

        private void AccumulateTierClearance(RundownDataBlock rundownDB, eRundownTier tier, ref RundownManager.RundownProgData progressionData)
        {
            var expeditionList = rundownDB.TierA; // assign TierA first, to get rid of that fking il2cpp list type specification

            switch(tier)
            {
                case eRundownTier.TierA: break;
                case eRundownTier.TierB: expeditionList = rundownDB.TierB; break;
                case eRundownTier.TierC: expeditionList = rundownDB.TierC; break;
                case eRundownTier.TierD: expeditionList = rundownDB.TierD; break;
                case eRundownTier.TierE: expeditionList = rundownDB.TierE; break;
                default: LPLogger.Error($"Unsupported eRundownTier {tier}"); return;
            }

            int index = 0;
            foreach (var exp in expeditionList)
            {
                if (!exp.Enabled) continue;
                
                if (exp.HideOnLocked) // R8 update
                {
                    bool unlocked = CheckExpeditionUnlocked(exp, tier);
                    if (!unlocked) continue;
                }

                progressionData.totalMain++;
                if (RundownManager.HasSecondaryLayer(exp)) progressionData.totalSecondary++;
                if (RundownManager.HasThirdLayer(exp)) progressionData.totalThird++;
                if (RundownManager.HasAllCompletetionPossibility(exp)) progressionData.totalAllClear++;
                if (exp.Descriptive.IsExtraExpedition) progressionData.totatlExtra++;
                string expKey = RundownManager.GetUniqueExpeditionKey(rundownDB, tier, index);
                if (CurrentRundownProgressionData.LocalProgressionDict.ContainsKey(expKey)) progressionData.clearedExtra++;

                index++;
            }
        }

        private bool CheckTierUnlocked(eRundownTier tier)
        {
            var rundownDB = RundownDataBlock.GetBlock(CurrentRundownProgressionData.RundownID);
            RundownTierProgressionData reqToReach = null;
            switch (tier)
            {
                case eRundownTier.TierA: return true;
                case eRundownTier.TierB: reqToReach = rundownDB.ReqToReachTierB; break;
                case eRundownTier.TierC: reqToReach = rundownDB.ReqToReachTierC; break;
                case eRundownTier.TierD: reqToReach = rundownDB.ReqToReachTierD; break;
                case eRundownTier.TierE: reqToReach = rundownDB.ReqToReachTierE; break;
                default: LPLogger.Error("Unsupporrted tier: {0}", tier); return true;
            }

            return CurrentRundownProgressionData.MainClearCount >= reqToReach.MainSectors
                && CurrentRundownProgressionData.SecondaryClearCount >= reqToReach.SecondarySectors
                && CurrentRundownProgressionData.ThirdClearCount >= reqToReach.ThirdSectors
                && CurrentRundownProgressionData.AllClearCount >= reqToReach.AllClearedSectors;
        }

        private bool CheckExpeditionUnlocked(ExpeditionInTierData expedition, eRundownTier tier)
        {
            switch (expedition.Accessibility)
            {
                case eExpeditionAccessibility.AlwayBlock:
                case eExpeditionAccessibility.BlockedAndScrambled:
                    return false;

                case eExpeditionAccessibility.AlwaysAllow: return true;
                case eExpeditionAccessibility.Normal: // depend on 'ReqToReachTierC' and all that
                    return CheckTierUnlocked(tier);

                case eExpeditionAccessibility.UseCustomProgressionLock:

                    RundownTierProgressionData reqToReach = expedition.CustomProgressionLock;
                    return CurrentRundownProgressionData.MainClearCount >= reqToReach.MainSectors
                        && CurrentRundownProgressionData.SecondaryClearCount >= reqToReach.SecondarySectors
                        && CurrentRundownProgressionData.ThirdClearCount >= reqToReach.ThirdSectors
                        && CurrentRundownProgressionData.AllClearCount >= reqToReach.AllClearedSectors;

                case eExpeditionAccessibility.UnlockedByExpedition:
                    var req = expedition.UnlockedByExpedition;
                    string expeditionKey = RundownManager.GetRundownProgressionExpeditionKey(req.Tier, (int)req.Exp);

                    return CurrentRundownProgressionData.LocalProgressionDict.ContainsKey(expeditionKey);

                default:
                    LPLogger.Warning("Unsupported eExpeditionAccessibility: {0}", expedition.Accessibility);
                    return true; // return true anyway
            }
        }

        // unused because it's fucked up (probably because of 'Seralizable')
        private void SetNativeRundownProgression()
        {
            var rundownProgressionData = RundownManager.RundownProgression.Expeditions;
            if (rundownProgressionData.Count > 0)
            {
                LPLogger.Warning($"Non-empty native rundown progression data! RundownID: {CurrentRundownProgressionData.RundownID}");
                rundownProgressionData.Clear();
            }

            var dataDict = CurrentRundownProgressionData.LocalProgressionDict;
            foreach (string expeditonKey in dataDict.Keys)
            {
                RundownProgression.Expedition expeditionData = new();
                var expeditionLocalData = dataDict[expeditonKey];

                expeditionData.AllLayerCompletionCount = expeditionLocalData.AllClearCount;
                expeditionData.Layers = new();

                RundownProgression.Expedition.Layer mainLayer, secondaryLayer, thirdLayer;

                mainLayer.CompletionCount = expeditionLocalData.MainCompletionCount;
                mainLayer.State = expeditionLocalData.MainCompletionCount > 0 ? LayerProgressionState.Completed : LayerProgressionState.Undiscovered;

                secondaryLayer.CompletionCount = expeditionLocalData.SecondaryCompletionCount;
                secondaryLayer.State = expeditionLocalData.SecondaryCompletionCount > 0 ? LayerProgressionState.Completed : LayerProgressionState.Undiscovered;

                thirdLayer.CompletionCount = expeditionLocalData.ThirdCompletionCount;
                thirdLayer.State = expeditionLocalData.ThirdCompletionCount > 0 ? LayerProgressionState.Completed : LayerProgressionState.Undiscovered;

                expeditionData.Layers.SetLayer(ExpeditionLayers.Main, mainLayer);
                expeditionData.Layers.SetLayer(ExpeditionLayers.Secondary, secondaryLayer);
                expeditionData.Layers.SetLayer(ExpeditionLayers.Third, thirdLayer);

                LPLogger.Warning($"{mainLayer.CompletionCount}, {secondaryLayer.CompletionCount}, {thirdLayer.CompletionCount}");

                rundownProgressionData[expeditonKey] = expeditionData;
            }
        }

        static LocalProgressionManager() {}

        private LocalProgressionManager() {}
    }
}
