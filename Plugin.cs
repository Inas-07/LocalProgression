using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Unity.IL2CPP;
using ChainedPuzzles;
using GTFO.API;
using ScanPosOverride.PuzzleOverrideData;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ScanPosOverride.JSON;
using GTFO.API.Utilities;


namespace ScanPosOverride
{
    [BepInPlugin("ScanPositionOverride", "ScanPositionOverride", "1.0.0")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MTFOUtil.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MTFOPartialDataUtil.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]

    internal sealed class Plugin : BasePlugin
    {
        private Action<byte, CP_Bioscan_Core> OnBioscan;
        private Action<byte, CP_Cluster_Core> OnClusterscan;

        // MainLevelLayout, List of puzzles to override
        private static Dictionary<uint, List<PuzzleOverride>> PuzzleOverrides = new();
        private static readonly string OVERRIDE_SCAN_POS_PATH = Path.Combine(MTFOUtil.CustomPath, "ScanPositionOverride");
        private static LiveEditListener listener = null;

        private uint ActiveExpedition => RundownManager.ActiveExpedition.LevelLayoutData;

        public override void Load()
        {
            Logger.Error(OVERRIDE_SCAN_POS_PATH);
            if (!Directory.Exists(OVERRIDE_SCAN_POS_PATH))
            {
                Logger.Error("Did not find ScanPositionOverride config folder, will not load.");
                return;
            }

            OnBioscan += new Action<byte, CP_Bioscan_Core>(MoveBio);
            OnBioscan += new Action<byte, CP_Bioscan_Core>(LogBio);
            OnClusterscan += new Action<byte, CP_Cluster_Core>(MoveCluster);
            OnClusterscan += new Action<byte, CP_Cluster_Core>(LogCluster);
            EventAPI.OnExpeditionStarted += new Action(OnExpeditionStarted);

            foreach (string config_file in Directory.EnumerateFiles(OVERRIDE_SCAN_POS_PATH, "*.json", SearchOption.AllDirectories))
            {
                PuzzleOverrideJsonFile puzzleOverrideConfig;
                Json.Load(config_file, out puzzleOverrideConfig);

                if (!PuzzleOverrides.ContainsKey(puzzleOverrideConfig.MainLevelLayout))
                {
                    PuzzleOverrides.Add(puzzleOverrideConfig.MainLevelLayout, puzzleOverrideConfig.Puzzles);
                }
                else
                {
                    Logger.Warning("Duplicate MainLevelLayout {0}.", puzzleOverrideConfig.MainLevelLayout);
                }
            }

            listener = LiveEdit.CreateListener(OVERRIDE_SCAN_POS_PATH, "*.json", includeSubDir: true);
            listener.FileChanged += LiveEdit_FileChanged;
        }

