using Shared.Authoring.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial struct ClientDeduplicateNetStreamSystem : ISystem
{
    /*
    public void OnCreate(ref SystemState s) => s.RequireForUpdate<EnableNetcode>();

    public void OnUpdate(ref SystemState s)
    {
        var em  = s.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Requests deduplizieren
        var reqQ = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamRequestConnect>());
        using (var reqs = reqQ.ToEntityArray(Allocator.Temp))
        {
            for (int i = 1; i < reqs.Length; i++) ecb.DestroyEntity(reqs[i]); // ersten behalten, Rest killen
        }

        // Connections deduplizieren
        var conQ = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
        using (var conns = conQ.ToEntityArray(Allocator.Temp))
        {
            for (int i = 1; i < conns.Length; i++)
            {
                // elegant trennen (statt direkt zerstören):
                ecb.AddComponent(conns[i], new NetworkStreamRequestDisconnect { Reason = NetworkStreamDisconnectReason.ClosedByRemote });
            }
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
    */
}