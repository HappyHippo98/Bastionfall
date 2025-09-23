using Shared.Authoring.Network;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ClientOnly
{
    // Einmal-Tag, damit wir die Identity nur einmal senden
    public struct SentIdentityOnceTag : IComponentData
    {
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClientEnterGameSystem))]
    [UpdateBefore(typeof(RpcSystem))]
    [BurstCompile]
    public partial struct ClientSendIdentityOnceSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            state.RequireForUpdate<NetworkStreamInGame>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Beispiel: pro Connection, die schon InGame ist und noch NICHT gesendet hat
            foreach (var (_, conn) in SystemAPI
                         .Query<NetworkId>()
                         .WithAll<NetworkStreamConnection, NetworkStreamInGame>()
                         .WithNone<SentIdentityOnceTag>()
                         .WithEntityAccess())
            {
                // 1) RPC-Entity erzeugen & Payload anheften
                var rpc = ecb.CreateEntity();
                // ecb.AddComponent(rpc, new YourIdentityRpc { Name = ..., ... });

                // 2) Richtung Client->Server: KEIN TargetConnection setzen
                ecb.AddComponent<SendRpcCommandRequest>(rpc);

                // 3) Merken, dass wir für diese Connection gesendet haben
                ecb.AddComponent<SentIdentityOnceTag>(conn);
            }

            ecb.Playback(em);
            ecb.Dispose();
        }
    }
}