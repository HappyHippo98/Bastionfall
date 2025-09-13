using Shared.Authoring.Network;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientInGameProbeSystem : ISystem
{
    EntityQuery _q;
    public void OnCreate(ref SystemState s)
    {
        s.RequireForUpdate<EnableNetcode>();
        _q = s.EntityManager.CreateEntityQuery(
            typeof(NetworkId), typeof(NetworkStreamInGame));
    }

    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;
        if (_q.IsEmptyIgnoreFilter)
        {
            Debug.Log("[ClientWorld] ❌ Noch NICHT InGame (keine NetworkStreamInGame auf der Connection).");
            return;
        }

        var nid = _q.GetSingleton<NetworkId>().Value;
        Debug.Log($"[ClientWorld] ✅ InGame. Meine NID={nid}.");

        // Optional: prüfe ob ein Ack-Component existiert (Snapshots fließen)
        if (em.HasComponent<NetworkSnapshotAck>(_q.GetSingletonEntity()))
            Debug.Log("[ClientWorld] Snapshot-Ack vorhanden (Snapshots sollten ankommen).");
        else
            Debug.LogWarning("[ClientWorld] ⚠ Kein NetworkSnapshotAck an der Connection (keine Snapshot-Pipeline?).");
    }
}