// Assets/Scripts/ServerOnly/ServerDespawnOnDisconnectSystem.cs

using Game.Shared.Authoring;
using Game.Shared.Config;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
// PlayerTag

// NetGameSettings (ScriptableObject)

namespace ServerOnly.Logging
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ServerDespawnOnDisconnectSystem : ISystem
    {
        bool _despawnEnabled;

        public void OnCreate(ref SystemState s)
        {
            _despawnEnabled = true;
            var cfg = Resources.Load<NetGameSettings>("NetGameSettings");
            if (cfg) _despawnEnabled = cfg.DespawnOnDisconnect;
        }

        public void OnUpdate(ref SystemState s)
        {
            if (!_despawnEnabled) return;

            var activeIds = new NativeHashSet<int>(16, Allocator.Temp);
            foreach (var id in SystemAPI.Query<RefRO<NetworkId>>().WithAll<NetworkStreamInGame>())
                activeIds.Add(id.ValueRO.Value);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (owner, e) in SystemAPI
                         .Query<RefRO<GhostOwner>>()
                         .WithAll<PlayerTag>()
                         .WithEntityAccess())
            {
                if (!activeIds.Contains(owner.ValueRO.NetworkId))
                {
                    ecb.DestroyEntity(e);
#if UNITY_DEBUG
                    Debug.Log($"[Server] Despawn player (orphaned owner netId={owner.ValueRO.NetworkId}).");
#endif
                }
            }

            ecb.Playback(s.EntityManager);
            activeIds.Dispose();
        }
    }
}