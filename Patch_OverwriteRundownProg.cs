using HarmonyLib;
using DropServer;
using GameData;
using Globals;
using System.IO;
using System;
using System.Text;
using System.Collections.Generic;
using CellMenu;
using Localization;

namespace LocalProgression
{
    [HarmonyPatch]
    internal class Patch_LocalRundownProgression
    {
        private static readonly string DirPath
            = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GTFO-Modding", "LocalProgression");

        // Local progression data holder.
        private static LocalProgressionData localProgressionData = new LocalProgressionData();

        private static string LocalProgressionStorePath() => Path.Combine(DirPath, localProgressionData.rundownName);

        private static void StoreLocalProgDict()
        {
            if (localProgressionData.localProgDict == null)
            {
                Logger.Error(nameof(Patch_LocalRundownProgression), "Critical: localProgData.LocalProgDict is null!");
                return;
            }

            foreach (string ExpKey in localProgressionData.localProgDict.Keys)
            {
                LocalExpProgData localExpProgData;
                if (localProgressionData.localProgDict.TryGetValue(ExpKey, out localExpProgData) == false)
                {
                    continue;
                }
            }

            using (var stream = File.Open(LocalProgressionStorePath(), FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    writer.Write(localProgressionData.localProgDict.Count);
                    foreach (string expKey in localProgressionData.localProgDict.Keys)
                    {
                        LocalExpProgData data;
                        localProgressionData.localProgDict.TryGetValue(expKey, out data);
                        writer.Write(expKey);
                        writer.Write(data.mainCompletionCount);
                        writer.Write(data.secondaryCompletionCount);
                        writer.Write(data.thirdCompletionCount);
                        writer.Write(data.allClearCount);
                    }
                }
            }
        }

