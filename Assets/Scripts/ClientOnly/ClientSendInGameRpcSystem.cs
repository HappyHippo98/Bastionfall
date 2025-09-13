using ClientOnly.Authoring;
using Shared.Authoring.Network;
using Shared.Logging;
using Shared.Rpc;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ClientOnly
{
    /*
    // Marker je Connection, damit wir nur 1x senden
    public struct SentGoInGameTag : IComponentData {}

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientSendInGameRpcSystem : ISystem
    {

        public void OnCreate(ref SystemState s)
        {
            s.RequireForUpdate<EnableNetcode>();
        }

        public void OnUpdate(ref SystemState s)
        {
            var em  = s.EntityManager;
            var q   = em.CreateEntityQuery(
                ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkStreamConnection>(),
                ComponentType.Exclude<SentGoInGameTag>()
            );
            if (q.IsEmptyIgnoreFilter) return;

            using var conns = q.ToEntityArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var conn in conns)
            {
                // 1) RPC an den Server (Client->Server: TargetConnection NICHT setzen)
                var rpc = ecb.CreateEntity();
                ecb.AddComponent(rpc, new GoInGameRpc());
                ecb.AddComponent<SendRpcCommandRequest>(rpc);

                // 2) Lokal InGame markieren, damit Snapshots ankommen
                if (!em.HasComponent<NetworkStreamInGame>(conn))
                    ecb.AddComponent<NetworkStreamInGame>(conn);

                // 3) Doppelsend verhindern
                ecb.AddComponent<SentGoInGameTag>(conn);

                DevLog.InfoSystem<ClientSendInGameRpcSystem>("Client", "Sent GoInGameRpc + marked local connection InGame.");
            }

            ecb.Playback(em);
            ecb.Dispose();
        }
    }
    */
}