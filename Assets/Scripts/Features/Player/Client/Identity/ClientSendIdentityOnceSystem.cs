using BastionFall.Features.Player.Shared.RPC;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BastionFall.Features.Player.Client.Identity
{
    /// <summary>
    ///     Sendet pro Connection genau 1x die eigene Identity (GUID + Name).
    ///     Persist-Flag global via PlayerPrefs:
    ///     BF_USE_PERSISTED_NAME = 0 (Default) → pro Start random Name
    ///     BF_USE_PERSISTED_NAME = 1 → gespeicherter Name wiederverwenden
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ClientSendIdentityOnceSystem : ISystem
    {
        private EntityQuery _unidentifiedConnections;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            _unidentifiedConnections = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<NetworkId>(), ComponentType.ReadOnly<NetworkStreamInGame>() },
                None = new[] { ComponentType.ReadOnly<IdentitySentTag>() }
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_unidentifiedConnections.IsEmpty) return;

            var usePersistedName = PlayerPrefs.GetInt("BF_USE_PERSISTED_NAME", 0) == 1;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var connections = _unidentifiedConnections.ToEntityArray(Allocator.Temp);
            var guid = ClientIdentity.GetOrCreateGuid(); // persistent (Editor/Player getrennt)
            var name = ClientIdentity.GetDisplayName(usePersistedName); // random oder persistent

            foreach (var conn in connections)
            {
                var rpc = ecb.CreateEntity();
                ecb.AddComponent(rpc, new IdentifyPlayerRpc
                {
                    Guid = guid,
                    DisplayName = name,
                    UsedPersistedName = usePersistedName
                });
                ecb.AddComponent(rpc, new SendRpcCommandRequest { TargetConnection = conn });
                ecb.AddComponent<IdentitySentTag>(conn);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Client] Identify → GUID={guid}, Name={name}, Persist={usePersistedName}");
#endif
            }

            ecb.Playback(state.EntityManager);
        }
    }

    public struct IdentitySentTag : IComponentData
    {
    }
}