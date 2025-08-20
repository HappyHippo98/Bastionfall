#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Erzwingt im Editor-Playmode einen separaten Log-Unterordner,
    /// damit Editor-Logs nicht mit Client/Server-Build-Logs vermischt werden.
    /// </summary>
    [InitializeOnLoad]
    public static class BfEditorLogStartup
    {
        private const string PrefKey_LogDir    = "bf.logdir";
        private const string PrefKey_LogPrefix = "bf.logprefix";
        private const string EditorSubdir      = "editor_client"; // Wunschname

        static BfEditorLogStartup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;

            // Basis: Application.persistentDataPath/Logs/editor_client
            var baseLogs = Path.Combine(Application.persistentDataPath, "Logs");
            var editorLogs = Path.Combine(baseLogs, EditorSubdir);
            Directory.CreateDirectory(editorLogs);

            // Werte in PlayerPrefs spiegeln (BFFileLogger liest die Prefs)
            PlayerPrefs.SetString(PrefKey_LogDir, editorLogs);
            PlayerPrefs.SetString(PrefKey_LogPrefix, "client");
            PlayerPrefs.Save();

            // Optional: Sofort öffnen, falls dein BFFileLogger eine direkte API hat
            // BFFileLogger.Open(editorLogs, "client");
            Debug.Log($"[BFEditorLogStartup] Editor-Logs nach: {editorLogs}");
        }
    }
}
#endif