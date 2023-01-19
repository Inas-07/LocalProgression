using System.Collections.Generic;

namespace LocalProgression
{
    public struct LocalExpProgData
    {
        public string expeditionKey { set; get; } 
        public int mainCompletionCount { set; get; }
        public int secondaryCompletionCount { set; get; }
        public int thirdCompletionCount { set; get; }
        public int allClearCount { set; get; }
    }

    public class LocalProgressionData
    {
        // rundownName would be used as the file name of the local progression storage
        public string rundownName { set; get; } = "";
        public uint rundownId { set; get; } = 0;
        public Dictionary<string, LocalExpProgData> localProgDict { set; get; }

        // the number of levels that have Main cleared
        public int mainClearCount { set; get; } = 0;
        // the number of levels that have Secondary cleared
        public int secondaryClearCount { set; get; } = 0;
        // ditto but third
        public int thirdClearCount { set; get; } = 0;
        // ditto but all clear
        public int allClearCount { set; get; } = 0;
        public LocalProgressionData()
        {
            localProgDict = new();
        }
    }
}
