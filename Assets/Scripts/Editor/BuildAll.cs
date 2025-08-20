// Assets/Scripts/Editor/Build/BuildAll.cs

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SysDiag = System.Diagnostics;
using UDebug = UnityEngine.Debug;

namespace Game.Editor
{
    public static class BuildAll
    {
        // Produkt-/Binary-Namen
        const string ProductClient = "Bastionfall";
        const string ProductServer = "Bastionfall_Server";

        // Default-Startparameter (für Startskripte)
        const string DefaultAddress = "127.0.0.1";
        const int    DefaultPort    = 7979;

        // Struktur: Builds/<Channel>/<Platform>/<Role>/
        enum Channel { Debug, Release }
        enum Role { Client, Server }

        // ---------- MENÜ: RELEASE -------------------------------------------------

        [MenuItem("Bastionfall/Build/Release/Windows Client")]
        public static void Release_Win_Client()  => Build(Channel.Release, BuildTarget.StandaloneWindows64, Role.Client);

        [MenuItem("Bastionfall/Build/Release/Linux Client")]
        public static void Release_Lin_Client()  => Build(Channel.Release, BuildTarget.StandaloneLinux64,   Role.Client);

        [MenuItem("Bastionfall/Build/Release/Windows Server")]
        public static void Release_Win_Server()  => Build(Channel.Release, BuildTarget.StandaloneWindows64, Role.Server);

        [MenuItem("Bastionfall/Build/Release/Linux Server")]
        public static void Release_Lin_Server()  => Build(Channel.Release, BuildTarget.StandaloneLinux64,   Role.Server);

        [MenuItem("Bastionfall/Build/Release/Windows Package")]
        public static void Release_Win_Package() => Package(Channel.Release, "Windows");

        [MenuItem("Bastionfall/Build/Release/Linux Package")]
        public static void Release_Lin_Package() => Package(Channel.Release, "Linux");

        // ---------- MENÜ: DEBUG ---------------------------------------------------

        [MenuItem("Bastionfall/Build/Debug/Windows Client")]
        public static void Debug_Win_Client()    => Build(Channel.Debug, BuildTarget.StandaloneWindows64, Role.Client);

        [MenuItem("Bastionfall/Build/Debug/Linux Client")]
        public static void Debug_Lin_Client()    => Build(Channel.Debug, BuildTarget.StandaloneLinux64,   Role.Client);

        [MenuItem("Bastionfall/Build/Debug/Windows Server")]
        public static void Debug_Win_Server()    => Build(Channel.Debug, BuildTarget.StandaloneWindows64, Role.Server);

        [MenuItem("Bastionfall/Build/Debug/Linux Server")]
        public static void Debug_Lin_Server()    => Build(Channel.Debug, BuildTarget.StandaloneLinux64,   Role.Server);

        [MenuItem("Bastionfall/Build/Debug/Windows Package")]
        public static void Debug_Win_Package()   => Package(Channel.Debug, "Windows");

        [MenuItem("Bastionfall/Build/Debug/Linux Package")]
        public static void Debug_Lin_Package()   => Package(Channel.Debug, "Linux");

        // ---------- MENÜ: BAKE ----------------------------------------------------

        [MenuItem("Bastionfall/Bake/Scenes in Build Settings")]
        public static void Bake_ScenesInBuildSettings()
        {
            UDebug.Log("here");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) { UDebug.LogWarning("[Bake] Keine Szenen in Build Settings."); return; }

            var original = SceneManager.GetActiveScene().path;
            try
            {
                for (int i = 0; i < scenes.Length; i++)
                {
                    var p = scenes[i];
                    EditorUtility.DisplayProgressBar("Bake Scenes", $"Öffne & backe: {Path.GetFileNameWithoutExtension(p)}", (float)i / scenes.Length);
                    EditorSceneManager.OpenScene(p, OpenSceneMode.Single);

                    // SubScenes backen (per Reflection, funktioniert mit verschiedenen DOTS Versionen)
                    TryBakeAllOpenSubScenes();

                    AssetDatabase.SaveAssets();
                    EditorSceneManager.SaveOpenScenes();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(original) && File.Exists(original))
                    EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
            }

            UDebug.Log("[Bake] Fertig – alle Szenen aus den Build Settings gespeichert & (sofern verfügbar) SubScenes gebacken.");
        }

        // ---------- KERN-BUILD ----------------------------------------------------

        static void Build(Channel channel, BuildTarget target, Role role)
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
                throw new Exception("No scenes in Build Settings.");

            bool il2cpp = channel == Channel.Release;
            bool server = role == Role.Server;

            var outRoot = GetOutputRoot(channel, target, role);
            Directory.CreateDirectory(outRoot);
            Directory.CreateDirectory(Path.Combine(outRoot, "logs"));

            var group = BuildTargetGroup.Standalone;
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
            EditorUserBuildSettings.standaloneBuildSubtarget = server
                ? StandaloneBuildSubtarget.Server
                : StandaloneBuildSubtarget.Player;

