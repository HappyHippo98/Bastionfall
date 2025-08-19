// Assets/Scripts/ServerOnly/ServerReadyLoggingSystem.cs
using Unity.Entities;
using Unity.NetCode;
using Game.Shared.Util; // für state.LogInfo(...)

namespace Game.Server
{
    // Nur im Server-Simulations-World laufen lassen
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ServerReadyLoggingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // Erst updaten, wenn der Driver existiert (dann sind wir "wirklich" bereit)
            state.RequireForUpdate<NetworkStreamDriver>();
            state.LogInfo("Bootstrap", fmt: "ServerReadyLoggingSystem ONLINE");
        }

        public void OnUpdate(ref SystemState state)
        {
            // Wenn wir hier sind, existiert NetworkStreamDriver -> bereit für Verbindungen
            state.LogInfo("Bootstrap", fmt: "Ready to accept connections.");
            state.Enabled = false; // einmaliges Logging reicht
        }
    }
}