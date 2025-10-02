#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BastionFall.Core.Shared.Editor
{
    public static class BastionFallBuildMenu
    {
        // ===== Menü =====
        [MenuItem("BastionFall/Build/Debug/Windows Client")]
        public static void Debug_WinClient()
        {
            BuildOne(Config.Debug, Platform.WindowsClient, true);
        }

        [MenuItem("BastionFall/Build/Debug/Windows Server")]
        public static void Debug_WinServer()
        {
            BuildOne(Config.Debug, Platform.WindowsServer, true);
        }

        [MenuItem("BastionFall/Build/Debug/Linux Client")]
        public static void Debug_LinClient()
        {
            BuildOne(Config.Debug, Platform.LinuxClient, true);
        }

        [MenuItem("BastionFall/Build/Debug/Linux Server")]
        public static void Debug_LinServer()
        {
            BuildOne(Config.Debug, Platform.LinuxServer, true);
        }

        [MenuItem("BastionFall/Build/Debug/Bundles/WindowsBundle")]
        public static void Debug_Bundle_Windows()
        {
            BuildBundle(Config.Debug, "Windows",
                Platform.WindowsClient, Platform.WindowsServer);
        }

        [MenuItem("BastionFall/Build/Debug/Bundles/LinuxBundle")]
        public static void Debug_Bundle_Linux()
        {
            BuildBundle(Config.Debug, "Linux",
                Platform.LinuxClient, Platform.LinuxServer);
        }

        [MenuItem("BastionFall/Build/Debug/Bundles/CompleteBundle")]
        public static void Debug_Bundle_Complete()
        {
            BuildBundle(Config.Debug, "Complete",
                Platform.WindowsClient, Platform.WindowsServer, Platform.LinuxClient, Platform.LinuxServer);
        }

        [MenuItem("BastionFall/Build/Release/Windows Client")]
        public static void Rel_WinClient()
        {
            BuildOne(Config.Release, Platform.WindowsClient, true);
        }

        [MenuItem("BastionFall/Build/Release/Windows Server")]
        public static void Rel_WinServer()
        {
            BuildOne(Config.Release, Platform.WindowsServer, true);
        }

        [MenuItem("BastionFall/Build/Release/Linux Client")]
        public static void Rel_LinClient()
        {
            BuildOne(Config.Release, Platform.LinuxClient, true);
        }

        [MenuItem("BastionFall/Build/Release/Linux Server")]
        public static void Rel_LinServer()
        {
            BuildOne(Config.Release, Platform.LinuxServer, true);
        }

        [MenuItem("BastionFall/Build/Release/Bundles/WindowsBundle")]
        public static void Rel_Bundle_Windows()
        {
            BuildBundle(Config.Release, "Windows",
                Platform.WindowsClient, Platform.WindowsServer);
        }

        [MenuItem("BastionFall/Build/Release/Bundles/LinuxBundle")]
        public static void Rel_Bundle_Linux()
        {
            BuildBundle(Config.Release, "Linux",
                Platform.LinuxClient, Platform.LinuxServer);
        }

        [MenuItem("BastionFall/Build/Release/Bundles/CompleteBundle")]
        public static void Rel_Bundle_Complete()
        {
            BuildBundle(Config.Release, "Complete",
                Platform.WindowsClient, Platform.WindowsServer, Platform.LinuxClient, Platform.LinuxServer);
        }

        // Optionaler Quick-Button im Menü
        [MenuItem("BastionFall/Dev/Regenerate Project Files Now")]
        public static void RegenerateNow()
        {
            QueueProjectSync("Manual menu");
        }

        private static void BuildBundle(Config cfg, string name, params Platform[] seq)
        {
            var label = $"{cfg} Bundles {name}";
            Debug.Log($"Build Started {label}");
            var sw = Stopwatch.StartNew();

            bool builtWinClient = false, builtWinServer = false;

            foreach (var p in seq)
            {
                if (!BuildOne(cfg, p, false))
                {
                    Debug.LogError($"Bundle Build FAILED {label} at {PlatToDisplay(p)}");
                    return;
                }

                if (p == Platform.WindowsClient) builtWinClient = true;
                if (p == Platform.WindowsServer) builtWinServer = true;
            }

            if (builtWinClient && builtWinServer)
                WriteWindowsBundleRunner(cfg);

            sw.Stop();
            Debug.Log($"Build Completed {label} in {Format(sw.Elapsed)}");
        }


        private static bool BuildOne(Config cfg, Platform plat, bool reveal)
        {
            var (target, sub, folder, file) = GetBuildInfo(plat);
            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                Debug.LogError($"Build target not installed: {target}. Install via Unity Hub.");
                return false;
            }

            var prevTarget = EditorUserBuildSettings.activeBuildTarget;
            var prevGroup = BuildPipeline.GetBuildTargetGroup(prevTarget);
            var prevSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;

            var prevDevelopment = EditorUserBuildSettings.development;
            var prevAllowDebugging = EditorUserBuildSettings.allowDebugging;
            var prevConnectProfiler = EditorUserBuildSettings.connectProfiler;

            try
            {
                if (EditorUserBuildSettings.activeBuildTarget != target)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

                EditorUserBuildSettings.standaloneBuildSubtarget = sub;

                var isDebug = cfg == Config.Debug;
                EditorUserBuildSettings.development = isDebug;
                EditorUserBuildSettings.allowDebugging = isDebug;
                EditorUserBuildSettings.connectProfiler = isDebug;

                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0)
                {
                    Debug.LogError("Build aborted: No scenes in Build Settings.");
                    return false;
                }

                var baseDir = Path.Combine("Builds", cfg.ToString(), folder);
                EnsureDir(baseDir);
                var exePath = Path.Combine(baseDir, file);

                var configName = $"{cfg} {PlatToDisplay(plat)}";
                Debug.Log($"Build Started {configName}");
                var sw = Stopwatch.StartNew();

                var opts = new BuildPlayerOptions
                {
                    scenes = scenes,
                    target = target,
                    locationPathName = exePath,
                    subtarget = (int)sub,
                    options = isDebug
                        ? BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler |
                          BuildOptions.CompressWithLz4
                        : BuildOptions.CompressWithLz4HC
                };

                var report = BuildPipeline.BuildPlayer(opts);
                sw.Stop();

                if (report.summary.result == BuildResult.Succeeded)
                {
                    WriteLaunchers(plat, exePath);
                    Debug.Log($"Build Completed {configName} in {Format(sw.Elapsed)}");
                    if (reveal) EditorUtility.RevealInFinder(exePath);
                    return true;
                }

                Debug.LogError($"Build FAILED {configName} with {report.summary.totalErrors} errors.");
                return false;
            }
            finally
            {
                if (EditorUserBuildSettings.standaloneBuildSubtarget != prevSubtarget)
                    EditorUserBuildSettings.standaloneBuildSubtarget = prevSubtarget;

                if (EditorUserBuildSettings.activeBuildTarget != prevTarget)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(prevGroup, prevTarget);

                EditorUserBuildSettings.development = prevDevelopment;
                EditorUserBuildSettings.allowDebugging = prevAllowDebugging;
                EditorUserBuildSettings.connectProfiler = prevConnectProfiler;

                QueueProjectSync($"Post-build ({PlatToDisplay(plat)})");
            }
        }


        private static (BuildTarget, StandaloneBuildSubtarget, string folder, string file) GetBuildInfo(Platform p)
        {
            var product = Application.productName;
            switch (p)
            {
                case Platform.WindowsClient:
                    return (BuildTarget.StandaloneWindows64, StandaloneBuildSubtarget.Player,
                        "WindowsClient", $"{product}_client.exe");
                case Platform.WindowsServer:
                    return (BuildTarget.StandaloneWindows64, StandaloneBuildSubtarget.Server,
                        "WindowsServer", $"{product}_server.exe");
                case Platform.LinuxClient:
                    return (BuildTarget.StandaloneLinux64, StandaloneBuildSubtarget.Player,
                        "LinuxClient", $"{product}_client.x86_64");
                case Platform.LinuxServer:
                    return (BuildTarget.StandaloneLinux64, StandaloneBuildSubtarget.Server,
                        "LinuxServer", $"{product}_server.x86_64");
                default:
                    throw new ArgumentOutOfRangeException(nameof(p), p, null);
            }
        }

        // ===== Launcher-Skripte =====
        private static void WriteLaunchers(Platform plat, string exePath)
        {
            var dir = Path.GetDirectoryName(exePath)!;
            var exe = Path.GetFileName(exePath);
            var commonArgs = "-unitycapture true -apploglevel info -unityloglevel warn";

            if (plat == Platform.WindowsClient || plat == Platform.WindowsServer)
            {
                var args = plat == Platform.WindowsClient
                    ? $"-client -host 127.0.0.1 -port 7979 {commonArgs}"
                    : $"-server -nogui -port 7979 {commonArgs}";

                var batName = plat == Platform.WindowsClient ? "RunClient.bat" : "RunServer.bat";
                var bat = string.Join("\r\n", "@echo off", "setlocal", "pushd %~dp0", $"\"{exe}\" {args}", "popd");
                File.WriteAllText(Path.Combine(dir, batName), bat);
            }
            else
            {
                var args = plat == Platform.LinuxClient
                    ? $"-client -host 127.0.0.1 -port 7979 {commonArgs}"
                    : $"-server -nogui -port 7979 {commonArgs}";

                var shName = plat == Platform.LinuxClient ? "run_client.sh" : "run_server.sh";
                var sh = string.Join("\n", "#!/usr/bin/env bash", "set -e",
                    "DIR=\"$(cd \"$(dirname \"${BASH_SOURCE[0]}\")\" && pwd)\"", $"\"${{DIR}}/{exe}\" {args}");
                var path = Path.Combine(dir, shName);
                File.WriteAllText(path, sh);
            }
        }

        private static void WriteWindowsBundleRunner(Config cfg)
        {
            var root = Path.Combine("Builds", cfg.ToString());
            var exeClient = Path.Combine(root, "WindowsClient", $"{Application.productName}_client.exe");
            var exeServer = Path.Combine(root, "WindowsServer", $"{Application.productName}_server.exe");
            if (!File.Exists(exeClient) || !File.Exists(exeServer)) return;

            var bat = string.Join("\r\n", "@echo off", "setlocal", "pushd %~dp0", "echo Starting Windows Server...",
                $"start \"\" \"WindowsServer\\{Path.GetFileName(exeServer)}\" -server -nogui -port 7979 -unitycapture true -apploglevel info -unityloglevel warn",
                "timeout /t 1 >nul", "echo Starting Windows Client...",
                $"start \"\" \"WindowsClient\\{Path.GetFileName(exeClient)}\" -client -host 127.0.0.1 -port 7979 -unitycapture true -apploglevel info -unityloglevel warn",
                "popd");
            File.WriteAllText(Path.Combine(root, "Run_Windows_ClientServer.bat"), bat);
        }

        // ===== Post-Build Sync =====
        private static void QueueProjectSync(string reason)
        {
            // Nach dem Build-Cleanup im nächsten Editor-Tick ausführen.
            EditorApplication.delayCall += () =>
            {
                try
                {
                    Debug.Log($"[Build/Post] Regenerating project files & recompiling… ({reason})");

                    // 1) Assets/asmdefs refreshen (wichtig nach Target/Subtarget-Wechsel)
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                    // 2) Scripting-Recompile anstoßen (aktualisiert Defines wie UNITY_SERVER)
                    CompilationPipeline.RequestScriptCompilation();

                    // 3) .sln/.csproj neu generieren – offizieller, public Weg
                    //    (öffnet den Standard-Editor kurz; reicht, um die Projektdateien zu erneuern)
                    if (!Application.isBatchMode)
                        // Dieser Menüpunkt macht genau das, was "Regenerate project files" tut.
                        EditorApplication.ExecuteMenuItem("Assets/Open C# Project");

                    Debug.Log("[Build/Post] Project sync done.");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Build/Post] Project sync failed: {e.Message}");
                }
            };
        }

        // ===== Utils =====
        private static string PlatToDisplay(Platform p)
        {
            return p switch
            {
                Platform.WindowsClient => "Windows Client",
                Platform.WindowsServer => "Windows Server",
                Platform.LinuxClient => "Linux Client",
                Platform.LinuxServer => "Linux Server",
                _ => p.ToString()
            };
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private static string Format(TimeSpan t)
        {
            if (t.TotalSeconds < 60) return $"{(int)t.TotalSeconds}s";
            if (t.TotalMinutes < 60) return $"{t.Minutes:D2}:{t.Seconds:D2}";
            return $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}";
        }

        // ===== Impl =====
        private enum Config
        {
            Debug,
            Release
        }

        private enum Platform
        {
            WindowsClient,
            WindowsServer,
            LinuxClient,
            LinuxServer
        }
    }
}
#endif