using Shared.Authoring.Monitoring; // PlayerIdentity
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ServerOnly.Systems
{
    // Markiert Player-Ghosts, bei denen die Identity bereits kopiert wurde
    public struct PlayerIdentityAppliedTag : IComponentData {}

    /// <summary>
    /// Sucht neue Player-Ghosts und kopiert die PlayerIdentity von der Connection auf den Ghost.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RpcSystem))]
    [BurstCompile]
    public partial struct ServerCopyIdentityToGhostSystem : ISystem
    {
        private EntityQuery _connectionsQuery;
        private EntityQuery _playersNeedingIdentity;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();

            _connectionsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<NetworkId>(),
                    ComponentType.ReadOnly<NetworkStreamInGame>(),
                    ComponentType.ReadOnly<PlayerIdentity>() // vom RPC gesetzt (an der Connection)
                }
            });

            _playersNeedingIdentity = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<GhostOwner>(),
                    ComponentType.ReadWrite<PlayerIdentity>() // auf dem Ghost (vom Baker)
                },
                None = new[]
                {
                    ComponentType.ReadOnly<PlayerIdentityAppliedTag>()
                }
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_playersNeedingIdentity.IsEmpty || _connectionsQuery.IsEmpty) return;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            // Map: NetworkId -> Connection-Entity
            var connEntities = _connectionsQuery.ToEntityArray(Allocator.Temp);
            var connIds      = _connectionsQuery.ToComponentDataArray<NetworkId>(Allocator.Temp);
            var connMap      = new NativeParallelHashMap<int, Entity>(connEntities.Length, Allocator.Temp);

            for (int i = 0; i < connEntities.Length; i++)
                connMap.TryAdd(connIds[i].Value, connEntities[i]);

            foreach (var (owner, ghost, e) in SystemAPI
                         .Query<RefRO<GhostOwner>, RefRW<PlayerIdentity>>()
                         .WithNone<PlayerIdentityAppliedTag>()
                         .WithEntityAccess())
            {
                var netId = owner.ValueRO.NetworkId;

                if (connMap.TryGetValue(netId, out var conn))
                {
                    var id = SystemAPI.GetComponent<PlayerIdentity>(conn);
                    ghost.ValueRW.DisplayName = id.DisplayName;
                    ghost.ValueRW.Guid        = id.Guid;

                    ecb.AddComponent<PlayerIdentityAppliedTag>(e);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[Server] Applied Identity to Ghost (NetId={netId}) → Name={id.DisplayName}, Guid={id.Guid}");
#endif
                }
            }

            ecb.Playback(state.EntityManager);
            connMap.Dispose();
        }
    }
}
