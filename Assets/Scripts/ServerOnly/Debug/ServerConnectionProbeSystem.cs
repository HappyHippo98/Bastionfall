using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

namespace ServerOnly.Debugging
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerConnectionProbeSystem : ISystem
    {
        public void OnCreate(ref SystemState s) { }
        public void OnUpdate(ref SystemState s)
        {
            var q = s.GetEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
            using var ents = q.ToEntityArray(Allocator.Temp);
            foreach (var e in ents)
            {
                bool hasId   = s.EntityManager.HasComponent<NetworkId>(e);
                bool inGame  = s.EntityManager.HasComponent<NetworkStreamInGame>(e);
                bool hasAck  = s.EntityManager.HasComponent<NetworkSnapshotAck>(e);
                int  nid     = hasId ? s.EntityManager.GetComponentData<NetworkId>(e).Value : -1;
                Shared.Logging.DevLog.InfoSystem<ServerConnectionProbeSystem>(
                    "Server", $"Conn={e} nid={nid} inGame={inGame} ack={hasAck}");
            }
        }
    }
}