using System.Collections.Generic;

namespace LocalProgression.Data
{
    public class RundownProgressionData
    {
        // rundownName would be used as the file name of the local progression storage
        public string RundownName { set; get; } = string.Empty;

        public uint RundownID { set; get; } = 0u;

        public Dictionary<string, ExpeditionProgressionData> LocalProgressionDict { set; get; } = new();

        public int MainClearCount { set; get; } = 0;

        public int SecondaryClearCount { set; get; } = 0;

        public int ThirdClearCount { set; get; } = 0;

        public int AllClearCount { set; get; } = 0;

        public void Reset()
        {
            RundownName = string.Empty;
            RundownID = 0;
            LocalProgressionDict.Clear();
            MainClearCount = SecondaryClearCount = ThirdClearCount = AllClearCount = 0;
        }
    }
}
