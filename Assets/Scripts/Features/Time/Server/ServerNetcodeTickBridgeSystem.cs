using BastionFall.Core.Shared.Authoring;
using BastionFall.Features.Time.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace BastionFall.Features.Time.Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ServerNetcodeTickBridgeSystem : ISystem
    {
        public void OnCreate(ref SystemState s)
        {
            s.RequireForUpdate<EnableNetcode>();
            if (!SystemAPI.TryGetSingleton<NetcodeTickState>(out _))
            {
                var e = s.EntityManager.CreateEntity(typeof(NetcodeTickState));
                s.EntityManager.SetComponentData(e, new NetcodeTickState { ServerTick = 0 });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState s)
        {
            if (!SystemAPI.TryGetSingletonRW<NetcodeTickState>(out var stRW)) return;
            if (!SystemAPI.TryGetSingleton<NetworkTime>(out var nt)) return;
            if (!nt.ServerTick.IsValid) return;

            stRW.ValueRW.ServerTick = nt.ServerTick.TickIndexForValidTick;
        }
    }
}