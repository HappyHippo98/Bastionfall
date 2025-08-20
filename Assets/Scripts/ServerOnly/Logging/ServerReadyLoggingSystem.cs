// Assets/Scripts/ServerOnly/ServerReadyLoggingSystem.cs
#if UNITY_DEBUG
using Game.Shared.Util;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly.Logging
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ServerReadyLoggingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamDriver>();
            state.LogInfo("Bootstrap", fmt: "ServerReadyLoggingSystem ONLINE");
        }

        public void OnUpdate(ref SystemState state)
        {
            state.LogInfo("Bootstrap", fmt: "Ready to accept connections.");
            state.Enabled = false;
        }
    }
}
#endif