        private static void LiveEdit_FileChanged(LiveEditEventArgs e)
        {
            Logger.Warning($"LiveEdit File Changed: {e.FullPath}.");

            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                PuzzleOverrideJsonFile overrideConfig = Json.Deserialize<PuzzleOverrideJsonFile>(content);
                if(!PuzzleOverrides.ContainsKey(overrideConfig.MainLevelLayout))
                {
                    // could also allow replacing.
                    Logger.Warning("MainLevelLayout not found, which is now not supported. Will not replace.");
                    return;
                }

                PuzzleOverrides[overrideConfig.MainLevelLayout] = overrideConfig.Puzzles;
                Logger.Warning("Replaced Override Puzzle with MainLevelLayout {0}", overrideConfig.MainLevelLayout);
            });
        }

        private void OnExpeditionStarted()
        {
            byte num = 0;
            for (int index1 = 0; index1 < ChainedPuzzleManager.Current.m_instances.Count; ++index1)
            {
                Il2CppArrayBase<CP_Bioscan_Core> componentsInChildren1 = ChainedPuzzleManager.Current.m_instances[index1].m_parent.GetComponentsInChildren<CP_Bioscan_Core>();
                if (componentsInChildren1 != null)
                {
                    for (int index2 = 0; index2 < componentsInChildren1.Count; ++index2)
                    {
                        Action<byte, CP_Bioscan_Core> onBioscan = OnBioscan;
                        if (onBioscan != null)
                            onBioscan(num, componentsInChildren1[index2]);
                        ++num;
                    }
                }
                Il2CppArrayBase<CP_Cluster_Core> componentsInChildren2 = ChainedPuzzleManager.Current.m_instances[index1].m_parent.GetComponentsInChildren<CP_Cluster_Core>();
                if (componentsInChildren2 != null)
                {
                    for (int index3 = 0; index3 < componentsInChildren2.Count; ++index3)
                    {
                        Action<byte, CP_Cluster_Core> onClusterscan = OnClusterscan;
                        if (onClusterscan != null)
                            onClusterscan(num, componentsInChildren2[index3]);
                        ++num;
                    }
                }
            }
        }

        private PuzzleOverride GetModifaction(byte count)
        {
            List<PuzzleOverride> puzzleOverrideList;
            if (PuzzleOverrides.TryGetValue(ActiveExpedition, out puzzleOverrideList))
            {
                for (int index = 0; index < puzzleOverrideList.Count; ++index)
                {
                    if (puzzleOverrideList[index].Index == count)
                        return puzzleOverrideList[index];
                }
            }
            return null;
        }

        private void MoveBio(byte count, CP_Bioscan_Core scan)
        {
            PuzzleOverride modifaction = this.GetModifaction(count);
            if (modifaction == null)
                return;
            scan.gameObject.transform.position = modifaction.Position.ToVector3();
            scan.gameObject.transform.rotation = modifaction.Rotation.ToQuaternion();
            if (!scan.m_isMovable || modifaction.TPositions.Count <= 0)
                return;

            if(scan.m_movingComp.ScanPositions != null)
            {
                foreach(var pos in modifaction.TPositions)
                {
                    scan.m_movingComp.ScanPositions.Add(pos.ToVector3());
                }
            }
            else
            {
                Logger.Error("scan.m_movingComp.ScanPositions is null!!");
            }
        }

        private void MoveCluster(byte count, CP_Cluster_Core scan)
        {
            PuzzleOverride modifaction = this.GetModifaction(count);
            if (modifaction == null)
                return;
            scan.gameObject.transform.position = modifaction.Position.ToVector3();
            scan.gameObject.transform.rotation = modifaction.Rotation.ToQuaternion();
        }

        private void LogCluster(byte count, CP_Cluster_Core scan) => this.LogScan(count, scan.gameObject);

        private void LogBio(byte count, CP_Bioscan_Core scan)
        {
            LogScan(count, scan.gameObject);
            if (!scan.m_isMovable)
                return;
            Logger.Debug("move positions");
            for (int index = 0; index < scan.m_movingComp.ScanPositions.Count; ++index)
            {
                Vector3 scanPosition = scan.m_movingComp.ScanPositions[index];
                bool isEnabled;
                BepInExDebugLogInterpolatedStringHandler interpolatedStringHandler = new BepInExDebugLogInterpolatedStringHandler(2, 3, out isEnabled);
                if (isEnabled)
                {
                    interpolatedStringHandler.AppendFormatted(scanPosition.x);
                    interpolatedStringHandler.AppendLiteral(",");
                    interpolatedStringHandler.AppendFormatted(scanPosition.y);
                    interpolatedStringHandler.AppendLiteral(",");
                    interpolatedStringHandler.AppendFormatted(scanPosition.z);
                }
                BepInExDebugLogInterpolatedStringHandler logHandler = interpolatedStringHandler;
                Logger.Log(logHandler);
            }
        }

        private void LogScan(byte count, GameObject scan)
        {
            Vector3 position = scan.transform.position;
            Vector3 eulerAngles = scan.transform.rotation.ToEulerAngles();
            bool isEnabled;
            BepInExDebugLogInterpolatedStringHandler interpolatedStringHandler = new BepInExDebugLogInterpolatedStringHandler(34, 9, out isEnabled);
            if (isEnabled)
            {
                interpolatedStringHandler.AppendLiteral("layout:");
                interpolatedStringHandler.AppendFormatted(ActiveExpedition);
                interpolatedStringHandler.AppendLiteral(" index:");
                interpolatedStringHandler.AppendFormatted(count);
                interpolatedStringHandler.AppendLiteral(" pos:");
                interpolatedStringHandler.AppendFormatted(position.x);
                interpolatedStringHandler.AppendLiteral(",");
                interpolatedStringHandler.AppendFormatted(position.y);
                interpolatedStringHandler.AppendLiteral(",");
                interpolatedStringHandler.AppendFormatted(position.z);
                interpolatedStringHandler.AppendLiteral(" rot:");
                interpolatedStringHandler.AppendFormatted(eulerAngles.x);
                interpolatedStringHandler.AppendLiteral(",");
                interpolatedStringHandler.AppendFormatted(eulerAngles.y);
                interpolatedStringHandler.AppendLiteral(",");
                interpolatedStringHandler.AppendFormatted(eulerAngles.z);
                interpolatedStringHandler.AppendLiteral(" name:");
                interpolatedStringHandler.AppendFormatted(scan.name);
            }
            BepInExDebugLogInterpolatedStringHandler logHandler = interpolatedStringHandler;
            Logger.Log(logHandler);
        }

    }
}
