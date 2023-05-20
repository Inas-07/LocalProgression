namespace LocalProgression.Data
{
    public class ExpeditionProgressionData
    {
        public string ExpeditionKey { set; get; } = string.Empty;

        public int MainCompletionCount { set; get; } = 0;

        public int SecondaryCompletionCount { set; get; } = 0;

        public int ThirdCompletionCount { set; get; } = 0;

        public int AllClearCount { set; get; } = 0;
    }
}
