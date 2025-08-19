using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Game.Shared.Util
{
    /// <summary>
    /// Single, unified file logger for Editor + Client + Server.
    /// Usage:
    ///  - In Editor, BFFileLoggerEditor boots this automatically.
    ///  - In builds, pass -logdir <dir> and -logprefix <client|server|listen> (BuildAll already does).
    ///  - Alternatively call BFFileLogger.StartIfNeeded() once on startup.
    /// The logger writes to "<logdir>/<Role>/<timestamp>_<prefix>.log".
    /// </summary>
    public static class BFFileLogger
    {
        static readonly object _lock = new object();
        static StreamWriter _writer;
        static string _logFilePath;
        static bool _started;

        // PlayerPrefs key used by the Editor bootstrap to persist the desired prefix
        const string PrefKeyPrefix = "BF_LogPrefix";

        /// <summary>Set a desired prefix for the file name (e.g. "client", "server"). Optional.</summary>
        public static void SetPrefix(string prefix)
        {
            try { if (!string.IsNullOrEmpty(prefix)) PlayerPrefs.SetString(PrefKeyPrefix, prefix); }
            catch { /* ignore in headless */ }
        }

        /// <summary>Start the logger once. Safe to call repeatedly.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void StartIfNeeded()
        {
            if (_started) return;
            _started = true;

            try
            {
                // 1) Base directory
                string argLogDir = GetArgValue("-logdir");
                string baseDir;
                if (!string.IsNullOrEmpty(argLogDir))
                {
                    baseDir = argLogDir;
                    // Relative path -> make it relative to the executable folder
                    if (!Path.IsPathRooted(baseDir))
                        baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, baseDir);
                }
                else
                {
                    baseDir = Path.Combine(Application.persistentDataPath, "Logs");
                }

                // 2) Role subfolder
                string prefix = GetArgValue("-logprefix");
                if (string.IsNullOrEmpty(prefix))
                {
                    try { prefix = PlayerPrefs.GetString(PrefKeyPrefix, string.Empty); }
                    catch { prefix = string.Empty; }
                }
                if (string.IsNullOrEmpty(prefix)) prefix = GuessPrefix();

                // Normalize for folder grouping
                string roleFolder =
                    Application.isEditor ? "Editor" :
                    string.Equals(prefix, "server", StringComparison.OrdinalIgnoreCase) ? "Server" :
                    string.Equals(prefix, "listen", StringComparison.OrdinalIgnoreCase) ? "Listen" :
                    "Client";

                string finalDir = Path.Combine(baseDir, roleFolder);
                Directory.CreateDirectory(finalDir);

                // 3) File name
                string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{ts}_{prefix.ToLowerInvariant()}.log";
                _logFilePath = Path.Combine(finalDir, fileName);

                // 4) Attach
                _writer = new StreamWriter(File.Open(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true,
                    NewLine = "\n"
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

        /// <summary>Stop and dispose writer. Optional.</summary>
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
            // Try to guess from command line or environment
            string mode = GetArgValue("-mode");
            if (!string.IsNullOrEmpty(mode))
                return mode.ToLowerInvariant();

            // If headless/batch likely server
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
                    if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
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
