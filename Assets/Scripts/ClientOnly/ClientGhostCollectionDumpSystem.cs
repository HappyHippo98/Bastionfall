// Assets/Scripts/ClientOnly/ClientGhostCollectionDumpSystem.cs
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Game.Shared.Util;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct ClientGhostCollectionDumpSystem : ISystem
{
    public void OnUpdate(ref SystemState s)
    {
        var go = Resources.Load<GameObject>("Ghosts/Player");
        s.LogInfo("Ghosts", "Resources.Load(\"Ghosts/Player\") = {0}", go ? "OK" : "NULL");

        var q = s.GetEntityQuery(ComponentType.ReadOnly<GhostCollection>());
        if (!q.IsEmptyIgnoreFilter)
        {
            var coll = q.GetSingleton<GhostCollection>();
            s.LogInfo("Ghosts", "GhostCollection NumLoadedPrefabs={0}", coll.NumLoadedPrefabs);
        }
        else
        {
            s.LogWarn("Ghosts", "No GhostCollection entity found.");
        }
        s.Enabled = false;
    }
}