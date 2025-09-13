using Shared.Authoring.Network;
using Shared.Network;
using Shared.Logging;
using Shared.Rpc;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ClientOnly
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    public partial struct ClientAutoConnectSystem : ISystem
    {
        double _nextAttempt;
        bool _wasConnected, _inGameSent;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            state.RequireForUpdate<NetRuntimeConfig>();
            _nextAttempt = 0;
            _wasConnected = false;
            _inGameSent = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var now = SystemAPI.Time.ElapsedTime;
            var cfg = SystemAPI.GetSingleton<NetRuntimeConfig>();

            // Verbunden?
            bool connected = SystemAPI.QueryBuilder().WithAll<NetworkId, NetworkStreamConnection>().Build()
                                 .CalculateEntityCount() > 0;

            if (!connected)
            {
                _wasConnected = false;
                _inGameSent = false;

                if (now >= _nextAttempt)
                {
                    var e = em.CreateEntity();
                    em.AddComponentData(e, new NetworkStreamRequestConnect { Endpoint = cfg.ServerEndpoint });
                    DevLog.InfoSystem<ClientAutoConnectSystem>("Client", $"Try connect {cfg.ServerEndpoint} …");
                    _nextAttempt = now + cfg.RetrySeconds; // aus NetRuntimeConfig
                }
                return;
            }

            // Gerade eben verbunden -> einmalig loggen
            if (!_wasConnected)
            {
                _wasConnected = true;
                DevLog.InfoSystem<ClientAutoConnectSystem>("Client", "Connected.");
            }

            // InGame-Handschlag einmalig senden (Client->Server: KEIN TargetConnection setzen)
            if (!_inGameSent)
            {
                var rpc = em.CreateEntity();
                em.AddComponentData(rpc, new GoInGameRpc());
                em.AddComponentData(rpc, new SendRpcCommandRequest());
                _inGameSent = true;
                DevLog.InfoSystem<ClientAutoConnectSystem>("Client", "Sent GoInGameRpc.");
            }
        }
    }
}
