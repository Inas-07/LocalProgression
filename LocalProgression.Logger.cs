using BepInEx.Logging;
using UnityEngine;
namespace LocalProgression
{
    internal static class LocalProgressionLogger
    {
        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("LocalProgression");

        public static void Log(string format, params object[] args)
        {
            LocalProgressionLogger.Log(string.Format(format, args));
        }

        public static void Log(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Message, str);
        }

        public static void Warning(string format, params object[] args)
        {
            LocalProgressionLogger.Warning(string.Format(format, args));
        }

        public static void Warning(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Warning, str);
        }

        public static void Error(string format, params object[] args)
        {
            LocalProgressionLogger.Error(string.Format(format, args));
        }

        public static void Error(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Error, str);
        }

        public static void Debug(string format, params object[] args)
        {
            LocalProgressionLogger.Debug(string.Format(format, args));
        }

        public static void Debug(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Debug, str);
        }
    }

    public static class Utils
    {
        public static bool TryGetComponent<T>(this GameObject obj, out T comp)
        {
            comp = obj.GetComponent<T>();
            return comp != null;
        }
    }
}

