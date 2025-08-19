// Assets/Scripts/ServerOnly/ServerConnectionLoggingSystem.cs
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Game.Shared.Util; // für state.LogInfo(...)

namespace Game.Server
{
    // Marker-Komponenten, um jede Phase nur einmal zu loggen
    public struct LoggedNewConnection : IComponentData {}
    public struct LoggedNetworkId   : IComponentData {}
    public struct LoggedInGame      : IComponentData {}
    // public struct LoggedDisconnected : IComponentData {} // optional, siehe unten

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [CreateAfter(typeof(NetworkStreamReceiveSystem))]
    public partial struct ServerConnectionLoggingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.LogInfo("Bootstrap", fmt: "ServerConnectionLoggingSystem ONLINE");
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 1) Neue Connection-Entities (noch ohne NetworkId)
            foreach (var (_, e) in SystemAPI
                         .Query<RefRO<NetworkStreamConnection>>()
                         .WithNone<LoggedNewConnection>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<LoggedNewConnection>(e);
                state.LogInfo("Conn", fmt: "New connection entity created (awaiting NetworkId).");
            }

            // 2) NetworkId zum ersten Mal vergeben
            foreach (var (id, e) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamConnection>()
                         .WithNone<LoggedNetworkId>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<LoggedNetworkId>(e);
                state.LogInfo("Conn", fmt: $"Assigned NetworkId={id.ValueRO.Value}.");
            }

            // 3) InGame gesetzt
            foreach (var (id, e) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamConnection, NetworkStreamInGame>()
                         .WithNone<LoggedInGame>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<LoggedInGame>(e);
                state.LogInfo("Conn", fmt: $"Connection is InGame (netId={id.ValueRO.Value}).");
            }

            // 4) Optional: Disconnects loggen.
            // In vielen NetCode-Versionen gibt es die public-Tag-Komponente `NetworkStreamDisconnected`.
            // Falls dein Paket sie enthält, kannst du den folgenden Block ent-kommentieren
            // UND die Marker-Komponente LoggedDisconnected (oben) aktivieren:
            /*
            foreach (var (id, e) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamDisconnected>()
                         .WithNone<LoggedDisconnected>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<LoggedDisconnected>(e);
                state.LogInfo("Conn", fmt: $"Disconnected (netId={id.ValueRO.Value}).");
            }
            */

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
