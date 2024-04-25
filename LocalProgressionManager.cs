using GameData;
using DropServer;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using CellMenu;
using LocalProgression.Data;
using Globals;
using BoosterImplants;
using LocalProgression.Component;
using MTFO.API;

namespace LocalProgression
{
    public partial class LocalProgressionManager
    {
        public static readonly LocalProgressionManager Current = new();

        public static readonly string DirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GTFO-Modding", "LocalProgression");

        private RundownProgressionData CurrentRundownPData { get; } = new RundownProgressionData();

        internal RundownManager.RundownProgData nativeProgData { get; private set; } = default;

        private CM_PageRundown_New rundownPage = null;

        private static string RundownLPDataPath(string rundownName)
        {
            char[] invalidPathChars = Path.GetInvalidPathChars();

            foreach (char c in invalidPathChars)
            {
                rundownName = rundownName.Replace(c, '_');
            }

            return Path.Combine(DirPath, rundownName);
        }

        private Dictionary<string, ExpeditionProgressionData> ReadRundownLPData(string rundownName)
        {
            string filepath = RundownLPDataPath(rundownName);
            var dataDict = new Dictionary<string, ExpeditionProgressionData>();

            //LPLogger.Warning($"ReadRundownLPData - {rundownName}");

            if (File.Exists(filepath))
            {
                var stream = File.Open(filepath, FileMode.Open);
                var reader = new BinaryReader(stream, Encoding.UTF8, false);
                try
                {
                    int dataCount = reader.ReadInt32();
                    for (int cnt = 0; cnt < dataCount; cnt++)
                    {
                        ExpeditionProgressionData data = new ExpeditionProgressionData();
                        data.ExpeditionKey = reader.ReadString();
                        //LPLogger.Warning($"data.ExpeditionKey: {data.ExpeditionKey}");
                        data.MainCompletionCount = reader.ReadInt32();
                        data.SecondaryCompletionCount = reader.ReadInt32();
                        data.ThirdCompletionCount = reader.ReadInt32();
                        data.AllClearCount = reader.ReadInt32(); // will cause EndOfStreamException for old version
                        data.NoBoosterAllClearCount = reader.ReadInt32();
                        dataDict[data.ExpeditionKey] = data;
                    }
                }

                catch(EndOfStreamException e)
                {
                    // TODO: make a backup 

                    LPLogger.Warning("Upgrading data format to latest LocalProgression version!");
                    dataDict.Clear();
                    reader.Close();
                    stream.Close();
                    
                    stream = File.Open(filepath, FileMode.Open);
                    reader = new BinaryReader(stream, Encoding.UTF8, false);

                    int dataCount = reader.ReadInt32();
                    for (int cnt = 0; cnt < dataCount; cnt++)
                    {
                        ExpeditionProgressionData data = new ExpeditionProgressionData();
                        data.ExpeditionKey = reader.ReadString();
                        //LPLogger.Warning($"data.ExpeditionKey: {data.ExpeditionKey}");
                        data.MainCompletionCount = reader.ReadInt32();
                        data.SecondaryCompletionCount = reader.ReadInt32();
                        data.ThirdCompletionCount = reader.ReadInt32();
                        data.AllClearCount = reader.ReadInt32(); // TODO: load and save?
                        //data.NoBoosterAllClearCount = reader.ReadInt32();
                        dataDict[data.ExpeditionKey] = data;
                    }

                    reader.Close();
                    stream.Close();

                    File.Move(filepath, filepath + " - backup");

                    SaveRundownLPDataToDisk(rundownName, dataDict);
                }
                finally
                {
                    reader.Close();
                    stream.Close();
                }
            }

            return dataDict;
        }

        internal RundownProgressionData GetLPDataForCurrentRundown() => CurrentRundownPData;

