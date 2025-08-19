using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

public static class BFFileLogger
{
    static readonly object Gate = new object();
    static StreamWriter _writer;
    static string _logPath;
    static string _prefix;
    static bool _started;

    // Reset bei (Re)Load/Enter Play Mode – wichtig für "Disable Domain Reload"
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _writer = null;
        _logPath = null;
        _prefix = null;
        _started = false;
    }

    // Läuft früh in Player/Headless-Builds; im Editor nur, wenn Domain reloadet
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void AutoStart_BeforeSplash() => StartIfNeeded();

    public static void StartIfNeeded(string prefixOverride = null)
    {
        if (_started) return;

        try
        {
            // Prefix finden: expliziter Override > Env > PlayerPrefs > -logprefix > -mode > Fallback
            _prefix = prefixOverride
                   ?? Environment.GetEnvironmentVariable("BF_LOGPREFIX")
                   ?? PlayerPrefs.GetString("BF_LogPrefix", null)
                   ?? GetArg("-logprefix");

            if (string.IsNullOrEmpty(_prefix))
            {
                var mode = GetArg("-mode")?.ToLowerInvariant();
                _prefix = mode switch {
                    "server" => "server",
                    "client" => "client",
                    "listen" => "listen",
                    _ => (Application.isBatchMode ? "headless" : "play")
                };
            }

            var logDir = GetArg("-logdir");
            if (string.IsNullOrEmpty(logDir))
                logDir = Path.Combine(Application.persistentDataPath, "Logs");

            Directory.CreateDirectory(logDir);
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logPath = Path.Combine(logDir, $"{ts}_{_prefix}.log");

            _writer = new StreamWriter(new FileStream(_logPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            { AutoFlush = true, NewLine = "\n" };

            WriteHeader();

            Application.logMessageReceivedThreaded += HandleLog;

            // Stacktrace-Policy
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);

            // Sichtbarer Hinweis mit Pfad
            Debug.Log($"[BFFileLogger] Writing logs to: {_logPath}");

            // Sauber schließen (auch im Editor-Spielmodus)
            var go = new GameObject("BFFileLogger_Sink") { hideFlags = HideFlags.HideAndDontSave };
            go.AddComponent<BFFileLoggerSink>();
            UnityEngine.Object.DontDestroyOnLoad(go);

            _started = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[BFFileLogger] Init failed: {e}");
        }
    }

    public static void Shutdown()
    {
        lock (Gate)
        {
            if (_writer != null)
            {
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
            }
            _started = false;
        }
    }

    static void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (_writer == null) return;

        var sb = new StringBuilder(512);
        sb.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
        sb.Append(" [").Append(Thread.CurrentThread.ManagedThreadId).Append("] ");
        sb.Append('[').Append(type).Append("] ").Append(condition);

        if (!string.IsNullOrEmpty(stackTrace) &&
            (type == LogType.Error || type == LogType.Assert || type == LogType.Exception))
        {
            sb.Append("\n").Append(stackTrace.TrimEnd());
        }

        lock (Gate) _writer.WriteLine(sb.ToString());
    }

    static void WriteHeader()
    {
        var args = Environment.GetCommandLineArgs();
        var role = GetArg("-mode") ?? (Application.isBatchMode ? "Server/Headless?" : "Editor/Player");
        var header =
$@"# === Log Start {DateTime.Now:O} ===
# Role={role}   BatchMode={Application.isBatchMode}
# Unity={Application.unityVersion}   Product={Application.productName} {Application.version}
# Platform={Application.platform}
# LogFile={_logPath}
# CmdLine={string.Join(" ", args)}
# =================================";
        lock (Gate) _writer.WriteLine(header);
    }

    static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        return null;
    }
}

// sorgt fürs saubere Schließen
public class BFFileLoggerSink : MonoBehaviour
{
    void OnApplicationQuit() => BFFileLogger.Shutdown();
    void OnDestroy() => BFFileLogger.Shutdown();
}
