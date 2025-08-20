#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Boots BFFileLogger inside the Editor and provides a menu to open the log folder.
    /// </summary>
    [InitializeOnLoad]
    public static class BfFileLoggerEditor
    {
        static BfFileLoggerEditor()
        {
            // Default the prefix to "client" for editor play sessions.
            Game.Shared.Util.BfFileLogger.SetPrefix("client");
            Game.Shared.Util.BfFileLogger.StartIfNeeded();
        }

        [MenuItem("Bastionfall/Logs/Open Editor Log Folder", priority = 50)]
        public static void OpenEditorLogFolder()
        {
            string baseDir = System.IO.Path.Combine(Application.persistentDataPath, "Logs", "Editor");
            System.IO.Directory.CreateDirectory(baseDir);
            EditorUtility.RevealInFinder(baseDir);
        }
    }
}
#endif