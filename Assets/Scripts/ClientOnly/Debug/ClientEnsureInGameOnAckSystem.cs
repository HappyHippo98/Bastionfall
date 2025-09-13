using Shared.Authoring.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ClientOnly.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientEnsureInGameOnAckSystem : ISystem
    {
        public void OnCreate(ref SystemState s)
        {
            s.RequireForUpdate<EnableNetcode>();
        }

        public void OnUpdate(ref SystemState s)
        {
            var em  = s.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Wichtig: keine strukturellen Änderungen per EntityManager in der Schleife – nur ECB!
            foreach (var (_, e) in SystemAPI
                         .Query<NetworkSnapshotAck>()
                         .WithAll<NetworkStreamConnection>()
                         .WithNone<NetworkStreamInGame>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<NetworkStreamInGame>(e);
                // (Optional) Loggen:
                // Debug.Log("[ClientWorld] Forced InGame via SnapshotAck.");
            }

            ecb.Playback(em);
            ecb.Dispose();
        }
    }
}