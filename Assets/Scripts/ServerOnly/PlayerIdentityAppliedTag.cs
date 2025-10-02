﻿using Shared.Authoring.Monitoring; // PlayerIdentity
using Shared.Rpc;                  // ChatMessageRpc
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
    /// Zusätzlich: broadcastet "<Name> spawned" (SERVER) sobald Identity angewendet wurde.
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
                    ComponentType.ReadOnly<NetworkStreamConnection>(),
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
            using var connEntities = _connectionsQuery.ToEntityArray(state.WorldUpdateAllocator);
            using var connIds      = _connectionsQuery.ToComponentDataArray<NetworkId>(state.WorldUpdateAllocator);
            var connMap = new NativeParallelHashMap<int, Entity>(connEntities.Length, state.WorldUpdateAllocator);
            for (int i = 0; i < connEntities.Length; i++)
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
                    ghostId.ValueRW.Guid        = id.Guid;

                    // Tag setzen (fertig angewendet)
                    ecb.AddComponent<PlayerIdentityAppliedTag>(e);

                    // --- SERVERMSG: "<Name> spawned" an alle ---
                    using var targets = _connectionsQuery.ToEntityArray(state.WorldUpdateAllocator);
                    for (int i = 0; i < targets.Length; i++)
                    {
                        var rpc = ecb.CreateEntity();
                        ecb.AddComponent(rpc, new ChatMessageRpc
                        {
                            SenderNetworkId = 0,
                            SenderName      = (FixedString64Bytes)"SERVER",
                            Text            = (FixedString512Bytes)$"{id.DisplayName} spawned"
                        });
                        ecb.AddComponent(rpc, new SendRpcCommandRequest { TargetConnection = targets[i] });
                    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[Server] Applied Identity to Ghost (NetId={netId}) → Name={id.DisplayName}, Guid={id.Guid} (+spawned msg)");
#endif
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
