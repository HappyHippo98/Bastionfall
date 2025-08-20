// Assets/Scripts/Shared/Net/RegisterGhostsFromRegistrySystem.cs
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Game.Shared.Authoring;
using Game.Shared.Util;

namespace Game.Shared.Net
{
    public struct GhostsRegisteredTag : IComponentData {}

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct RegisterGhostsFromRegistrySystem : ISystem
    {
        public void OnCreate(ref SystemState s)
        {
            s.RequireForUpdate<GhostCollection>();
            s.RequireForUpdate<GhostRegistry>();
        }

        public void OnUpdate(ref SystemState s)
        {
            var regEnt = SystemAPI.GetSingletonEntity<GhostRegistry>();
            var list   = s.EntityManager.GetBuffer<GhostRegistryEntry>(regEnt);

            s.LogInfo("Ghosts", "Registering {0} ghost prefab(s) from registry...", list.Length);

            foreach (var entry in list)
            {
                FixedString64Bytes name =
                    s.EntityManager.HasComponent<GhostName>(entry.Prefab)
                        ? s.EntityManager.GetComponentData<GhostName>(entry.Prefab).Value
                        : (FixedString64Bytes)"Ghost";

                s.LogInfo("Ghosts", "Register '{0}'", name);

                var cfg = new GhostPrefabCreation.Config { Name = name };
                GhostPrefabCreation.ConvertToGhostPrefab(s.EntityManager, entry.Prefab, cfg);
            }

            if (!SystemAPI.HasSingleton<GhostsRegisteredTag>())
            {
                var tag = s.EntityManager.CreateEntity();
                s.EntityManager.AddComponent<GhostsRegisteredTag>(tag);
            }

            s.Enabled = false;
        }
    }
}