// Assets/Scripts/ClientOnly/ClientRequestInGameSystem.cs
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Game.Shared.Net;

namespace Game.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(Game.Shared.Net.RegisterGhostsFromRegistrySystem))]
    public partial struct ClientRequestInGameSystem : ISystem
    {
        public void OnCreate(ref SystemState s)
        {
            s.RequireForUpdate<NetworkId>();
            s.RequireForUpdate<Game.Shared.Net.GhostsRegisteredTag>();
        }

        public void OnUpdate(ref SystemState s)
        {
            var connQ = SystemAPI.QueryBuilder().WithAll<NetworkStreamConnection>().Build();
            if (connQ.IsEmpty) return;

            var conn = connQ.GetSingletonEntity();

            if (!s.EntityManager.HasComponent<NetworkStreamInGame>(conn))
            {
                s.EntityManager.AddComponent<NetworkStreamInGame>(conn);
#if UNITY_DEBUG
                Debug.Log("[Client][RPC] Marked local connection InGame");
#endif
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var req = ecb.CreateEntity();
            ecb.AddComponent(req, new GoInGameRpc());
            ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = conn });
            ecb.Playback(s.EntityManager);

#if UNITY_DEBUG
            Debug.Log("[Client][RPC] Sent GoInGameRpc");
#endif
            s.Enabled = false; // nur einmal
        }

        public void OnDestroy(ref SystemState state)
        {
#if UNITY_DEBUG
            Debug.Log("On Destroy ClientRequestInGameSystem");
#endif
            state.Enabled = true;
        }
    }
}