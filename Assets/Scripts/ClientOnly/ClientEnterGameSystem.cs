using Shared.Authoring.Network;
using Shared.Rpc;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
[UpdateAfter(typeof(NetworkStreamConnectSystem))] // erst NACHDEM die Connection existiert
public partial struct ClientEnterGameSystem : ISystem
{
    private EntityQuery _q;

    public void OnCreate(ref SystemState s)
    {
        s.RequireForUpdate<EnableNetcode>();
        _q = s.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<NetworkId>(),
            ComponentType.ReadOnly<NetworkStreamConnection>(),
            ComponentType.Exclude<NetworkStreamInGame>()); // Debounce ohne Tag
    }

    public void OnUpdate(ref SystemState s)
    {
        if (_q.IsEmptyIgnoreFilter) return;

        var em  = s.EntityManager;
        using var conns = _q.ToEntityArray(Allocator.Temp);
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var conn in conns)
        {
            // 1) RPC an Server
            var rpc = ecb.CreateEntity();
            ecb.AddComponent(rpc, new GoInGameRpc());
            ecb.AddComponent<SendRpcCommandRequest>(rpc);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Shared.Logging.DevLog.InfoSystem<ClientEnterGameSystem>(
                "Client", $"GoInGame: queued RPC (→Server). LocalConnEntity={conn}");
#endif

            // 2) Lokal SOFORT InGame markieren (Snapshots dürfen ankommen)
            if (!em.HasComponent<NetworkStreamInGame>(conn))
            {
                ecb.AddComponent<NetworkStreamInGame>(conn);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Shared.Logging.DevLog.InfoSystem<ClientEnterGameSystem>(
                    "Client", "GoInGame: locally marked connection InGame");
#endif
            }
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

}