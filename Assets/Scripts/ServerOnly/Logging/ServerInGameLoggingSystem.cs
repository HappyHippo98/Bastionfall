// Assets/Scripts/ServerOnly/ServerInGameLoggingSystem.cs
#if UNITY_DEBUG
using Game.Shared.Util;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly.Logging
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ServerInGameLoggingSystem : ISystem
    {
        public void OnUpdate(ref SystemState s)
        {
            foreach (var (netId, e) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>()
                         .WithEntityAccess()
                         .WithChangeFilter<NetworkStreamInGame>())
            {
                s.LogInfo("Conn", "Server marked connection InGame (netId={0})", netId.ValueRO.Value);
            }
        }
    }
}
#endif