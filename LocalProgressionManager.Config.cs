using System.IO;
using LocalProgression.Data;
using MTFO.API;

namespace LocalProgression
{
    public partial class LocalProgressionManager
    {
        public string LP_CONFIG_DIR => Path.Combine(MTFOPathAPI.CustomPath, "LocalProgressionConfig");

        public const string CONFIG_FILE_NAME = "ProgressionConfig.json";

        public string CONFIG_PATH => Path.Combine(LP_CONFIG_DIR, CONFIG_FILE_NAME);

        public RundownProgressonConfig RundownProgressonConfig { get; private set; } = new();

        private void InitConfig()
        {
            if(!Directory.Exists(LP_CONFIG_DIR)) 
            {
                Directory.CreateDirectory(LP_CONFIG_DIR);
            }

            if(!File.Exists(CONFIG_PATH))
            {
                var file = File.CreateText(CONFIG_PATH);
                file.WriteLine(JSON.Serialize(new RundownProgressonConfig()));
                file.Flush();
                file.Close();

                RundownProgressonConfig = new();
            }
            
            ReloadConfig();
            RundownManager.OnRundownProgressionUpdated += new System.Action(ReloadConfig);
        }

        private void ReloadConfig()
        {
            try
            {
                RundownProgressonConfig = JSON.Deserialize<RundownProgressonConfig>(File.ReadAllText(CONFIG_PATH));
            }
            catch
            {
                LPLogger.Error("Cannot reload RundownProgressonConfig, probably the file is invalid");
                RundownProgressonConfig = new();
            }
        }

        public bool TryGetRundownConfig(uint RundownID, out RundownConfig rundownConf)
        {
            rundownConf = RundownProgressonConfig.Configs.Find(rundownConf => rundownConf.RundownID == RundownID);
            return rundownConf != null;
        }


        public bool TryGetExpeditionConfig(uint RundownID, eRundownTier tier, int expIndex, out ExpeditionProgressionConfig expConf)
        {
            expConf = null;
            if(TryGetRundownConfig(RundownID, out var rundownConf))
            {
                expConf = rundownConf.Expeditions.Find(e => e.Tier == tier && e.ExpeditionIndex == expIndex);
            }
            return expConf != null;
        }
    }
}