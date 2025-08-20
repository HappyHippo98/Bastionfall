// Assets/Scripts/ClientOnly/ClientLogOnHaveIdSystem.cs
#if UNITY_DEBUG
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Game.Shared.Util;

namespace Game.Client
{
    public struct ClientConnSeenTag : IComponentData {}
    public struct ClientInGameLoggedTag : IComponentData {}

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ClientLogOnHaveIdSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var em  = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var q = SystemAPI.QueryBuilder()
                .WithAll<NetworkId>()
                .WithNone<ClientConnSeenTag>()
                .Build();

            using var ents = q.ToEntityArray(Allocator.Temp);
            foreach (var e in ents)
            {
                var id = em.GetComponentData<NetworkId>(e).Value;
                state.LogInfo("Conn", "Have NetworkId={0}", id);
                ecb.AddComponent<ClientConnSeenTag>(e);
            }
            ecb.Playback(em);
        }
    }
}
#endif