        private static void checkAndInitialize()
        {
            if (localProgressionData.rundownName != "") return; // already initialized.

            if (!Directory.Exists(DirPath))
            {
                Directory.CreateDirectory(DirPath);
            }

            // Set Rundown Identifier: localProgressionData.rundownName
            {
                var RundownIdsToLoad = GameSetupDataBlock.GetBlock(1).RundownIdsToLoad;
                if (RundownIdsToLoad == null || RundownIdsToLoad.Count < 1)
                {
                    Logger.Error("Could not find any rundown id in GameSetupDatablock, using Rundown_id: 1");
                    localProgressionData.rundownId = 1;
                }
                else 
                {
                    if (RundownIdsToLoad.Count > 1)
                        Logger.Warning("Multiple Rundown_id found in GameSetupDatablock, using the first Rundown_id: {0}", RundownIdsToLoad[0]);
                    
                    localProgressionData.rundownId = RundownIdsToLoad[0];
                }

                var rundownDB = RundownDataBlock.GetBlock(localProgressionData.rundownId);
                if(rundownDB == null)
                {
                    Logger.Warning("Procrastinating initialization.");
                    localProgressionData.rundownName = "";
                    return;
                }

                localProgressionData.rundownName = RundownDataBlock.GetBlock(localProgressionData.rundownId).name;

                char[] invalidPathChars = Path.GetInvalidPathChars();

                foreach (char c in invalidPathChars)
                {
                    localProgressionData.rundownName.Replace(c, '_');
                }
            }


            string filePath = LocalProgressionStorePath();

            //APILogger.Verbose(nameof(Patch_LocalRundownProgression), string.Format("Get current rundown name: {0}", RUNDOWN_IDENTIFIER));
            //APILogger.Verbose(nameof(Patch_LocalRundownProgression), string.Format("FilePath: {0}", LocalProgressionStorePath()));
            //Logger.Verbose(nameof(Patch_LocalRundownProgression), string.Format("Get current rundown name: {0}", RUNDOWN_IDENTIFIER));
            //Logger.Verbose(nameof(Patch_LocalRundownProgression), string.Format("FilePath: {0}", LocalProgressionStorePath()));

            localProgressionData.localProgDict = new Dictionary<string, LocalExpProgData>();

            if (File.Exists(filePath))
            {
                using (var stream = File.Open(LocalProgressionStorePath(), FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                    {
                        int LocalProgDict_Count = 0;
                        LocalProgDict_Count = reader.ReadInt32();
                        for (int cnt = 0; cnt < LocalProgDict_Count; cnt++)
                        {
                            LocalExpProgData data = new LocalExpProgData();
                            data.expeditionKey = reader.ReadString();
                            data.mainCompletionCount = reader.ReadInt32();
                            data.secondaryCompletionCount = reader.ReadInt32();
                            data.thirdCompletionCount = reader.ReadInt32();
                            data.allClearCount = reader.ReadInt32();
                            localProgressionData.localProgDict.Add(data.expeditionKey, data);

                            localProgressionData.mainClearCount += data.mainCompletionCount > 0 ? 1 : 0;
                            localProgressionData.secondaryClearCount += data.secondaryCompletionCount > 0 ? 1 : 0;
                            localProgressionData.thirdClearCount += data.thirdCompletionCount > 0 ? 1 : 0;
                            localProgressionData.allClearCount += data.allClearCount > 0 ? 1 : 0;
                        }
                    }
                }
            }
        }

        private static bool checkTierUnlocked(eRundownTier tier)
        {
            var rundownDB = RundownDataBlock.GetBlock(localProgressionData.rundownId);
            RundownTierProgressionData reqToReach = null;
            switch (tier)
            {
                case eRundownTier.TierA: return true;
                case eRundownTier.TierB: reqToReach = rundownDB.ReqToReachTierB; break;
                case eRundownTier.TierC: reqToReach = rundownDB.ReqToReachTierC; break;
                case eRundownTier.TierD: reqToReach = rundownDB.ReqToReachTierD; break;
                case eRundownTier.TierE: reqToReach = rundownDB.ReqToReachTierE; break;
                default: Logger.Error("Unsupporrted tier: {0}", tier); return true;
            }

            if (localProgressionData.mainClearCount < reqToReach.MainSectors
                || localProgressionData.secondaryClearCount < reqToReach.SecondarySectors
                || localProgressionData.thirdClearCount < reqToReach.ThirdSectors
                || localProgressionData.allClearCount < reqToReach.AllClearedSectors) return false;
            return true;
        }

        private static bool checkExpeditionUnlocked(ExpeditionInTierData expedition, eRundownTier tier)
        {
            
            if (localProgressionData.localProgDict == null)
            {
                //APILogger.Error(nameof(Patch_LocalRundownProgression), "Critical: localRundownProgData is uninitialized.");
                Logger.Error("Critical: localRundownProgData is uninitialized.");
                return false;
            }
           

            switch (expedition.Accessibility)
            {
                case eExpeditionAccessibility.AlwayBlock:
                case eExpeditionAccessibility.BlockedAndScrambled: 
                    return false;
                case eExpeditionAccessibility.AlwaysAllow: return true;
                case eExpeditionAccessibility.Normal: // depend on 'ReqToReachTierC' and all that
                    return checkTierUnlocked(tier);

                case eExpeditionAccessibility.UseCustomProgressionLock:
                    RundownTierProgressionData reqToReach = expedition.CustomProgressionLock;
                    if (localProgressionData.mainClearCount < reqToReach.MainSectors
                        || localProgressionData.secondaryClearCount < reqToReach.SecondarySectors
                        || localProgressionData.thirdClearCount < reqToReach.ThirdSectors
                        || localProgressionData.allClearCount < reqToReach.AllClearedSectors) return false;

                    return true;

                case eExpeditionAccessibility.UnlockedByExpedition:
                    var req = expedition.UnlockedByExpedition;
                    string expeditionKey = RundownManager.GetRundownProgressionExpeditionKey(req.Tier, (int)req.Exp);
                    if(!localProgressionData.localProgDict.ContainsKey(expeditionKey)) return false;
                    return true;
                
                default:
                    Logger.Warning("Unsupported eExpeditionAccessibility: {0}", expedition.Accessibility);
                    return true; // return true anyway
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GS_ExpeditionSuccess), nameof(GS_ExpeditionSuccess.Enter))]
        private static void DoChangeState(GS_ExpeditionSuccess __instance)
        {
            if (RundownManager.ActiveExpedition.ExcludeFromProgression) return;


            checkAndInitialize();

            // ----------------------
            // store prog data on my side
            // ----------------------
            var CompletedExpedtionData = RundownManager.GetActiveExpeditionData();

            var progData = new LocalExpProgData()
            {
                expeditionKey = RundownManager.GetRundownProgressionExpeditionKey(CompletedExpedtionData.tier, CompletedExpedtionData.expeditionIndex),
                mainCompletionCount = WardenObjectiveManager.CurrentState.main_status == eWardenObjectiveStatus.WardenObjectiveItemSolved ? 1 : 0,
                secondaryCompletionCount = WardenObjectiveManager.CurrentState.second_status == eWardenObjectiveStatus.WardenObjectiveItemSolved ? 1 : 0,
                thirdCompletionCount = WardenObjectiveManager.CurrentState.third_status == eWardenObjectiveStatus.WardenObjectiveItemSolved ? 1 : 0
            };

            if (progData.mainCompletionCount == 1 && progData.secondaryCompletionCount == 1 && progData.thirdCompletionCount == 1)
            {
                progData.allClearCount = 1;
            }

            if (localProgressionData.localProgDict.ContainsKey(progData.expeditionKey))
            {
                LocalExpProgData PreviousProgData;
                if (localProgressionData.localProgDict.TryGetValue(progData.expeditionKey, out PreviousProgData) == false)
                {
                    return;
                }

                localProgressionData.localProgDict.Remove(progData.expeditionKey);
                progData.mainCompletionCount += PreviousProgData.mainCompletionCount;
                progData.secondaryCompletionCount += PreviousProgData.secondaryCompletionCount;
                progData.thirdCompletionCount += PreviousProgData.thirdCompletionCount;
                progData.allClearCount += PreviousProgData.allClearCount;
                localProgressionData.localProgDict.Add(progData.expeditionKey, progData);

                if (PreviousProgData.mainCompletionCount == 0 && progData.mainCompletionCount == 1) localProgressionData.mainClearCount += 1;
                if (PreviousProgData.secondaryCompletionCount == 0 && progData.secondaryCompletionCount == 1) localProgressionData.secondaryClearCount += 1;
                if (PreviousProgData.thirdCompletionCount == 0 && progData.thirdCompletionCount == 1) localProgressionData.thirdClearCount += 1;
                if (PreviousProgData.allClearCount == 0 && progData.allClearCount == 1) localProgressionData.allClearCount += 1;
            }
            else
            {
                localProgressionData.localProgDict.Add(progData.expeditionKey, progData);

                localProgressionData.mainClearCount += progData.mainCompletionCount > 0 ? 1 : 0;
                localProgressionData.secondaryClearCount += progData.secondaryCompletionCount > 0 ? 1 : 0;
                localProgressionData.thirdClearCount += progData.thirdCompletionCount > 0 ? 1 : 0;
                localProgressionData.allClearCount += progData.allClearCount > 0 ? 1 : 0;
            }

            StoreLocalProgDict();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_PageRundown_New), nameof(CM_PageRundown_New.UpdateTierIconsWithProgression))]
        private static void Post_UpdateTierIconsWithProgression(CM_PageRundown_New __instance,
            RundownProgression progression,
            RundownManager.RundownProgData progData,
            Il2CppSystem.Collections.Generic.List<CM_ExpeditionIcon_New> tierIcons,
            CM_RundownTierMarker tierMarker,
            bool thisTierUnlocked)
        {
            //if (progression == null) return;

            /* 
             *  null check 
            */
            //Logger.Warning("CM_PageRundown_New __instance == null ? {0}", __instance == null);
            //Logger.Warning("RundownManager.RundownProgData progData == null ? {0}", progData);
            //Logger.Warning("List<CM_ExpeditionIcon_New> tierIcons == null ? {0}", tierIcons == null);
            //Logger.Warning("CM_RundownTierMarker tierMarker == null ? {0}", tierMarker == null);

            checkAndInitialize();

            // ----------------------------------------
            // original method rewrite
            // ----------------------------------------

            if (tierIcons == null || tierIcons.Count == 0)
            {
                if(tierMarker != null)
                {
                    tierMarker.SetVisible(false);
                }
            }
            else
            {
                int num = 0;
                bool allowFullRundown = Global.AllowFullRundown;
                for (int index = 0; index < tierIcons.Count; ++index)
                {
                    CM_ExpeditionIcon_New tierIcon = tierIcons[index];
                    string progressionExpeditionKey = RundownManager.GetRundownProgressionExpeditionKey(tierIcons[index].Tier, tierIcons[index].ExpIndex);
                    LocalExpProgData localExpProgData;
                    bool flag1 = localProgressionData.localProgDict.TryGetValue(progressionExpeditionKey, out localExpProgData); 

                    string mainFinishCount = "0";
                    string secondFinishCount = RundownManager.HasSecondaryLayer(tierIcons[index].DataBlock) ? "0" : "-";
                    string thirdFinishCount = RundownManager.HasThirdLayer(tierIcons[index].DataBlock) ? "0" : "-";
                    string allFinishedCount = RundownManager.HasAllCompletetionPossibility(tierIcons[index].DataBlock) ? "0" : "-";

                    if (localExpProgData.mainCompletionCount > 0)
                        mainFinishCount = localExpProgData.mainCompletionCount.ToString();
                    if (localExpProgData.secondaryCompletionCount > 0)
                        secondFinishCount = localExpProgData.secondaryCompletionCount.ToString();
                    if (localExpProgData.thirdCompletionCount > 0)
                        thirdFinishCount = localExpProgData.thirdCompletionCount.ToString();
                    if (localExpProgData.allClearCount > 0)
                        allFinishedCount = localExpProgData.allClearCount.ToString();

                    //bool expUnlocked = RundownManager.CheckExpeditionUnlocked(tierIcon.DataBlock, tierIcon.Tier, progData);
                    bool expUnlocked = checkExpeditionUnlocked(tierIcon.DataBlock, tierIcon.Tier);
                    if (allowFullRundown | thisTierUnlocked | expUnlocked)
                    {
                        if (allowFullRundown | flag1)
                        {
                            if (allowFullRundown || localExpProgData.mainCompletionCount > 0)
                            {
                                __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.PlayedAndFinished, mainFinishCount, secondFinishCount, thirdFinishCount, allFinishedCount);
                                ++num;
                            }
                            //else if (prog.Layers.Main.State >= LayerProgressionState.Entered)
                            //    __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.PlayedNotFinished);
                            else
                                __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.NotPlayed);
                        }
                        else
                            __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.NotPlayed);
                    }
                    else if (flag1 && localExpProgData.mainCompletionCount > 0)
                        __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.TierLockedFinishedAnyway, mainFinishCount, secondFinishCount, thirdFinishCount, allFinishedCount);
                    else
                        __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.TierLocked);
                }

                if (allowFullRundown | thisTierUnlocked)
                    tierMarker.SetStatus(eRundownTierMarkerStatus.Unlocked);
                else
                    tierMarker.SetStatus(eRundownTierMarkerStatus.Locked);
            }
        }

        private static RundownManager.RundownProgData RecomputeRundownProgData()
        {
            RundownManager.RundownProgData rundownProgData = new RundownManager.RundownProgData();
            //if (localProgressionData.localProgDict == null) return rundownProgData;

            RundownDataBlock block = GameDataBlockBase<RundownDataBlock>.GetBlock(localProgressionData.rundownId);
            if (block == null)
                return rundownProgData;

            //foreach (var localExpData in localProgressionData.localProgDict.Values)
            //{
            //    if (localExpData.mainCompletionCount > 0) rundownProgData.clearedMain++;
            //    if (localExpData.secondaryCompletionCount > 0) rundownProgData.clearedSecondary++;
            //    if (localExpData.thirdCompletionCount > 0) rundownProgData.clearedThird++;
            //    if (localExpData.allClearCount > 0) rundownProgData.clearedAllClear++;
            //}

            rundownProgData.clearedMain = localProgressionData.mainClearCount;
            rundownProgData.clearedSecondary = localProgressionData.secondaryClearCount;
            rundownProgData.clearedThird = localProgressionData.thirdClearCount;
            rundownProgData.clearedAllClear = localProgressionData.allClearCount;

            int index = 0;
            eRundownTier tier = eRundownTier.TierA;
            foreach (var exp in block.TierA)
            {
                if (exp.Enabled == false) 
                    continue;

                rundownProgData.totalMain++;
                if (RundownManager.HasSecondaryLayer(exp)) rundownProgData.totalSecondary++;
                if (RundownManager.HasThirdLayer(exp)) rundownProgData.totalThird++;
                if (RundownManager.HasAllCompletetionPossibility(exp)) rundownProgData.totalAllClear++;
                if (exp.Descriptive.IsExtraExpedition) rundownProgData.totatlExtra++;
                string expKey = RundownManager.GetUniqueExpeditionKey(block, tier, index);
                if (localProgressionData.localProgDict.ContainsKey(expKey)) rundownProgData.clearedExtra++;

                index++;
            }

            index = 0;
            tier = eRundownTier.TierB;
            foreach (var exp in block.TierB)
            {
                if (exp.Enabled == false)
                    continue;

                rundownProgData.totalMain++;
                if (RundownManager.HasSecondaryLayer(exp)) rundownProgData.totalSecondary++;
                if (RundownManager.HasThirdLayer(exp)) rundownProgData.totalThird++;
                if (RundownManager.HasAllCompletetionPossibility(exp)) rundownProgData.totalAllClear++;
                if (exp.Descriptive.IsExtraExpedition) rundownProgData.totatlExtra++;
                string expKey = RundownManager.GetUniqueExpeditionKey(block, tier, index);
                if (localProgressionData.localProgDict.ContainsKey(expKey)) rundownProgData.clearedExtra++;

                index++;
            }

            index = 0;
            tier = eRundownTier.TierC;
            foreach (var exp in block.TierC)
            {
                if (exp.Enabled == false)
                    continue;

                rundownProgData.totalMain++;
                if (RundownManager.HasSecondaryLayer(exp)) rundownProgData.totalSecondary++;
                if (RundownManager.HasThirdLayer(exp)) rundownProgData.totalThird++;
                if (RundownManager.HasAllCompletetionPossibility(exp)) rundownProgData.totalAllClear++;
                if (exp.Descriptive.IsExtraExpedition) rundownProgData.totatlExtra++;
                string expKey = RundownManager.GetUniqueExpeditionKey(block, tier, index);
                if (localProgressionData.localProgDict.ContainsKey(expKey)) rundownProgData.clearedExtra++;

                index++;
            }

            index = 0;
            tier = eRundownTier.TierD;
            foreach (var exp in block.TierD)
            {
                if (exp.Enabled == false)
                    continue;

                rundownProgData.totalMain++;
                if (RundownManager.HasSecondaryLayer(exp)) rundownProgData.totalSecondary++;
                if (RundownManager.HasThirdLayer(exp)) rundownProgData.totalThird++;
                if (RundownManager.HasAllCompletetionPossibility(exp)) rundownProgData.totalAllClear++;
                if (exp.Descriptive.IsExtraExpedition) rundownProgData.totatlExtra++;
                string expKey = RundownManager.GetUniqueExpeditionKey(block, tier, index);
                if (localProgressionData.localProgDict.ContainsKey(expKey)) rundownProgData.clearedExtra++;

                index++;
            }

            index = 0;
            tier = eRundownTier.TierE;
            foreach (var exp in block.TierE)
            {
                if (exp.Enabled == false)
                    continue;

                rundownProgData.totalMain++;
                if (RundownManager.HasSecondaryLayer(exp)) rundownProgData.totalSecondary++;
                if (RundownManager.HasThirdLayer(exp)) rundownProgData.totalThird++;
                if (RundownManager.HasAllCompletetionPossibility(exp)) rundownProgData.totalAllClear++;
                if (exp.Descriptive.IsExtraExpedition) rundownProgData.totatlExtra++;
                string expKey = RundownManager.GetUniqueExpeditionKey(block, tier, index);
                if (localProgressionData.localProgDict.ContainsKey(expKey)) rundownProgData.clearedExtra++;

                index++;
            }

            checkTierUnlocked(eRundownTier.TierB);
            checkTierUnlocked(eRundownTier.TierC);
            checkTierUnlocked(eRundownTier.TierD);
            checkTierUnlocked(eRundownTier.TierE);
            return rundownProgData;
        }

        // rewrite CM_RundownTierMarker.SetProgression
        //private static bool setProgression(CM_RundownTierMarker tierMarker, RundownTierProgressionData tierReq)
        //{
        //    bool flag1 = localProgressionData.mainClearCount >= tierReq.MainSectors;
        //    bool flag2 = localProgressionData.secondaryClearCount >= tierReq.SecondarySectors;
        //    bool flag3 = localProgressionData.thirdClearCount >= tierReq.ThirdSectors;
        //    bool flag4 = flag1 & flag2 & flag3;
        //    string str1 = "#FF000033";
        //    string str2 = "#2a543588";
        //    string str3 = flag4 ? "100%" : "100%";
        //    if (flag4)
        //        tierMarker.m_progression = "<size=" + str3 + ">" + Text.Get(19U) + "</size>";
        //    else
        //        tierMarker.m_progression = "<size=" + str3 + "><color=" + str1 + ">" + Text.Get(20U) + "</color> <size=110%>" + Text.Get(21U) + "</size></color></size>";
        //    // 下面这几行代码完全没有用，绝了
        //    tierMarker.m_sectorIconSummaryMain.SetVisible(true);
        //    tierMarker.m_sectorIconSummarySecondary.SetVisible(true);
        //    tierMarker.m_sectorIconSummaryThird.SetVisible(true);
        //    tierMarker.m_sectorIconSummaryAllClear.SetVisible(false);
        //    tierMarker.SetSectorIconTextForMain(tierReq.MainSectors.ToString(), flag1 ? str2 : str1);
        //            tierMarker.SetSectorIconTextForSecondary(tierReq.SecondarySectors.ToString(), flag2 ? str2 : str1);
        //            tierMarker.SetSectorIconTextForThird(tierReq.ThirdSectors.ToString(), flag3 ? str2 : str1);
        //            tierMarker.SetSectorIconTextForAllCleared(tierReq.AllClearedSectors.ToString(), flag4 ? str2 : str1);
            
        //    //if (flag4)
        //    //{
        //    //    tierMarker.m_sectorIconSummaryMain.SetVisible(false);
        //    //    tierMarker.m_sectorIconSummarySecondary.SetVisible(false);
        //    //    tierMarker.m_sectorIconSummaryThird.SetVisible(false);
        //    //    tierMarker.m_sectorIconSummaryAllClear.SetVisible(false);
        //    //}
        //    //else
        //    //{
        //    //    if (tierReq.MainSectors > 0)
        //    //        tierMarker.SetSectorIconTextForMain(tierReq.MainSectors.ToString(), flag1 ? str2 : str1);
        //    //    else
        //    //        tierMarker.m_sectorIconSummaryMain.SetVisible(false);

        //    //    if (tierReq.SecondarySectors > 0)
        //    //        tierMarker.SetSectorIconTextForSecondary(tierReq.SecondarySectors.ToString(), flag2 ? str2 : str1);
        //    //    else
        //    //        tierMarker.m_sectorIconSummarySecondary.SetVisible(false);

        //    //    if (tierReq.ThirdSectors > 0)
        //    //        tierMarker.SetSectorIconTextForThird(tierReq.ThirdSectors.ToString(), flag3 ? str2 : str1);
        //    //    else
        //    //        tierMarker.m_sectorIconSummaryThird.SetVisible(false);

        //    //    if (tierReq.AllClearedSectors > 0)
        //    //        tierMarker.SetSectorIconTextForAllCleared(tierReq.AllClearedSectors.ToString(), flag4 ? str2 : str1);
        //    //    else
        //    //        tierMarker.m_sectorIconSummaryAllClear.SetVisible(false);
        //    //}
        //    tierMarker.UpdateHeader();
        //    return flag4;
        //}

        // method rewrite.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CM_PageRundown_New), nameof(CM_PageRundown_New.UpdateExpeditionIconProgression))]
        private static bool Pre_UpdateExpeditionIconProgression(CM_PageRundown_New __instance)
        {
            checkAndInitialize();

            RundownDataBlock rundownDB = GameDataBlockBase<RundownDataBlock>.GetBlock(localProgressionData.rundownId);
            if (rundownDB == null) return true;
            RundownManager.RundownProgData rundownProgData1 = RecomputeRundownProgData();

            //UnityEngine.Debug.Log("CM_PageRundown_New.UpdateRundownExpeditionProgression, RundownManager.RundownProgressionReady: " + RundownManager.RundownProgressionReady.ToString());
            RundownProgression rundownProgression = RundownManager.RundownProgression;

            // modified line
            RundownManager.RundownProgData rundownProgData2 = RecomputeRundownProgData();

            if (__instance.m_tierMarkerSectorSummary != null)
            {
                __instance.m_tierMarkerSectorSummary.SetSectorIconTextForMain(rundownProgData2.clearedMain + "<size=50%><color=#FFFFFF33><size=55%>/" + rundownProgData2.totalMain + "</color></size>");
                __instance.m_tierMarkerSectorSummary.SetSectorIconTextForSecondary(rundownProgData2.clearedSecondary + "<size=50%><color=#FFFFFF33><size=55%>/" + rundownProgData2.totalSecondary + "</color></size>");
                __instance.m_tierMarkerSectorSummary.SetSectorIconTextForThird(rundownProgData2.clearedThird + "<size=50%><color=#FFFFFF33><size=55%>/" + rundownProgData2.totalThird + "</color></size>");
                __instance.m_tierMarkerSectorSummary.SetSectorIconTextForAllCleared(rundownProgData2.clearedAllClear + "<size=50%><color=#FFFFFF33><size=55%>/" + rundownProgData2.totalAllClear + "</color></size>");
            }
            if (!(__instance.m_tierMarker1 != null))
                return false;
            __instance.m_tierMarker1.SetProgression(rundownProgData2, new RundownTierProgressionData());
            //setProgression(__instance.m_tierMarker1, new RundownTierProgressionData());
            __instance.UpdateTierIconsWithProgression(rundownProgression, rundownProgData2, __instance.m_expIconsTier1, __instance.m_tierMarker1, true);

            __instance.m_tierMarker2.SetProgression(rundownProgData2, __instance.m_currentRundownData.ReqToReachTierB);
            //setProgression(__instance.m_tierMarker2, rundownDB.ReqToReachTierB);
            __instance.UpdateTierIconsWithProgression(rundownProgression, rundownProgData2, __instance.m_expIconsTier2, __instance.m_tierMarker2, rundownProgData2.tierBUnlocked && __instance.m_currentRundownData.UseTierUnlockRequirements);

            __instance.m_tierMarker3.SetProgression(rundownProgData2, __instance.m_currentRundownData.ReqToReachTierC);
            //setProgression(__instance.m_tierMarker3, rundownDB.ReqToReachTierC);
            __instance.UpdateTierIconsWithProgression(rundownProgression, rundownProgData2, __instance.m_expIconsTier3, __instance.m_tierMarker3, rundownProgData2.tierCUnlocked && __instance.m_currentRundownData.UseTierUnlockRequirements);

            __instance.m_tierMarker4.SetProgression(rundownProgData2, __instance.m_currentRundownData.ReqToReachTierD);
            //setProgression(__instance.m_tierMarker4, rundownDB.ReqToReachTierD);
            __instance.UpdateTierIconsWithProgression(rundownProgression, rundownProgData2, __instance.m_expIconsTier4, __instance.m_tierMarker4, rundownProgData2.tierDUnlocked && __instance.m_currentRundownData.UseTierUnlockRequirements);

            __instance.m_tierMarker5.SetProgression(rundownProgData2, __instance.m_currentRundownData.ReqToReachTierE);
            //setProgression(__instance.m_tierMarker5, rundownDB.ReqToReachTierE);
            __instance.UpdateTierIconsWithProgression(rundownProgression, rundownProgData2, __instance.m_expIconsTier5, __instance.m_tierMarker5, rundownProgData2.tierEUnlocked && __instance.m_currentRundownData.UseTierUnlockRequirements);

            return false;
        }

        

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Global), nameof(Global.OnApplicationQuit))]
        private static void Post_OnApplicationQuit()
        {
            localProgressionData = null;
        }
    }
}