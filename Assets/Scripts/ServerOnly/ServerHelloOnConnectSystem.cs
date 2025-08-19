// Assets/Scripts/ServerOnly/ServerHelloOnConnectSystem.cs
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Game.Shared.Net;
using Game.Shared.Util;

namespace Game.Server
{
    public struct HelloSentTag : IComponentData {}

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ServerHelloOnConnectSystem : ISystem
    {
        public void OnUpdate(ref SystemState s)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (id, e) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamConnection>()
                         .WithNone<HelloSentTag>()
                         .WithEntityAccess())
            {
                var msg = ecb.CreateEntity();
                ecb.AddComponent(msg, new ServerHelloRpc());
                ecb.AddComponent(msg, new SendRpcCommandRequest { TargetConnection = e });

                ecb.AddComponent<HelloSentTag>(e);
                s.LogInfo("RPC", "Hello → netId={0}", id.ValueRO.Value);
            }

            ecb.Playback(s.EntityManager);
        }
    }
}