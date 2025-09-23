using System;
using System.IO;
using Shared.Logging;
using Shared.Network;
using Shared.Time;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Scripting;

namespace Shared.Bootstrap
{
    [Preserve]
    public class NetworkBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            NetArgs.ParseOnce();

            ConfigureLogSinks();

            if (NetArgs.IsServer)
            {
                var server = CreateServerWorld("ServerWorld");
                EnsureTickRate(server);
                EnsureSingleRuntimeConfig(server);
                AppLog.For("Bootstrap", "NetworkBootstrap").Info($"ServerWorld created: {server.Name}");
            }

            if (NetArgs.IsClient && !NetArgs.NoGui)
            {
                var client = CreateClientWorld("ClientWorld");
                EnsureTickRate(client);
                EnsureSingleRuntimeConfig(client);
                AppLog.For("Bootstrap", "NetworkBootstrap").Info($"ClientWorld created: {client.Name}");
            }

            for (var i = 0; i < NetArgs.ThinClientCount; i++)
            {
                var thin = CreateThinClientWorld();
                EnsureTickRate(thin);
                EnsureSingleRuntimeConfig(thin);
                AppLog.For("Bootstrap", "NetworkBootstrap").Info($"ThinClientWorld created: {thin.Name}");
            }

            AppLog.For("Bootstrap", "NetworkBootstrap")
                .Info(
                    $"Args => server={NetArgs.IsServer}, client={NetArgs.IsClient}, thin={NetArgs.ThinClientCount}, host={NetArgs.Host}, port={NetArgs.Port}, nogui={NetArgs.NoGui}, unitycapture={NetArgs.UnityCapture}, apploglevel={NetArgs.AppLogLevel}, unityloglevel={NetArgs.UnityLogLevel}");

            return true;
        }

        private static void ConfigureLogSinks()
        {
            var role = NetArgs.IsServer && !NetArgs.IsClient && NetArgs.ThinClientCount == 0 ? "server"
                : !NetArgs.IsServer && (NetArgs.IsClient || NetArgs.ThinClientCount > 0) ? "client"
                : "multi";

            // Standardbasis für Logs:
            // - Editor: persistentDataPath/Logs
            // - Player DevelopmentBuild: <ExeFolder>/Logs
            // - Player Release: persistentDataPath/Logs
            string baseDir;
            if (!Application.isEditor && Debug.isDebugBuild)
            {
                // <ExeFolder> = Parent von "<ExeName>_Data"
                var exeDir = Directory.GetParent(Application.dataPath)!.FullName;
                baseDir = Path.Combine(exeDir, "Logs");
            }
            else
            {
                baseDir = Path.Combine(Application.persistentDataPath, "Logs");
            }

            var appPath = string.IsNullOrWhiteSpace(NetArgs.AppLogFile)
                ? Path.Combine(baseDir, $"app-{role}-{DateTime.Now:yyyyMMdd-HHmmss}.log")
                : NetArgs.AppLogFile;

            var unityPath = string.IsNullOrWhiteSpace(NetArgs.UnityLogFile)
                ? Path.Combine(baseDir, $"unity-{role}-{DateTime.Now:yyyyMMdd-HHmmss}.log")
                : NetArgs.UnityLogFile;

            AppLog.ConfigureAppFile(appPath, NetArgs.AppLogLevel);
            AppLog.ConfigureUnityFile(unityPath, NetArgs.UnityLogLevel);
            AppLog.SetUnityCapture(NetArgs.UnityCapture);
            AppLog.HookUnityCapture();

            AppLog.For("Bootstrap", "NetworkBootstrap")
                .Info($"Log files → app: '{AppLog.AppFilePath}', unity: '{AppLog.UnityFilePath}'");
        }

        private static void EnsureTickRate(World world)
        {
            var em = world.EntityManager;
            var hz = (int)math.round(TickDefaults.SystemTicksPerSecond);
            var rate = new ClientServerTickRate
            {
                SimulationTickRate = hz,
                NetworkTickRate = hz,
                MaxSimulationStepsPerFrame = 4
            };

            var q = em.CreateEntityQuery(ComponentType.ReadWrite<ClientServerTickRate>());
            if (q.CalculateEntityCount() == 0)
            {
                var e = em.CreateEntity(typeof(ClientServerTickRate));
                em.SetComponentData(e, rate);
            }
            else
            {
                em.SetComponentData(q.GetSingletonEntity(), rate);
            }

            q.Dispose();
        }

        private static void EnsureSingleRuntimeConfig(World world)
        {
            var em = world.EntityManager;
            var q = em.CreateEntityQuery(ComponentType.ReadWrite<NetRuntimeConfig>());
            var cfg = new NetRuntimeConfig
            {
                ServerEndpoint = NetworkEndpoint.Parse(NetArgs.Host, NetArgs.Port),
                Port = NetArgs.Port,
                RetrySeconds = NetConfig.RetrySeconds
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
                for (var i = 1; i < entities.Length; i++) em.DestroyEntity(entities[i]);
                entities.Dispose();
            }

            q.Dispose();
        }
    }
}