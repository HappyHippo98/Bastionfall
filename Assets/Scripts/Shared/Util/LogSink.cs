// Assets/Scripts/Shared/Util/LogSink.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Game.Shared.Util
{
    /// Log-Datei mit Zeitstempel:
    ///   YYYYMMDD_HHMMSS_server.log / ..._client.log
    /// CLI:
    ///   -logdir <dir>   (Ordner, Standard: ./logs)
    ///   -logprefix <p>  ("server" oder "client", Standard: aus BatchMode abgeleitet)
    ///   -nolog          (deaktiviert File-Logging)
    public static class LogSink
    {
        static StreamWriter _writer;
        static readonly object _lock = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() => StartNewFileInternal(null);

        // --- Editor helper API (wird vom Editor-Bridge aufgerufen) ------------
        public static void RestartForEditor(string prefixOverride = null)
        {
            Close();
            StartNewFileInternal(prefixOverride);
        }

        public static void StopForEditor() => Close();

        // ----------------------------------------------------------------------
        static void StartNewFileInternal(string forcedPrefix)
        {
            if (HasArg("nolog")) { Debug.Log("[LogSink] disabled via -nolog"); return; }

            try
            {
                var dir = Arg("logdir") ?? "logs";
                if (!Path.IsPathRooted(dir))
                {
                    var baseDir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    dir = Path.GetFullPath(Path.Combine(baseDir, dir));
                }
                Directory.CreateDirectory(dir);

                var prefix = forcedPrefix ?? Arg("logprefix") ?? (Application.isBatchMode ? "server" : "client");
                var file = $"{DateTime.Now:yyyyMMdd_HHmmss}_{prefix}.log";
                var path = Path.Combine(dir, file);

                var fs = OpenWithRetry(path);
                _writer = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = true };

                Header(prefix, path);
                Application.logMessageReceivedThreaded += Handle;
                Application.quitting += Close;
                AppDomain.CurrentDomain.ProcessExit += (_, __) => Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LogSink] init failed: {e}");
            }
        }

        static FileStream OpenWithRetry(string path, int retries = 6, int backoffMs = 200)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
                }
                catch (IOException) { Thread.Sleep(backoffMs); }
                catch (UnauthorizedAccessException) { Thread.Sleep(backoffMs); }
            }
            var fallback = Path.Combine(Path.GetDirectoryName(path) ?? ".", $"{Path.GetFileNameWithoutExtension(path)}_alt{Path.GetExtension(path)}");
            return new FileStream(fallback, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        }

        static void Header(string role, string path)
        {
            Write($"# === Log Start {DateTime.Now:O} ===");
            Write($"# Role={role}   BatchMode={Application.isBatchMode}");
            Write($"# Unity={Application.unityVersion}   Product={Application.productName} {Application.version}");
            Write($"# Platform={Application.platform}");
            Write($"# LogFile={path}");
            Write($"# CmdLine={Environment.CommandLine}");
            Write("# =================================");
        }

        static void Handle(string condition, string stack, LogType type)
        {
            var stamp = $"{DateTime.Now:HH:mm:ss.fff} (+{Time.realtimeSinceStartup,7:0.000}s)";
            var lvl = type.ToString().ToUpperInvariant();
            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;

            Write($"{stamp} [T{tid}] [{lvl}] {condition}");
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && !string.IsNullOrEmpty(stack))
                Write(stack);
        }

        static void Write(string line)
        {
            lock (_lock) _writer?.WriteLine(line);
        }

        static void Close()
        {
            try
            {
                Application.logMessageReceivedThreaded -= Handle;
                lock (_lock) { _writer?.Flush(); _writer?.Dispose(); _writer = null; }
            } catch { /* ignore */ }
        }

        static string Arg(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i].TrimStart('-','/'), key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }
        static bool HasArg(string key)
        {
            var args = Environment.GetCommandLineArgs();
            return args.Any(a => string.Equals(a.TrimStart('-','/'), key, StringComparison.OrdinalIgnoreCase));
        }
    }
}
