// Assets/Scripts/Shared/Util/BFFileLogger.cs
#if UNITY_DEBUG || UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Shared.Util
{
    /// <summary>
    /// File-Logger: Nur in Editor/Debug aktiv.
    /// In Release wird ein No-Op-Stub (unten) kompiliert.
    /// </summary>
    public static class BfFileLogger
    {
        static readonly object _lock = new object();
        static StreamWriter _writer;
        static string _logFilePath;
        static bool _started;

        const string PrefKeyPrefix = "BF_LogPrefix";

        public static void SetPrefix(string prefix)
        {
            try { if (!string.IsNullOrEmpty(prefix)) PlayerPrefs.SetString(PrefKeyPrefix, prefix); }
            catch { /* ignore in headless */ }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void StartIfNeeded()
        {
            if (_started) return;
            _started = true;

            try
            {
                string argLogDir = GetArgValue("-logdir");
                string baseDir;
                if (!string.IsNullOrEmpty(argLogDir))
                {
                    baseDir = argLogDir;
                    if (!Path.IsPathRooted(baseDir))
                        baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, baseDir);
                }
                else
                {
                    baseDir = Path.Combine(Application.persistentDataPath, "Logs");
                }

                string prefix = GetArgValue("-logprefix");
                if (string.IsNullOrEmpty(prefix))
                {
                    try { prefix = PlayerPrefs.GetString(PrefKeyPrefix, string.Empty); }
                    catch { prefix = string.Empty; }
                }
                if (string.IsNullOrEmpty(prefix)) prefix = GuessPrefix();

                string roleFolder =
                    Application.isEditor ? "Editor" :
                    string.Equals(prefix, "server", StringComparison.OrdinalIgnoreCase) ? "Server" :
                    string.Equals(prefix, "listen", StringComparison.OrdinalIgnoreCase) ? "Listen" :
                    "Client";

                string finalDir = Path.Combine(baseDir, roleFolder);
                Directory.CreateDirectory(finalDir);

                string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{ts}_{prefix.ToLowerInvariant()}.log";
                _logFilePath = Path.Combine(finalDir, fileName);

                _writer = new StreamWriter(File.Open(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true, NewLine = "\n"
                };

                Application.logMessageReceivedThreaded += OnLogMessage;

                WriteHeader(baseDir, roleFolder, prefix);
                Info("BFFileLogger", $"Writing logs to: {_logFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BFFileLogger] Failed to start: {ex}");
            }
        }

        public static void Stop()
        {
            if (!_started) return;
            _started = false;
            Application.logMessageReceivedThreaded -= OnLogMessage;
            lock (_lock)
            {
                try { _writer?.Flush(); _writer?.Dispose(); }
                catch { /* ignore */ }
                _writer = null;
            }
        }

        static void WriteHeader(string baseDir, string roleFolder, string prefix)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# === Log Start {DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffffffK} ===");
            sb.AppendLine($"# Role={(Application.isEditor ? "Editor/Player" : prefix)}   BatchMode={Application.isBatchMode}");
            sb.AppendLine($"# Unity={Application.unityVersion}   Product={Application.productName} {Application.version}");
            sb.AppendLine($"# Platform={Application.platform}");
            sb.AppendLine($"# LogFile={_logFilePath}");
            sb.AppendLine($"# CmdLine={Environment.CommandLine}");
            sb.AppendLine("# =================================");
            lock (_lock) _writer?.WriteLine(sb.ToString());
        }

        static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (_writer == null) return;

            var now = DateTime.Now;
            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var level = type.ToString().ToUpperInvariant();
            string line = $"{now:HH:mm:ss.fff} [T{tid}] [{level}] {condition}";
            lock (_lock)
            {
                _writer.WriteLine(line);
                if (type == LogType.Exception || type == LogType.Error)
                    _writer.WriteLine(stackTrace);
            }
        }

        static string GuessPrefix()
        {
            string mode = GetArgValue("-mode");
            if (!string.IsNullOrEmpty(mode)) return mode.ToLowerInvariant();

            if (Application.isBatchMode && SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return "server";

            return "client";
        }

        static string GetArgValue(string name)
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < args.Length) return args[i + 1];
                        return string.Empty;
                    }
                }
            }
            catch { /* ignored */ }
            return string.Empty;
        }

        static void Info(string tag, string msg) => Debug.Log($"[{tag}] {msg}");
    }
}
#else
// Release: No-Op Stub, damit Aufrufe kompilieren
namespace Game.Shared.Util
{
    public static class BFFileLogger
    {
        public static void SetPrefix(string prefix) { }
        public static void StartIfNeeded() { }
        public static void Stop() { }
    }
}
#endif