            // --- NEU: UNITY_DEBUG Define setzen/entfernen ---
            var definesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var defines = definesStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            bool hasUnityDebug = defines.Contains("UNITY_DEBUG");
            if (channel == Channel.Debug)
            {
                if (!hasUnityDebug) { defines.Add("UNITY_DEBUG"); }
            }
            else
            {
                if (hasUnityDebug) { defines.RemoveAll(d => d == "UNITY_DEBUG"); }
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
            // ------------------------------------------------

            PlayerSettings.productName = server ? ProductServer : ProductClient;
            PlayerSettings.SetScriptingBackend(group, il2cpp ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);

            var options = BuildOptions.CompressWithLz4;
            if (!il2cpp) options |= BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler;
            if (server)  options |= BuildOptions.EnableHeadlessMode;

            string binary = GetBinaryName(target, server);
            string location = Path.Combine(outRoot, binary);

            var sw = SysDiag.Stopwatch.StartNew();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                target = target,
                subtarget = (int)(server ? StandaloneBuildSubtarget.Server : StandaloneBuildSubtarget.Player),
                options = options,
                scenes = scenes.Select(s => s.path).ToArray(),
                locationPathName = location
            });
            sw.Stop();

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"{target} {role} {channel} build failed: {report.summary.result}");

            UDebug.Log($"{target} {role} {channel} OK: {report.summary.totalSize/(1024*1024)} MB  |  Build: {sw.Elapsed:mm\\:ss}");

            if (target == BuildTarget.StandaloneWindows64) WriteWindowsScripts(outRoot, server);
            else if (target == BuildTarget.StandaloneLinux64) WriteLinuxScripts(outRoot, server);

            EditorUtility.RevealInFinder(outRoot);
        }

        // ---------- PACKAGING -----------------------------------------------------

        static void Package(Channel channel, string platform)
        {
            // Packt die komplette Plattform-Mappe (Client + Server) als ZIP
            var root = Path.Combine("Builds", channel.ToString(), platform);
            if (!Directory.Exists(root)) { UDebug.LogWarning($"[Package] Ordner existiert nicht: {root}"); return; }

            Directory.CreateDirectory("Builds/Packages");
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var zipPath = Path.Combine("Builds/Packages", $"Bastionfall_{platform}_{channel}_{stamp}.zip");

            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(root, zipPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: true);
            EditorUtility.RevealInFinder(zipPath);
            UDebug.Log($"[Package] Fertig: {zipPath}");
        }

        // ---------- UTIL ----------------------------------------------------------

        static string GetOutputRoot(Channel ch, BuildTarget t, Role r)
        {
            var platform = t == BuildTarget.StandaloneWindows64 ? "Windows" :
                t == BuildTarget.StandaloneLinux64   ? "Linux"   : t.ToString();

            var role = r == Role.Client ? "Client" : "Server";
            return Path.Combine("Builds", ch.ToString(), platform, role);
        }

        static string GetBinaryName(BuildTarget t, bool server)
        {
            var prod = server ? ProductServer : ProductClient;
            switch (t)
            {
                case BuildTarget.StandaloneWindows64: return prod + ".exe";
                case BuildTarget.StandaloneLinux64:   return prod + ".x86_64";
                default: throw new NotSupportedException(t.ToString());
            }
        }

        static void WriteWindowsScripts(string dir, bool server)
        {
            if (!server)
            {
                File.WriteAllText(Path.Combine(dir, "Start_Client.bat"),
                    $@"@echo off
setlocal
mkdir ""logs"" 2>nul
set EXE=""{ProductClient}.exe""
%EXE% -mode client -address {DefaultAddress} -port {DefaultPort} -logdir logs -logprefix client
");
            }
            else
            {
                File.WriteAllText(Path.Combine(dir, "Start_Server.bat"),
                    $@"@echo off
setlocal
mkdir ""logs"" 2>nul
set EXE=""{ProductServer}.exe""
%EXE% -batchmode -nographics -mode server -port {DefaultPort} -logdir logs -logprefix server
");
            }
        }

        static void WriteLinuxScripts(string dir, bool server)
        {
            if (!server)
            {
                var sh = Path.Combine(dir, "start_client.sh");
                File.WriteAllText(sh,
                    $@"#!/usr/bin/env bash
mkdir -p ""$(dirname ""$0"")/logs""
cd ""$(dirname ""$0"")""
./{ProductClient}.x86_64 -mode client -address {DefaultAddress} -port {DefaultPort} -logdir logs -logprefix client
");
                TryMakeExecutable(sh);
            }
            else
            {
                var sh = Path.Combine(dir, "start_server.sh");
                File.WriteAllText(sh,
                    $@"#!/usr/bin/env bash
mkdir -p ""$(dirname ""$0"")/logs""
cd ""$(dirname ""$0"")""
./{ProductServer}.x86_64 -batchmode -nographics -mode server -port {DefaultPort} -logdir logs -logprefix server
");
                TryMakeExecutable(sh);
            }
        }

        static void TryMakeExecutable(string path)
        {
            try
            {
                SysDiag.Process.Start(new SysDiag.ProcessStartInfo {
                    FileName = "bash",
                    Arguments = $"-lc \"chmod +x '{path}'\"",
                    UseShellExecute = false
                });
            }
            catch { /* kein bash verfügbar – ok */ }
        }

        static void TryBakeAllOpenSubScenes()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Unity.Scenes.Editor");
                if (asm == null) return;

                var t = asm.GetType("Unity.Scenes.Editor.SubSceneInspectorUtility");
                if (t == null) return;

                // Häufige API-Namen in verschiedenen Versionen
                var m = t.GetMethod("BakeAllOpenSubScenes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        ?? t.GetMethod("BakeOpenSubScenes",     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        ?? t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                            .FirstOrDefault(mi => mi.Name.Contains("Bake") && mi.GetParameters().Length == 0);

                m?.Invoke(null, null);
            }
            catch (Exception e)
            {
                UDebug.Log($"[Bake] Konnte SubScenes nicht backen: {e.Message}");
            }
        }
    }
}
