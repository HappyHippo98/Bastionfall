// Assets/Scripts/ServerOnly/ServerInGameLoggingSystem.cs
using Unity.Entities;
using Unity.NetCode;
using Game.Shared.Util;

namespace Game.Server
{
    // Loggt genau dann, wenn der Server eine Connection als InGame markiert
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