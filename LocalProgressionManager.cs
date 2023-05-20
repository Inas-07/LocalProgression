using GameData;
using DropServer;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using CellMenu;
using Globals;
using LocalProgression.Data;

namespace LocalProgression
{
    public class LocalProgressionManager
    {
        public static readonly LocalProgressionManager Current = new();

        public static readonly string DirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GTFO-Modding", "LocalProgression");

        private RundownProgressionData CurrentRundownProgressionData = new RundownProgressionData();

        private CM_PageRundown_New CurrentRundownPage = null;

        internal RundownProgressionData GetLocalProgressionDataForCurrentRundown() => CurrentRundownProgressionData;

        // TODO: support multi-rundown
        private static string RundownLocalProgressionFilePath(string rundownName)
        {
            char[] invalidPathChars = Path.GetInvalidPathChars();

            foreach (char c in invalidPathChars)
            {
                rundownName = rundownName.Replace(c, '_');
            }

            return Path.Combine(DirPath, rundownName);
        }

        private void SaveRundownProgressionDataToDisk()
        {
            string filepath = RundownLocalProgressionFilePath(CurrentRundownProgressionData.RundownName);

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

        // to be invoked at GS_ExpeditionSuccess
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

        public void UpdateLocalProgressionDataToRundown(uint rundownID)
        {
            CurrentRundownProgressionData.Reset();

            RundownDataBlock rundownDB = GameDataBlockBase<RundownDataBlock>.GetBlock(rundownID);
            if (rundownDB == null)
            {
                LocalProgressionLogger.Error($"Didn't find Rundown Datablock with rundown id {rundownID}");
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

        public void Init()
        {
            if (!Directory.Exists(DirPath))
            {
                Directory.CreateDirectory(DirPath);
            }
            
            RundownManager.OnRundownProgressionUpdated += new Action(OnNativeRundownProgressionUpdated);
        }

        private void OnNativeRundownProgressionUpdated()
        {
            var rundownKey = RundownManager.ActiveRundownKey;

            uint rundownID = 0u;
            if(!RundownManager.TryGetIdFromLocalRundownKey(rundownKey, out rundownID) || rundownID == 0u)
            {
                LocalProgressionLogger.Error($"OnRundownProgressionUpdated: cannot find rundown with rundown key `{rundownKey}`!");
                return;
            }

            LocalProgressionLogger.Log($"OnNativeRundownProgressionUpdated: Update LocalProgression Data to rundown id {rundownID}");
            UpdateLocalProgressionDataToRundown(rundownID);

            if (!CurrentRundownPage.m_isActive)
            {
                // recompute on patch - CurrentRundownPage.OnActive
                LocalProgressionLogger.Error("SetLocalProgressionDataToRundownPage: page is not active");
                return;
            }

            UpdateRundownPageExpeditionIconProgression();
        }

        internal void SetCurrentRundownPageInstance(CM_PageRundown_New __instance) => CurrentRundownPage = __instance;

        internal void UpdateRundownPageExpeditionIconProgression()
        {
            if (CurrentRundownPage == null) return;

            RundownDataBlock block = GameDataBlockBase<RundownDataBlock>.GetBlock(CurrentRundownProgressionData.RundownID);
            LocalProgressionLogger.Log($"CM_PageRundown_New.UpdateRundownExpeditionProgression, overwrite with LocalProgression Data, RundownID {CurrentRundownProgressionData.RundownID}");
            
            RundownManager.RundownProgData nativeLocalProgData = ComputeLocalProgressionDataToRundownProgData();
            if (CurrentRundownPage.m_tierMarkerSectorSummary != null)
            {
                CurrentRundownPage.m_tierMarkerSectorSummary.SetSectorIconTextForMain(nativeLocalProgData.clearedMain.ToString() + "<size=50%><color=#FFFFFF33><size=55%>/" + nativeLocalProgData.totalMain + "</color></size>");
                CurrentRundownPage.m_tierMarkerSectorSummary.SetSectorIconTextForSecondary(nativeLocalProgData.clearedSecondary.ToString() + "<size=50%><color=#FFFFFF33><size=55%>/" + nativeLocalProgData.totalSecondary + "</color></size>");
                CurrentRundownPage.m_tierMarkerSectorSummary.SetSectorIconTextForThird(nativeLocalProgData.clearedThird.ToString() + "<size=50%><color=#FFFFFF33><size=55%>/" + nativeLocalProgData.totalThird + "</color></size>");
                CurrentRundownPage.m_tierMarkerSectorSummary.SetSectorIconTextForAllCleared(nativeLocalProgData.clearedAllClear.ToString() + "<size=50%><color=#FFFFFF33><size=55%>/" + nativeLocalProgData.totalAllClear + "</color></size>");
            }

            if (CurrentRundownPage.m_tierMarker1 == null) return;

            CurrentRundownPage.m_tierMarker1.SetProgression(nativeLocalProgData, new RundownTierProgressionData());
            UpdateTierIconsWithProgression(CurrentRundownPage.m_expIconsTier1, CurrentRundownPage.m_tierMarker1, true);
            CurrentRundownPage.m_tierMarker2.SetProgression(nativeLocalProgData, block.ReqToReachTierB);
            UpdateTierIconsWithProgression(CurrentRundownPage.m_expIconsTier2, CurrentRundownPage.m_tierMarker2, nativeLocalProgData.tierBUnlocked && block.UseTierUnlockRequirements);
            CurrentRundownPage.m_tierMarker3.SetProgression(nativeLocalProgData, block.ReqToReachTierC);
            UpdateTierIconsWithProgression(CurrentRundownPage.m_expIconsTier3, CurrentRundownPage.m_tierMarker3, nativeLocalProgData.tierCUnlocked && block.UseTierUnlockRequirements);
            CurrentRundownPage.m_tierMarker4.SetProgression(nativeLocalProgData, block.ReqToReachTierD);
            UpdateTierIconsWithProgression(CurrentRundownPage.m_expIconsTier4, CurrentRundownPage.m_tierMarker4, nativeLocalProgData.tierDUnlocked && block.UseTierUnlockRequirements);
            CurrentRundownPage.m_tierMarker5.SetProgression(nativeLocalProgData, block.ReqToReachTierE);
            UpdateTierIconsWithProgression(CurrentRundownPage.m_expIconsTier5, CurrentRundownPage.m_tierMarker5, nativeLocalProgData.tierEUnlocked && block.UseTierUnlockRequirements);
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

                ExpeditionProgressionData expeditionProgression;
                bool hasClearanceData = CurrentRundownProgressionData.LocalProgressionDict.TryGetValue(progressionExpeditionKey, out expeditionProgression);

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
                if (thisTierUnlocked | expUnlocked)
                {
                    if (hasClearanceData)
                    {
                        CurrentRundownPage.SetIconStatus(tierIcon, eExpeditionIconStatus.PlayedAndFinished, mainFinishCount, secondFinishCount, thirdFinishCount, allFinishedCount);
                    }
                    else
                    {
                        CurrentRundownPage.SetIconStatus(tierIcon, eExpeditionIconStatus.NotPlayed);
                    }
                }
                else if (hasClearanceData)
                {
                    CurrentRundownPage.SetIconStatus(tierIcon, eExpeditionIconStatus.TierLockedFinishedAnyway, mainFinishCount, secondFinishCount, thirdFinishCount, allFinishedCount);
                }
                else
                {
                    CurrentRundownPage.SetIconStatus(tierIcon, eExpeditionIconStatus.TierLocked);
                }
            }

            if (thisTierUnlocked)
            {
                tierMarker.SetStatus(eRundownTierMarkerStatus.Unlocked);
            }
            else
            {
                tierMarker.SetStatus(eRundownTierMarkerStatus.Locked);
            }
        }

        private RundownManager.RundownProgData ComputeLocalProgressionDataToRundownProgData()
        {
            RundownManager.RundownProgData rundownProgData = new RundownManager.RundownProgData();

            RundownDataBlock block = GameDataBlockBase<RundownDataBlock>.GetBlock(CurrentRundownProgressionData.RundownID);
            if (block == null) return rundownProgData;

            rundownProgData.clearedMain = CurrentRundownProgressionData.MainClearCount;
            rundownProgData.clearedSecondary = CurrentRundownProgressionData.SecondaryClearCount;
            rundownProgData.clearedThird = CurrentRundownProgressionData.ThirdClearCount;
            rundownProgData.clearedAllClear = CurrentRundownProgressionData.AllClearCount;

            AccumulateTierClearanceToProgressionData(block, eRundownTier.TierA, ref rundownProgData);
            AccumulateTierClearanceToProgressionData(block, eRundownTier.TierB, ref rundownProgData);
            AccumulateTierClearanceToProgressionData(block, eRundownTier.TierC, ref rundownProgData);
            AccumulateTierClearanceToProgressionData(block, eRundownTier.TierD, ref rundownProgData);
            AccumulateTierClearanceToProgressionData(block, eRundownTier.TierE, ref rundownProgData);

            CheckTierUnlocked(eRundownTier.TierB);
            CheckTierUnlocked(eRundownTier.TierC);
            CheckTierUnlocked(eRundownTier.TierD);
            CheckTierUnlocked(eRundownTier.TierE);
            return rundownProgData;
        }

        private void AccumulateTierClearanceToProgressionData(RundownDataBlock rundownDB, eRundownTier tier, ref RundownManager.RundownProgData progressionData)
        {
            var expeditionList = rundownDB.TierA; // assign TierA first, to get rid of that fking il2cpp list type specification

            switch(tier)
            {
                case eRundownTier.TierA: break;
                case eRundownTier.TierB: expeditionList = rundownDB.TierB; break;
                case eRundownTier.TierC: expeditionList = rundownDB.TierC; break;
                case eRundownTier.TierD: expeditionList = rundownDB.TierD; break;
                case eRundownTier.TierE: expeditionList = rundownDB.TierE; break;
                default: LocalProgressionLogger.Error($"Unsupported eRundownTier {tier}"); return;
            }

            int index = 0;
            foreach (var exp in expeditionList)
            {
                if (!exp.Enabled) continue;

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
                default: LocalProgressionLogger.Error("Unsupporrted tier: {0}", tier); return true;
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
                    LocalProgressionLogger.Warning("Unsupported eExpeditionAccessibility: {0}", expedition.Accessibility);
                    return true; // return true anyway
            }
        }

        // unused because it's fucked up (probably because of 'Seralizable')
        private void SetNativeRundownProgression()
        {
            var rundownProgressionData = RundownManager.RundownProgression.Expeditions;
            if (rundownProgressionData.Count > 0)
            {
                LocalProgressionLogger.Warning($"Non-empty native rundown progression data! RundownID: {CurrentRundownProgressionData.RundownID}");
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

                LocalProgressionLogger.Warning($"{mainLayer.CompletionCount}, {secondaryLayer.CompletionCount}, {thirdLayer.CompletionCount}");

                rundownProgressionData[expeditonKey] = expeditionData;
            }
        }

        static LocalProgressionManager() {}

        private LocalProgressionManager() {}
    }
}
