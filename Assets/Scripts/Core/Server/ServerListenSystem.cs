using BastionFall.Core.Shared.Authoring;
using BastionFall.Core.Shared.Logging;
using BastionFall.Core.Shared.Network;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace BastionFall.Core.Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ServerListenSystem : ISystem
    {
        private bool _requested;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            state.RequireForUpdate<NetRuntimeConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_requested) return;

            var cfg = SystemAPI.GetSingleton<NetRuntimeConfig>();
            var endpoint = NetworkEndpoint.AnyIpv4.WithPort(cfg.Port);

            var e = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(e, new NetworkStreamRequestListen { Endpoint = endpoint });

            _requested = true;
            DevLog.InfoSystem<ServerListenSystem>("Server", $"Listening on 0.0.0.0:{cfg.Port} …");
        }
    }
}