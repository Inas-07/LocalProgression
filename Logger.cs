﻿using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ScanPosOverride
{
    internal static class Logger
    {
        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("ScanPosOverride");

        public static void Log(string format, params object[] args)
        {
            Logger.Log(string.Format(format, args));
        }

        public static void Log(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Message, str);
        }

        public static void Warning(string format, params object[] args)
        {
            Logger.Warning(string.Format(format, args));
        }

        public static void Warning(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Warning, str);
        }

        public static void Error(string format, params object[] args)
        {
            Logger.Error(string.Format(format, args));
        }

        public static void Error(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Error, str);
        }

        public static void Debug(string format, params object[] args)
        {
            Logger.Debug(string.Format(format, args));
        }

        public static void Debug(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Debug, str);
        }

        public static void Log(BepInExDebugLogInterpolatedStringHandler logHandler)
        {
            logger.LogDebug(logHandler);
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