        private void SaveRundownLPDataToDisk(string rundownName = "", Dictionary<string, ExpeditionProgressionData> dataDict = null)
        {
            if(rundownName.Equals(""))
            {
                rundownName = CurrentRundownPData.RundownName;
            }

            string filepath = RundownLPDataPath(rundownName);
            if(dataDict == null)
            {
                dataDict = CurrentRundownPData.LPData;
            }

            LPLogger.Warning($"SaveData: saving {rundownName} LPData to '{filepath}'");

            using (var stream = File.Open(filepath, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    writer.Write(dataDict.Count);
                    foreach (string expKey in dataDict.Keys)
                    {
                        ExpeditionProgressionData data = dataDict[expKey];
                        writer.Write(expKey);
                        writer.Write(data.MainCompletionCount);
                        writer.Write(data.SecondaryCompletionCount);
                        writer.Write(data.ThirdCompletionCount);
                        writer.Write(data.AllClearCount);
                        writer.Write(data.NoBoosterAllClearCount);
                    }
                }
            }
        }

        public void RecordExpeditionSuccessForCurrentRundown(string expeditionKey, bool mainLayerCleared, bool secondaryLayerCleared, bool thirdLayerCleared, bool clearedWithNoBooster)
        {
            if (RundownManager.ActiveExpedition.ExcludeFromProgression) return;

            bool allLayerCleared = mainLayerCleared && secondaryLayerCleared && thirdLayerCleared;
            var dataDict = CurrentRundownPData.LPData;

            if (!dataDict.ContainsKey(expeditionKey)) // first clear for this expedition
            {
                dataDict[expeditionKey] = new ExpeditionProgressionData()
                {
                    ExpeditionKey = expeditionKey,
                    MainCompletionCount = mainLayerCleared ? 1 : 0,
                    SecondaryCompletionCount = secondaryLayerCleared ? 1 : 0,
                    ThirdCompletionCount = thirdLayerCleared ? 1 : 0,
                    AllClearCount = allLayerCleared ? 1 : 0,
                    NoBoosterAllClearCount = clearedWithNoBooster ? 1 : 0
                };

                CurrentRundownPData.MainClearCount += mainLayerCleared ? 1 : 0;
                CurrentRundownPData.SecondaryClearCount += secondaryLayerCleared ? 1 : 0;
                CurrentRundownPData.ThirdClearCount += thirdLayerCleared ? 1 : 0;
                CurrentRundownPData.AllClearCount += allLayerCleared ? 1 : 0;
                CurrentRundownPData.NoBoosterAllClearCount += clearedWithNoBooster ? 1 : 0;
            }

            else
            {
                ExpeditionProgressionData progData = dataDict[expeditionKey];

                if (progData.MainCompletionCount == 0 && mainLayerCleared) CurrentRundownPData.MainClearCount += 1;
                if (progData.SecondaryCompletionCount == 0 && secondaryLayerCleared) CurrentRundownPData.SecondaryClearCount += 1;
                if (progData.ThirdCompletionCount == 0 && thirdLayerCleared) CurrentRundownPData.ThirdClearCount += 1;
                if (progData.AllClearCount == 0 && allLayerCleared) CurrentRundownPData.AllClearCount += 1;
                if (progData.NoBoosterAllClearCount == 0 && clearedWithNoBooster) CurrentRundownPData.NoBoosterAllClearCount += 1;

                progData.MainCompletionCount += mainLayerCleared ? 1 : 0;
                progData.SecondaryCompletionCount += secondaryLayerCleared ? 1 : 0;
                progData.ThirdCompletionCount += thirdLayerCleared ? 1 : 0;
                progData.AllClearCount += allLayerCleared ? 1 : 0;
                progData.NoBoosterAllClearCount += clearedWithNoBooster ? 1 : 0;
            }

            SaveRundownLPDataToDisk();
        }

