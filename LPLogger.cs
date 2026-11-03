using BepInEx.Logging;
namespace LocalProgression
{
    internal static class LPLogger
    {
        public static ManualLogSource Logger { get; internal set; } = null!;

        public static void Log(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }

        public static void Log(string str)
        {
            if (Logger == null) return;

            Logger.Log(LogLevel.Message, str);
        }

        public static void Warning(string format, params object[] args)
        {
            Warning(string.Format(format, args));
        }

        public static void Warning(string str)
        {
            if (Logger == null) return;

            Logger.Log(LogLevel.Warning, str);
        }

        public static void Error(string format, params object[] args)
        {
            Error(string.Format(format, args));
        }

        public static void Error(string str)
        {
            if (Logger == null) return;

            Logger.Log(LogLevel.Error, str);
        }

        public static void Debug(string format, params object[] args)
        {
            Debug(string.Format(format, args));
        }

        public static void Debug(string str)
        {
            if (Logger == null) return;

            Logger.Log(LogLevel.Debug, str);
        }
    }
}
