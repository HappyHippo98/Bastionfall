using ClientOnly.Authoring;
using Shared.Authoring.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace ClientOnly
{
    /*
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientAutoConnectSingleShotSystem : ISystem
    {
        private EntityQuery _connQ, _reqQ;

        public void OnCreate(ref SystemState s)
        {
            s.RequireForUpdate<EnableNetcode>();
            _connQ = s.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
            _reqQ  = s.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamRequestConnect>());

            // Singleton anlegen (falls nicht vorhanden)
            if (!s.EntityManager.CreateEntityQuery(typeof(AutoConnectState)).HasSingleton<AutoConnectState>())
            {
                var e = s.EntityManager.CreateEntity();
                s.EntityManager.AddComponentData(e, new AutoConnectState { NextTryAt = 0 });
            }
        }

        public void OnUpdate(ref SystemState s)
        {
            var em = s.EntityManager;

            // Schon verbunden? -> nix tun
            if (!_connQ.IsEmptyIgnoreFilter) return;

            // Bereits ein offener Request? -> nix tun
            if (!_reqQ.IsEmptyIgnoreFilter) return;

            // Cooldown prüfen
            var state = SystemAPI.GetSingletonRW<AutoConnectState>();
            if (SystemAPI.Time.ElapsedTime < state.ValueRO.NextTryAt) return;

            // EINEN Request anlegen
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var req = ecb.CreateEntity();
            ecb.AddComponent(req, new NetworkStreamRequestConnect
            {
                Endpoint = NetworkEndpoint.LoopbackIpv4.WithPort(7979) // <— deinen Port setzen
            });
            ecb.Playback(em);
            ecb.Dispose();

            // Nächsten Versuch frühestens in 2s
            state.ValueRW.NextTryAt = SystemAPI.Time.ElapsedTime + 2.0;

            UnityEngine.Debug.Log("[Client] AutoConnect: connect request issued (cooldown 2s).");
        }
    }
    */
}