        public void UpdateLPDataToActiveRundown()
        {
            uint rundownID = ActiveRundownID();
            if(rundownID == 0)
            {
                LPLogger.Debug($"UpdateLPDataToActiveRundown: cannot find any active rundown!");
                return;
            }

            LPLogger.Warning($"Update LPData to rundown_id: {rundownID}");
            CurrentRundownPData.Reset();

            RundownDataBlock rundownDB = GameDataBlockBase<RundownDataBlock>.GetBlock(rundownID);
            if (rundownDB == null)
            {
                LPLogger.Error($"Didn't find Rundown Datablock with rundown id {rundownID}");
                return;
            }

            var localProgressionDataDict = ReadRundownLPData(rundownDB.name);
            CurrentRundownPData.RundownID = rundownID;
            CurrentRundownPData.RundownName = rundownDB.name;
            CurrentRundownPData.LPData = localProgressionDataDict;

            foreach (var progressionData in localProgressionDataDict.Values)
            {
                CurrentRundownPData.MainClearCount += progressionData.MainCompletionCount > 0 ? 1 : 0;
                CurrentRundownPData.SecondaryClearCount += progressionData.SecondaryCompletionCount > 0 ? 1 : 0;
                CurrentRundownPData.ThirdClearCount += progressionData.ThirdCompletionCount > 0 ? 1 : 0;
                CurrentRundownPData.AllClearCount += progressionData.AllClearCount > 0 ? 1 : 0;
                CurrentRundownPData.NoBoosterAllClearCount += progressionData.NoBoosterAllClearCount > 0 ? 1 : 0;
            }
        }

        internal void Init()
        {
            if (!Directory.Exists(DirPath))
            {
                Directory.CreateDirectory(DirPath);
            }
            
            RundownManager.OnRundownProgressionUpdated += new Action(OnNativeRundownProgressionUpdated);

            InitConfig();
        }

        internal void OnNativeRundownProgressionUpdated()
        {
            UpdateLPDataToActiveRundown();

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

            uint rundownID = CurrentRundownPData.RundownID;
            if (rundownID == 0)
            {
                LPLogger.Warning($"UpdateRundownPageExpeditionIconProgression: rundown_id == 0!");
                return;
            }

            RundownDataBlock block = GameDataBlockBase<RundownDataBlock>.GetBlock(rundownID);
            LPLogger.Log($"CM_PageRundown_New.UpdateRundownExpeditionProgression, overwrite with LocalProgression Data, RundownID {CurrentRundownPData.RundownID}");
            
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
                var noIconSectorSummary = rundownPage.m_tierMarkerSectorSummary.GetComponent<RundownTierMarker_NoBoosterIcon>();
                if (noIconSectorSummary != null)
                {
                    if (TryGetRundownConfig(rundownID, out var conf))
                    {
                        int totalClearCount = conf.ComputeNoBoosterClearPossibleCount();
                        if(conf.EnableNoBoosterUsedProgressionForRundown || totalClearCount > 0)
                        {
                            if(conf.EnableNoBoosterUsedProgressionForRundown)
                            {
                                totalClearCount = nativeProgData.totalMain;
                            }

                            noIconSectorSummary.SetVisible(true);
                            noIconSectorSummary.SetSectorIconText($"<color=orange>[{CurrentRundownPData.NoBoosterAllClearCount}/{totalClearCount}]</color>");
                        }
                        else
                        {
                            noIconSectorSummary.SetVisible(false);
                        }
                    }
                }
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

                bool hasClearanceData = CurrentRundownPData.LPData.TryGetValue(progressionExpeditionKey, out var expeditionProgression);

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

            if(CurrentRundownPData.RundownID == 0)
            {
                LPLogger.Error($"ComputeLocalProgressionDataToRundownProgData: rundown_id == 0...");
                return rundownProgData;
            }

            RundownDataBlock block = GameDataBlockBase<RundownDataBlock>.GetBlock(CurrentRundownPData.RundownID);
            if (block == null)
            {
                LPLogger.Error($"ComputeLocalProgressionDataToRundownProgData: cannot get rundown datablock with rundown_id: {CurrentRundownPData.RundownID}");
                return rundownProgData;
            }

            rundownProgData.clearedMain = CurrentRundownPData.MainClearCount;
            rundownProgData.clearedSecondary = CurrentRundownPData.SecondaryClearCount;
            rundownProgData.clearedThird = CurrentRundownPData.ThirdClearCount;
            rundownProgData.clearedAllClear = CurrentRundownPData.AllClearCount;

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
                if (CurrentRundownPData.LPData.ContainsKey(expKey)) progressionData.clearedExtra++;

                index++;
            }
        }

