// Assets/Scripts/ClientOnly/ClientLogOnInGameSystem.cs
#if UNITY_DEBUG
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Game.Shared.Util;

namespace Game.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ClientLogOnHaveIdSystem))]
    public partial struct ClientLogOnInGameSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var em  = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var q = SystemAPI.QueryBuilder()
                .WithAll<NetworkId, NetworkStreamInGame>()
                .WithNone<ClientInGameLoggedTag>()
                .Build();

            using var ents = q.ToEntityArray(Allocator.Temp);
            foreach (var e in ents)
            {
                var id = em.GetComponentData<NetworkId>(e).Value;
                state.LogInfo("Conn", "Connection is InGame (netId={0})", id);
                ecb.AddComponent<ClientInGameLoggedTag>(e);
            }
            ecb.Playback(em);
        }
    }
}
#endif