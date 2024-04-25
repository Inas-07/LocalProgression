using System.Collections.Generic;
using System.Linq;

namespace LocalProgression.Data
{
    public class ExpeditionProgressionConfig
    {
        public eRundownTier Tier { get; set; }

        public int ExpeditionIndex { get; set; }

        public bool EnableNoBoosterUsedProgression { get; set; } = false;
    }

    public class RundownConfig 
    {
        public uint RundownID { get; set; } = 0u;

        public bool EnableNoBoosterUsedProgressionForRundown { get; set; } = false;

        public List<ExpeditionProgressionConfig> Expeditions { get; set; } = new() { new() };

        internal int ComputeNoBoosterClearPossibleCount()
        {
            if (EnableNoBoosterUsedProgressionForRundown)
            {
                return int.MaxValue;
            }

            return Expeditions.TakeWhile(conf => conf.EnableNoBoosterUsedProgression == true).Count();
        }
    }

    public class RundownProgressonConfig
    {
        public List<RundownConfig> Configs { get; set; } = new() { new() };
    }
}
