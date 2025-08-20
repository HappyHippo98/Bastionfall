// Assets/Scripts/ServerOnly/ServerConnectionCountMonitorSystem.cs
#if UNITY_DEBUG
using Game.Shared.Util;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly.Logging
{
    public struct ConnectionCountTracker : IComponentData { public int LastCount; }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ServerConnectionCountMonitorSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton(new ConnectionCountTracker { LastCount = -1 });
        }

        public void OnUpdate(ref SystemState state)
        {
            var q = SystemAPI.QueryBuilder().WithAll<NetworkStreamInGame>().Build();
            int current = q.CalculateEntityCount();

            var tracker = SystemAPI.GetSingletonRW<ConnectionCountTracker>();
            if (tracker.ValueRO.LastCount != current)
            {
                state.LogInfo("Conn", "InGame connections: {0}", current);
                tracker.ValueRW.LastCount = current;
            }
        }
    }
}
#endif
