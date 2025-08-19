#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class GhostPrefabGuidCheck
{
    [MenuItem("Tools/NetCode/Check Player Prefab GUIDs")]
    public static void Check()
    {
        var res = Resources.Load<GameObject>("Ghosts/Player");
        if (!res) { Debug.LogError("Resources/Ghosts/Player.prefab not found"); return; }

        var resPath = AssetDatabase.GetAssetPath(res);
        var resGuid = AssetDatabase.AssetPathToGUID(resPath);

        // Suche einen PlayerSpawnerAuthoring im geöffneten SubScene/Scene
        var spawner = Object.FindFirstObjectByType<Game.Shared.Authoring.PlayerSpawnerAuthoring>(FindObjectsInactive.Exclude);
        if (!spawner || !spawner.PlayerPrefab) { Debug.LogError("No PlayerSpawnerAuthoring or no PlayerPrefab set."); return; }

        var spPath = AssetDatabase.GetAssetPath(spawner.PlayerPrefab);
        var spGuid = AssetDatabase.AssetPathToGUID(spPath);

        Debug.Log($"Resources GUID: {resGuid}\nSpawner GUID:   {spGuid}\nMatch: {resGuid == spGuid}");
    }
}
#endif