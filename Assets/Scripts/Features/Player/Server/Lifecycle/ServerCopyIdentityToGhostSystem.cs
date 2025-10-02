using BastionFall.Core.Shared.Request.Server;
using BastionFall.Features.Player.Shared.Authoring.Monitoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace BastionFall.Features.Player.Server.Lifecycle
{
    public struct PlayerIdentityAppliedTag : IComponentData
    {
    }

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
                    ComponentType.ReadOnly<NetworkStreamConnection>(),
                    ComponentType.ReadOnly<PlayerIdentity>()
                }
            });

            _playersNeedingIdentity = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<GhostOwner>(),
                    ComponentType.ReadWrite<PlayerIdentity>()
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

            using var connEntities = _connectionsQuery.ToEntityArray(state.WorldUpdateAllocator);
            using var connIds = _connectionsQuery.ToComponentDataArray<NetworkId>(state.WorldUpdateAllocator);
            var connMap = new NativeParallelHashMap<int, Entity>(connEntities.Length, state.WorldUpdateAllocator);
            for (var i = 0; i < connEntities.Length; i++)
                connMap.TryAdd(connIds[i].Value, connEntities[i]);

            foreach (var (owner, ghostId, e) in SystemAPI
                         .Query<RefRO<GhostOwner>, RefRW<PlayerIdentity>>()
                         .WithNone<PlayerIdentityAppliedTag>()
                         .WithEntityAccess())
            {
                var netId = owner.ValueRO.NetworkId;

                if (connMap.TryGetValue(netId, out var conn))
                {
                    var id = SystemAPI.GetComponent<PlayerIdentity>(conn);
                    ghostId.ValueRW.DisplayName = id.DisplayName;
                    ghostId.ValueRW.Guid = id.Guid;

                    ecb.AddComponent<PlayerIdentityAppliedTag>(e);

                    // System-Message request (statt Chat RPC)
                    var msgE = ecb.CreateEntity();
                    ecb.AddComponent(msgE, new RequestBroadcastSystemMessage
                    {
                        Text = (FixedString512Bytes)$"{id.DisplayName} spawned"
                    });
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}