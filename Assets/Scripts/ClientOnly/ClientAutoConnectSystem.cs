using Shared.Authoring.Network;
using Shared.Network;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ClientOnly
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    public partial struct ClientAutoConnectSystem : ISystem
    {
        private double _nextAttempt;
        private bool _wasConnected;

        [BurstCompile] public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            state.RequireForUpdate<NetRuntimeConfig>();
            _nextAttempt = 0;
            _wasConnected = false;
        }

        [BurstCompile] public void OnUpdate(ref SystemState state)
        {
            var time = SystemAPI.Time.ElapsedTime;

            bool connected = SystemAPI.QueryBuilder()
                .WithAll<NetworkId>()
                .Build()
                .CalculateEntityCount() > 0;

            if (connected && !_wasConnected)
            {
                _wasConnected = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.Log("[Client] Connected.");
#endif
                return;
            }

            if (!connected && time >= _nextAttempt)
            {
                _wasConnected = false;

                var cfg = SystemAPI.GetSingleton<NetRuntimeConfig>();
                var e = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(e, new NetworkStreamRequestConnect { Endpoint = cfg.ServerEndpoint });

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.Log($"[Client] Try connect {cfg.ServerEndpoint.Address}:{cfg.ServerEndpoint.Port} …");
#endif
                _nextAttempt = time + cfg.RetrySeconds;
            }
        }
    }
}