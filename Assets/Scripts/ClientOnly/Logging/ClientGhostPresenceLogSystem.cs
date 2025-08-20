// Assets/Scripts/ClientOnly/ClientGhostPresenceLogSystem.cs
#if UNITY_DEBUG
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Unity.Transforms;
using Game.Shared.Authoring;
using Game.Shared.Util;

namespace Game.Client
{
    public struct GhostCountDebug : IComponentData { public int All, Local; }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ClientGhostPresenceLogSystem : ISystem
    {
        public void OnCreate(ref SystemState s)
        {
            s.EntityManager.CreateSingleton(new GhostCountDebug { All = -1, Local = -1 });
        }

        public void OnUpdate(ref SystemState s)
        {
            ref var dbg = ref SystemAPI.GetSingletonRW<GhostCountDebug>().ValueRW;

            var qAll   = SystemAPI.QueryBuilder().WithAll<PlayerTag>().Build();
            var qLocal = SystemAPI.QueryBuilder().WithAll<PlayerTag, GhostOwnerIsLocal>().Build();

            int all   = qAll.CalculateEntityCount();
            int local = qLocal.CalculateEntityCount();

            if (all != dbg.All || local != dbg.Local)
            {
                s.LogInfo("Ghosts", "PlayerGhosts total={0}, local={1}", all, local);
                dbg.All = all; dbg.Local = local;
            }
        }
    }
}
#endif