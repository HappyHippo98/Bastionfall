using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Burst;
using UnityEngine;

namespace Shared.Logging
{
    public enum LogLevel { Trace, Debug, Info, Warn, Error }

    static class LogLevelUtil
    {
        public static bool TryParse(string s, out LogLevel level)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "trace": level = LogLevel.Trace; return true;
                case "debug": level = LogLevel.Debug; return true;
                case "info":  level = LogLevel.Info;  return true;
                case "warn":
                case "warning": level = LogLevel.Warn; return true;
                case "error": level = LogLevel.Error; return true;
                default: level = LogLevel.Info; return false;
            }
        }
    }

    public readonly struct LogEntry
    {
        public readonly DateTime TsUtc;
        public readonly LogLevel Level;
        public readonly string Location; // Server/Client/ThinClient/Bootstrap/Unity
        public readonly string Source;   // System/Authoring/Console
        public readonly string Message;
        public readonly string StackTrace;

        public LogEntry(DateTime tsUtc, LogLevel level, string location, string source, string message, string stackTrace)
        {
            TsUtc      = tsUtc;
            Level      = level;
            Location   = string.IsNullOrWhiteSpace(location) ? "App"     : location;
            Source     = string.IsNullOrWhiteSpace(source)   ? "General" : source;
            Message    = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
        }

        public override string ToString()
            => $"[{TsUtc:yyyy-MM-dd HH:mm:ss.fff}] [{Level.ToString().ToUpper()}] [{Location}] [{Source}] {Message}";
    }

    public interface IAppLogger
    {
        void Trace(string msg);
        void Debug(string msg);
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg, Exception ex = null);
    }

    public sealed class AppLogger : IAppLogger
    {
        private readonly string _location;
        private readonly string _source;

        internal AppLogger(string location, string source)
        {
            _location = string.IsNullOrWhiteSpace(location) ? "App" : location;
            _source   = string.IsNullOrWhiteSpace(source)   ? "General" : source;
        }

        private static void WriteCore(LogEntry e)
        {
            AppLog.EnqueueApp(e);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // in Editor/Dev zusätzlich in Konsole zeigen (ohne in Unity-Log zu duplizieren)
            AppLog.SuppressUnityCapture = true;
            try
            {
                switch (e.Level)
                {
                    case LogLevel.Error:
                        UnityEngine.Debug.LogError(e.ToString());
                        if (!string.IsNullOrEmpty(e.StackTrace))
                            UnityEngine.Debug.LogError(e.StackTrace);
                        break;
                    case LogLevel.Warn:
                        UnityEngine.Debug.LogWarning(e.ToString());
                        break;
                    default:
                        UnityEngine.Debug.Log(e.ToString());
                        break;
                }
            }
            finally { AppLog.SuppressUnityCapture = false; }
#endif
        }

        private void Write(LogLevel level, string msg, Exception ex = null)
        {
            var entry = new LogEntry(DateTime.UtcNow, level, _location, _source, msg, ex?.ToString() ?? string.Empty);
            WriteCore(entry);
        }

        public void Trace(string msg)                 => Write(LogLevel.Trace, msg);
        public void Debug(string msg)                 => Write(LogLevel.Debug, msg);
        public void Info (string msg)                 => Write(LogLevel.Info , msg);
        public void Warn (string msg)                 => Write(LogLevel.Warn , msg);
        public void Error(string msg, Exception ex=null)=> Write(LogLevel.Error, msg, ex);
    }

    public static class AppLog
    {
        // Separate Stores
        static readonly ConcurrentQueue<LogEntry> AppStore   = new();
        static readonly ConcurrentQueue<LogEntry> UnityStore = new();
        
        public static string AppFilePath   => _appPath;
        public static string UnityFilePath => _unityPath;


        // File sinks
        static readonly object AppFileGate   = new(); static StreamWriter _appWriter;   static string _appPath;
        static readonly object UnityFileGate = new(); static StreamWriter _unityWriter; static string _unityPath;

        // Filtering
        static LogLevel _minAppLevel   = LogLevel.Info;
        static LogLevel _minUnityLevel = LogLevel.Warn;

        // Unity capture control
        public  static bool UnityCaptureEnabled { get; private set; } = true;
        internal static bool SuppressUnityCapture = false;

        static readonly Dictionary<(string loc,string src), AppLogger> _loggers = new();

        public static IAppLogger For<T>(string location) => For(location, typeof(T).Name);
        public static IAppLogger For(string location, string source)
        {
            lock (_loggers)
            {
                var key = (location ?? "App", source ?? "General");
                if (!_loggers.TryGetValue(key, out var logger))
                    _loggers[key] = logger = new AppLogger(key.Item1, key.Item2);
                return logger;
            }
        }

        // ---------- APP ENQUEUE ----------
        internal static void EnqueueApp(LogEntry e)
        {
            if (e.Level < _minAppLevel) return;
            AppStore.Enqueue(e);
            lock (AppFileGate)
            {
                if (_appWriter != null)
                {
                    try {
                        _appWriter.WriteLine(e.ToString());
                        if (!string.IsNullOrEmpty(e.StackTrace)) _appWriter.WriteLine(e.StackTrace);
                    } catch { /* ignore */ }
                }
            }
            Trim(AppStore);
        }

        // ---------- UNITY CAPTURE ----------
        static void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (!UnityCaptureEnabled || SuppressUnityCapture) return;

            var level = type switch
            {
                LogType.Error or LogType.Assert or LogType.Exception => LogLevel.Error,
                LogType.Warning => LogLevel.Warn,
                _ => LogLevel.Info
            };
            if (level < _minUnityLevel) return;

            var entry = new LogEntry(DateTime.UtcNow, level, "Unity", "Console", condition ?? string.Empty, stackTrace ?? string.Empty);
            UnityStore.Enqueue(entry);

            lock (UnityFileGate)
            {
                if (_unityWriter != null)
                {
                    try {
                        _unityWriter.WriteLine(entry.ToString());
                        if (!string.IsNullOrEmpty(entry.StackTrace)) _unityWriter.WriteLine(entry.StackTrace);
                    } catch { /* ignore */ }
                }
            }
            Trim(UnityStore);
        }

        public static void ConfigureAppFile(string path, LogLevel minLevel)
        {
            _minAppLevel = minLevel;
            if (string.IsNullOrWhiteSpace(path)) return;
            try {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                lock (AppFileGate)
                {
                    _appWriter?.Dispose();
                    _appWriter = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                    { AutoFlush = true, NewLine = Environment.NewLine };
                    _appPath = path;
                }
            } catch (Exception ex) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogError($"[Bootstrap] [ERROR] [Bootstrap] [AppLog] App file sink failed: {ex}");
#endif
            }
        }

        public static void ConfigureUnityFile(string path, LogLevel minLevel)
        {
            _minUnityLevel = minLevel;
            if (string.IsNullOrWhiteSpace(path)) return;
            try {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                lock (UnityFileGate)
                {
                    _unityWriter?.Dispose();
                    _unityWriter = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                    { AutoFlush = true, NewLine = Environment.NewLine };
                    _unityPath = path;
                }
            } catch (Exception ex) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogError($"[Bootstrap] [ERROR] [Bootstrap] [AppLog] Unity file sink failed: {ex}");
#endif
            }
        }

        public static void SetUnityCapture(bool enabled)
        {
            if (enabled == UnityCaptureEnabled) return;
            UnityCaptureEnabled = enabled;
        }

        public static void HookUnityCapture()
        {
            Application.logMessageReceivedThreaded -= OnUnityLog; // safety
            Application.logMessageReceivedThreaded += OnUnityLog;
        }

        public static List<LogEntry> SnapshotApp()
        {
            var list = new List<LogEntry>(AppStore.Count);
            foreach (var e in AppStore) list.Add(e);
            return list;
        }

        public static List<LogEntry> SnapshotUnity()
        {
            var list = new List<LogEntry>(UnityStore.Count);
            foreach (var e in UnityStore) list.Add(e);
            return list;
        }

        static int _cap = 20000;
        static void Trim(ConcurrentQueue<LogEntry> q)
        {
            while (q.Count > _cap && q.TryDequeue(out _)) { }
        }
    }

    // bequeme Dev-Calls für Systems, kompilieren nur in Editor/Dev
    public static class DevLog
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [BurstDiscard]
        public static void InfoSystem<T>(string location, string msg)
            => AppLog.For<T>(location).Info(msg);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [BurstDiscard]
        public static void WarnSystem<T>(string location, string msg)
            => AppLog.For<T>(location).Warn(msg);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [BurstDiscard]
        public static void ErrorSystem<T>(string location, string msg, System.Exception ex = null)
            => AppLog.For<T>(location).Error(msg, ex);
    }
}
