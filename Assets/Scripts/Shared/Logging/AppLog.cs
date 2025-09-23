using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Burst;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Shared.Logging
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error
    }

    internal static class LogLevelUtil
    {
        public static bool TryParse(string s, out LogLevel level)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "trace":
                    level = LogLevel.Trace;
                    return true;
                case "debug":
                    level = LogLevel.Debug;
                    return true;
                case "info":
                    level = LogLevel.Info;
                    return true;
                case "warn":
                case "warning":
                    level = LogLevel.Warn;
                    return true;
                case "error":
                    level = LogLevel.Error;
                    return true;
                default:
                    level = LogLevel.Info;
                    return false;
            }
        }
    }

    public readonly struct LogEntry
    {
        public readonly DateTime TsUtc;
        public readonly LogLevel Level;
        public readonly string Location; // Server/Client/ThinClient/Bootstrap/Unity
        public readonly string Source; // System/Authoring/Console
        public readonly string Message;
        public readonly string StackTrace;

        public LogEntry(DateTime tsUtc, LogLevel level, string location, string source, string message,
            string stackTrace)
        {
            TsUtc = tsUtc;
            Level = level;
            Location = string.IsNullOrWhiteSpace(location) ? "App" : location;
            Source = string.IsNullOrWhiteSpace(source) ? "General" : source;
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
        }

        public override string ToString()
        {
            return
                $"[{TsUtc:yyyy-MM-dd HH:mm:ss.fff}] [{Level.ToString().ToUpper()}] [{Location}] [{Source}] {Message}";
        }
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
            _source = string.IsNullOrWhiteSpace(source) ? "General" : source;
        }

        public void Trace(string msg)
        {
            Write(LogLevel.Trace, msg);
        }

        public void Debug(string msg)
        {
            Write(LogLevel.Debug, msg);
        }

        public void Info(string msg)
        {
            Write(LogLevel.Info, msg);
        }

        public void Warn(string msg)
        {
            Write(LogLevel.Warn, msg);
        }

        public void Error(string msg, Exception ex = null)
        {
            Write(LogLevel.Error, msg, ex);
        }

        private static void WriteCore(LogEntry e)
        {
            AppLog.EnqueueApp(e);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // In Editor/Dev zusätzlich in Konsole zeigen (ohne Unity-Capture-Duplikate)
            AppLog.SuppressUnityCapture = true;
            try
            {
                var line = AppLog.FormatConsoleLine(e); // -> ohne Timestamp
                switch (e.Level)
                {
                    case LogLevel.Error:
                        UnityEngine.Debug.LogError(line);
                        // Stacktrace nur bei Warn/Error
                        var stErr = string.IsNullOrEmpty(e.StackTrace)
                            ? new StackTrace(1, true).ToString()
                            : e.StackTrace;
                        UnityEngine.Debug.LogError(stErr);
                        break;

                    case LogLevel.Warn:
                        UnityEngine.Debug.LogWarning(line);
                        var stWarn = string.IsNullOrEmpty(e.StackTrace)
                            ? new StackTrace(1, true).ToString()
                            : e.StackTrace;
                        UnityEngine.Debug.LogWarning(stWarn);
                        break;

                    default:
                        UnityEngine.Debug.Log(line);
                        break;
                }
            }
            finally
            {
                AppLog.SuppressUnityCapture = false;
            }
#endif
        }


        private void Write(LogLevel level, string msg, Exception ex = null)
        {
            var entry = new LogEntry(DateTime.UtcNow, level, _location, _source, msg, ex?.ToString() ?? string.Empty);
            WriteCore(entry);
        }
    }

    public static class AppLog
    {
        // Separate Stores
        private static readonly ConcurrentQueue<LogEntry> AppStore = new();
        private static readonly ConcurrentQueue<LogEntry> UnityStore = new();


        // File sinks
        private static readonly object AppFileGate = new();
        private static StreamWriter _appWriter;
        private static readonly object UnityFileGate = new();
        private static StreamWriter _unityWriter;

        // Filtering
        private static LogLevel _minAppLevel = LogLevel.Info;
        private static LogLevel _minUnityLevel = LogLevel.Warn;
        internal static bool SuppressUnityCapture;

        private static readonly Dictionary<(string loc, string src), AppLogger> _loggers = new();

        private static readonly int _cap = 20000;

        public static string AppFilePath { get; private set; }

        public static string UnityFilePath { get; private set; }

        // Unity capture control
        public static bool UnityCaptureEnabled { get; private set; } = true;

        public static IAppLogger For<T>(string location)
        {
            return For(location, typeof(T).Name);
        }

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
                    try
                    {
                        _appWriter.WriteLine(e.ToString()); // -> enthält Timestamp wie gewünscht
                        if (ShouldWriteStackTrace(e) && !string.IsNullOrEmpty(e.StackTrace))
                            _appWriter.WriteLine(e.StackTrace);
                    }
                    catch
                    {
                        /* ignore */
                    }
            }

            Trim(AppStore);
        }

        // ---------- UNITY CAPTURE ----------
        private static void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (!UnityCaptureEnabled || SuppressUnityCapture) return;

            var level = type switch
            {
                LogType.Error or LogType.Assert or LogType.Exception => LogLevel.Error,
                LogType.Warning => LogLevel.Warn,
                _ => LogLevel.Info
            };
            if (level < _minUnityLevel) return;

            var entry = new LogEntry(DateTime.UtcNow, level, "Unity", "Console", condition ?? string.Empty,
                stackTrace ?? string.Empty);
            UnityStore.Enqueue(entry);

            lock (UnityFileGate)
            {
                if (_unityWriter != null)
                    try
                    {
                        _unityWriter.WriteLine(entry.ToString());
                        if (ShouldWriteStackTrace(entry) && !string.IsNullOrEmpty(entry.StackTrace))
                            _unityWriter.WriteLine(entry.StackTrace);
                    }
                    catch
                    {
                        /* ignore */
                    }
            }

            Trim(UnityStore);
        }

        public static void ConfigureAppFile(string path, LogLevel minLevel)
        {
            _minAppLevel = minLevel;
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                lock (AppFileGate)
                {
                    _appWriter?.Dispose();
                    _appWriter =
                        new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                            { AutoFlush = true, NewLine = Environment.NewLine };
                    AppFilePath = path;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[Bootstrap] [ERROR] [Bootstrap] [AppLog] App file sink failed: {ex}");
#endif
            }
        }

        public static void ConfigureUnityFile(string path, LogLevel minLevel)
        {
            _minUnityLevel = minLevel;
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                lock (UnityFileGate)
                {
                    _unityWriter?.Dispose();
                    _unityWriter =
                        new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                            { AutoFlush = true, NewLine = Environment.NewLine };
                    UnityFilePath = path;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[Bootstrap] [ERROR] [Bootstrap] [AppLog] Unity file sink failed: {ex}");
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

        public static void WriteDebugNoTimestamp(string location, string source, string message)
        {
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Debug,
                string.IsNullOrWhiteSpace(location) ? "App" : location,
                string.IsNullOrWhiteSpace(source) ? "General" : source,
                message ?? string.Empty, string.Empty);
            AppStore.Enqueue(entry);
            Trim(AppStore);

            var line = $"[DEBUG] [{entry.Location}] [{entry.Source}] {entry.Message}";

            lock (AppFileGate)
            {
                if (_appWriter != null)
                    try
                    {
                        _appWriter.WriteLine(line);
                    }
                    catch
                    {
                        /* ignore */
                    }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SuppressUnityCapture = true;
            try
            {
                Debug.Log(line);
            }
            finally
            {
                SuppressUnityCapture = false;
            }
#endif
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

        private static void Trim(ConcurrentQueue<LogEntry> q)
        {
            while (q.Count > _cap && q.TryDequeue(out _))
            {
            }
        }

        internal static string FormatConsoleLine(LogEntry e)
        {
            return $"[{e.Level.ToString().ToUpper()}] [{e.Location}] [{e.Source}] {e.Message}";
        }

        internal static bool ShouldWriteStackTrace(LogEntry e)
        {
            return e.Level == LogLevel.Warn || e.Level == LogLevel.Error;
        }
    }

    public static class DevLog
    {
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [BurstDiscard]
        public static void DebugSystem<T>(string location, string msg)
        {
            AppLog.WriteDebugNoTimestamp(location, typeof(T).Name, msg);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [BurstDiscard]
        public static void InfoSystem<T>(string location, string msg)
        {
            AppLog.For<T>(location).Info(msg);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [BurstDiscard]
        public static void WarnSystem<T>(string location, string msg)
        {
            AppLog.For<T>(location).Warn(msg);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [BurstDiscard]
        public static void ErrorSystem<T>(string location, string msg, Exception ex = null)
        {
            AppLog.For<T>(location).Error(msg, ex);
        }
    }
}