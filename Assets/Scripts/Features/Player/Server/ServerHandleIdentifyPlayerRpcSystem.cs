using BastionFall.Features.Player.Shared.Authoring.Monitoring;
using BastionFall.Features.Player.Shared.RPC;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

// PlayerIdentity (Ghost-Komponente)

namespace BastionFall.Features.Player.Server
{
    /// <summary>
    ///     Server: empfängt IdentifyPlayerRpc und speichert die Identity an der Connection.
    ///     (Von dort kopiert ServerCopyIdentityToGhostSystem sie einmalig auf den Player-Ghost.)
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RpcSystem))]
    [BurstCompile]
    public partial struct ServerHandleIdentifyPlayerRpcSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (rpc, reqSrc, rpcEntity) in SystemAPI
                         .Query<RefRO<IdentifyPlayerRpc>, RefRO<ReceiveRpcCommandRequest>>()
                         .WithEntityAccess())
            {
                var conn = reqSrc.ValueRO.SourceConnection;

                // An der Connection merken (Server-seitig)
                var id = new PlayerIdentity
                {
                    DisplayName = rpc.ValueRO.DisplayName,
                    Guid = rpc.ValueRO.Guid
                };

                if (!em.HasComponent<PlayerIdentity>(conn))
                    ecb.AddComponent(conn, id);
                else
                    ecb.SetComponent(conn, id);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var ownerId = em.HasComponent<NetworkId>(conn) ? em.GetComponentData<NetworkId>(conn).Value : -1;
                Debug.Log($"[Server] Identity received ← Owner={ownerId}, GUID={id.Guid}, Name={id.DisplayName}");
#endif
                ecb.DestroyEntity(rpcEntity); // RPC verbrauchen
            }

            ecb.Playback(em);
        }
    }
}