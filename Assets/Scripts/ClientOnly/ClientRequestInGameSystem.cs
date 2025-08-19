// Assets/Scripts/ClientOnly/ClientRequestInGameSystem.cs
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Game.Shared.Net;

namespace Game.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(Game.Shared.Net.RegisterGhostsFromRegistrySystem))]
    public partial struct ClientRequestInGameSystem : ISystem
    {
        public void OnCreate(ref SystemState s)
        {
            s.RequireForUpdate<NetworkId>();               // Verbindung existiert
            s.RequireForUpdate<Game.Shared.Net.GhostsRegisteredTag>(); // lokale Registrierung fertig
        }

        public void OnUpdate(ref SystemState s)
        {
            var connQ = SystemAPI.QueryBuilder().WithAll<NetworkStreamConnection>().Build();
            if (connQ.IsEmpty) return;

            var conn = connQ.GetSingletonEntity();

            // WICHTIG: Client selbst in den InGame-State bringen (Empfang freischalten)
            if (!s.EntityManager.HasComponent<NetworkStreamInGame>(conn))
            {
                s.EntityManager.AddComponent<NetworkStreamInGame>(conn);
                Debug.Log("[Client][RPC] Marked local connection InGame");
            }

            // RPC an den Server schicken (wie gehabt)
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var req = ecb.CreateEntity();
            ecb.AddComponent(req, new GoInGameRpc());
            ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = conn });
            ecb.Playback(s.EntityManager);

            Debug.Log("[Client][RPC] Sent GoInGameRpc");
            s.Enabled = false; // nur einmal
        }
    }
}