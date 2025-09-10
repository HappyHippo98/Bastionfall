using Shared.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Shared.Bootstrap
{
    public class NetworkBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            NetArgs.ParseOnce();

            if (NetArgs.IsServer)
            {
                var server = CreateServerWorld(defaultWorldName);
                EnsureSingleRuntimeConfig(server);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Bootstrap] ServerWorld created: {server.Name}");
#endif
            }

            if (NetArgs.IsClient && !NetArgs.NoGui)
            {
                var client = CreateClientWorld(defaultWorldName);
                EnsureSingleRuntimeConfig(client);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Bootstrap] ClientWorld created: {client.Name}");
#endif
            }

            for (int i = 0; i < NetArgs.ThinClientCount; i++)
            {
                var thin = CreateThinClientWorld();
                EnsureSingleRuntimeConfig(thin);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Bootstrap] ThinClientWorld created: {thin.Name}");
#endif
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Bootstrap] Args => server={NetArgs.IsServer}, client={NetArgs.IsClient}, thin={NetArgs.ThinClientCount}, host={NetArgs.Host}, port={NetArgs.Port}, nogui={NetArgs.NoGui}");
#endif
            return true;
        }

        private static void EnsureSingleRuntimeConfig(World world)
        {
            var em = world.EntityManager;
            var q  = em.CreateEntityQuery(ComponentType.ReadWrite<NetRuntimeConfig>());
            var cfg = new NetRuntimeConfig
            {
                ServerEndpoint = NetworkEndpoint.Parse(NetArgs.Host, NetArgs.Port),
                Port           = NetArgs.Port,
                RetrySeconds   = NetConfig.RetrySeconds
            };

            var count = q.CalculateEntityCount();
            if (count == 0)
            {
                var e = em.CreateEntity(typeof(NetRuntimeConfig));
                em.SetComponentData(e, cfg);
            }
            else
            {
                var entities = q.ToEntityArray(Allocator.Temp);
                em.SetComponentData(entities[0], cfg);
                for (int i = 1; i < entities.Length; i++)
                    em.DestroyEntity(entities[i]);
                entities.Dispose();
            }

            q.Dispose();
        }
    }
}
