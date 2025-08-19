#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BFFileLoggerEditor
{
    static BFFileLoggerEditor()
    {
        // Bei Enter Play Mode starten – auch ohne Domain Reload
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // gewünschten Prefix in PlayerPrefs spiegeln (zur Laufzeit lesbar)
                var pfx = EditorPrefs.GetString("BF_LogPrefix", "client"); // Wunsch: "client"
                PlayerPrefs.SetString("BF_LogPrefix", pfx);
                PlayerPrefs.Save();

                BFFileLogger.StartIfNeeded(); // erstellt die Datei + schreibt den Pfad ins Console-Log
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                BFFileLogger.Shutdown();
            }
        };
    }

    [MenuItem("Bastionfall/Logs/Prefix/Client")]
    static void SetClient() => EditorPrefs.SetString("BF_LogPrefix", "client");

    [MenuItem("Bastionfall/Logs/Prefix/Server")]
    static void SetServer() => EditorPrefs.SetString("BF_LogPrefix", "server");

    [MenuItem("Bastionfall/Logs/Prefix/Play")]
    static void SetPlay() => EditorPrefs.SetString("BF_LogPrefix", "play");

    [MenuItem("Bastionfall/Logs/Open persistentDataPath")]
    static void OpenFolder()
    {
        EditorUtility.RevealInFinder(System.IO.Path.Combine(Application.persistentDataPath, "Logs"));
    }
}
#endif