        private bool CheckTierUnlocked(eRundownTier tier)
        {
            var rundownDB = RundownDataBlock.GetBlock(CurrentRundownPData.RundownID);
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

            return CurrentRundownPData.MainClearCount >= reqToReach.MainSectors
                && CurrentRundownPData.SecondaryClearCount >= reqToReach.SecondarySectors
                && CurrentRundownPData.ThirdClearCount >= reqToReach.ThirdSectors
                && CurrentRundownPData.AllClearCount >= reqToReach.AllClearedSectors;
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
                    return CurrentRundownPData.MainClearCount >= reqToReach.MainSectors
                        && CurrentRundownPData.SecondaryClearCount >= reqToReach.SecondarySectors
                        && CurrentRundownPData.ThirdClearCount >= reqToReach.ThirdSectors
                        && CurrentRundownPData.AllClearCount >= reqToReach.AllClearedSectors;

                case eExpeditionAccessibility.UnlockedByExpedition:
                    var req = expedition.UnlockedByExpedition;
                    string expeditionKey = RundownManager.GetRundownProgressionExpeditionKey(req.Tier, (int)req.Exp);

                    return CurrentRundownPData.LPData.ContainsKey(expeditionKey);

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
                LPLogger.Warning($"Non-empty native rundown progression data! RundownID: {CurrentRundownPData.RundownID}");
                rundownProgressionData.Clear();
            }

            var dataDict = CurrentRundownPData.LPData;
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

        public bool AllSectorCompletedWithoutBoosterAndCheckpoint()
        {
            bool hasSecondary = RundownManager.HasSecondaryLayer(RundownManager.ActiveExpedition);
            if (hasSecondary && WardenObjectiveManager.CurrentState.second_status != eWardenObjectiveStatus.WardenObjectiveItemSolved) return false;
                
            bool hasThird = RundownManager.HasThirdLayer(RundownManager.ActiveExpedition);
            if (hasThird && WardenObjectiveManager.CurrentState.second_status != eWardenObjectiveStatus.WardenObjectiveItemSolved) return false; 

            bool isClearedWithNoBosster = CheckpointManager.CheckpointUsage == 0;
            if (isClearedWithNoBosster)
            {
                foreach (var playerBoosterImplantState in BoosterImplantManager.Current.m_boosterPlayers)
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
            }

            return isClearedWithNoBosster;
        }

        public uint ActiveRundownID()
        {
            var rundownKey = RundownManager.ActiveRundownKey;

            if (!RundownManager.TryGetIdFromLocalRundownKey(rundownKey, out var rundownID) || rundownID == 0u)
            {
                return 0u;
            }

            return rundownID;
        }

        public string ExpeditionKey(eRundownTier tier, int expIndex) => RundownManager.GetRundownProgressionExpeditionKey(tier, expIndex);

        public ExpeditionProgressionData GetExpeditionLP(uint RundownID, eRundownTier tier, int expIndex)
        {
            Dictionary<string, ExpeditionProgressionData> localProgressionDataDict;
            var EmptyProgression = new ExpeditionProgressionData() { ExpeditionKey = ExpeditionKey(tier, expIndex) };

            if (CurrentRundownPData.RundownID != RundownID)
            {
                RundownDataBlock rundownDB = GameDataBlockBase<RundownDataBlock>.GetBlock(RundownID);
                if (rundownDB == null)
                {
                    LPLogger.Error($"Didn't find Rundown Datablock with rundown id {RundownID}");
                    
                    return EmptyProgression;
                }
                
                localProgressionDataDict = ReadRundownLPData(rundownDB.name);
            }
            else
            {
                localProgressionDataDict = CurrentRundownPData.LPData;
            }

            string expKey = ExpeditionKey(tier, expIndex);
            if (localProgressionDataDict.TryGetValue(expKey, out var data)) 
            {
                return data;
            }

            return EmptyProgression;
        }

        static LocalProgressionManager() {}

        private LocalProgressionManager() {}
    }
}
