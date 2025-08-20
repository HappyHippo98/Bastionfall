#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// SubScene-Component
// ^ Achte darauf: Deine Game.Editor.asmdef muss Unity.Scenes und Unity.Entities referenzieren.

namespace Game.Editor
{
    public static class SceneBakeTools
    {
        // --------------------------
        // MENÜEINTRÄGE
        // --------------------------

        [MenuItem("Build/Bake & Save > Open Scene (incl. SubScenes)")]
        public static void BakeOpenScene()
        {
            var active = SceneManager.GetActiveScene();
            if (!active.IsValid() || string.IsNullOrEmpty(active.path))
            {
                Debug.LogWarning("[Bake] Keine offene Scene mit Pfad gefunden.");
                return;
            }
            BakeOneScene(active.path);
        }

        [MenuItem("Build/Bake & Save > All Scenes in Build Settings")]
        public static void BakeAllInBuildSettings()
        {
            var list = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled) list.Add(s.path);

            BakeScenes(list);
        }

        [MenuItem("Build/Bake & Save > All Scenes in Project (Assets/**/*.unity)")]
        public static void BakeAllScenesInProject()
        {
            var guids = AssetDatabase.FindAssets("t:SceneAsset");
            var list = new List<string>(guids.Length);
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    list.Add(path);
            }
            BakeScenes(list);
        }

        // --------------------------
        // CORE
        // --------------------------

        static void BakeScenes(List<string> scenePaths)
        {
            if (scenePaths == null || scenePaths.Count == 0)
            {
                Debug.LogWarning("[Bake] Keine Szenen gefunden.");
                return;
            }

            try
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

                for (int i = 0; i < scenePaths.Count; i++)
                {
                    var path = scenePaths[i];
                    EditorUtility.DisplayProgressBar("Baking SubScenes",
                        $"{Path.GetFileNameWithoutExtension(path)} ({i + 1}/{scenePaths.Count})", (float)i / scenePaths.Count);

                    BakeOneScene(path);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[Bake] Fertig. {scenePaths.Count} Szene(n) verarbeitet.");
        }

        static void BakeOneScene(string scenePath)
        {
            // Szene additiv öffnen, damit wir sie wieder schließen können
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Bake] Szene ungültig: {scenePath}");
                return;
            }

            // 1) Alle SubScenes dieser Scene suchen
            var subScenes = FindSubScenes(scene);

            // 2) Jede SubScene öffnen, "baken" (Import), speichern, schließen
            for (int i = 0; i < subScenes.Count; i++)
            {
                var ssComp = subScenes[i];
                var ssAsset = ssComp.SceneAsset as SceneAsset;
                if (ssAsset == null)
                    continue;

                var ssPath = AssetDatabase.GetAssetPath(ssAsset);
                // SubScene selber als Scene öffnen
                var ss = EditorSceneManager.OpenScene(ssPath, OpenSceneMode.Additive);

                // Rebuild der Entity-Cache anstoßen (per Reflection, falls verfügbar)
                TryForceRebuildSubSceneCache(ssComp);

                // Import erzwingen – triggert die Entities-Baking-Pipeline
                AssetDatabase.ImportAsset(ssPath, ImportAssetOptions.ForceUpdate);

                // Speichern & wieder schließen
                EditorSceneManager.SaveScene(ss);
                EditorSceneManager.CloseScene(ss, true);
            }

            // 3) Hauptszene speichern
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Bake] Gebacken & gespeichert: {scenePath}  (SubScenes: {subScenes.Count})");
        }

        static List<SubScene> FindSubScenes(Scene scene)
        {
            var list = new List<SubScene>();
            foreach (var root in scene.GetRootGameObjects())
                list.AddRange(root.GetComponentsInChildren<SubScene>(true));
            return list;
        }

        // Versucht, Unity.Scenes.Editor.SubSceneInspectorUtility.RebuildEntityCache(SubScene) aufzurufen,
        // wenn es in deiner DOTS-Version vorhanden ist (API variiert je nach Version).
        static void TryForceRebuildSubSceneCache(SubScene subSceneComp)
        {
            try
            {
                var editorAsm = AppDomain.CurrentDomain.Load("Unity.Scenes.Editor");
                var utilType  = editorAsm?.GetType("Unity.Scenes.Editor.SubSceneInspectorUtility");
                var method    = utilType?.GetMethod("RebuildEntityCache", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(SubScene) }, null);
                method?.Invoke(null, new object[] { subSceneComp });
            }
            catch { /* unkritisch – ImportAsset unten triggert Baking ohnehin */ }
        }
    }
}
#endif
