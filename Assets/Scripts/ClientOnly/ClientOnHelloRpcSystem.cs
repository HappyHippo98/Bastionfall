// Assets/Scripts/ClientOnly/ClientOnHelloRpcSystem.cs
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Game.Shared.Net;
using Game.Shared.Util;

namespace Game.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ClientOnHelloRpcSystem : ISystem
    {
        public void OnUpdate(ref SystemState s)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (hello, recv, e) in SystemAPI
                         .Query<RefRO<ServerHelloRpc>, RefRO<ReceiveRpcCommandRequest>>()
                         .WithEntityAccess())
            {
                s.LogInfo("RPC", "Hello from server (RPC arrived)");
                ecb.DestroyEntity(e);
            }

            ecb.Playback(s.EntityManager);
        }
    }
}