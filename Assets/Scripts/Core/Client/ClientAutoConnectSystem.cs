using BastionFall.Core.Shared.Authoring;
using BastionFall.Core.Shared.Logging;
using BastionFall.Core.Shared.Network;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace BastionFall.Core.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ClientAutoConnectSystem : ISystem
    {
        private double _nextAttempt;
        private EntityQuery _connectReqQ;
        private bool _wasConnected;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            state.RequireForUpdate<NetRuntimeConfig>();
            _connectReqQ = state.GetEntityQuery(ComponentType.ReadOnly<NetworkStreamRequestConnect>());
            _nextAttempt = 0;
            _wasConnected = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var now = SystemAPI.Time.ElapsedTime;
            var cfg = SystemAPI.GetSingleton<NetRuntimeConfig>();

            var hasConn = SystemAPI.QueryBuilder().WithAll<NetworkId, NetworkStreamConnection>().Build()
                .CalculateEntityCount() > 0;

            if (!hasConn)
            {
                _wasConnected = false;
                var requestInFlight = !_connectReqQ.IsEmptyIgnoreFilter;
                if (!requestInFlight && now >= _nextAttempt)
                {
                    var e = em.CreateEntity();
                    em.AddComponentData(e, new NetworkStreamRequestConnect { Endpoint = cfg.ServerEndpoint });
                    DevLog.InfoSystem<ClientAutoConnectSystem>("Client", $"Try connect {cfg.ServerEndpoint} …");
                    _nextAttempt = now + cfg.RetrySeconds;
                }

                return;
            }

            if (!_wasConnected)
            {
                _wasConnected = true;
                DevLog.InfoSystem<ClientAutoConnectSystem>("Client", "Connected.");
            }
        }
    }
}