// Assets/Scripts/ClientOnly/ClientSetCommandTargetSystem.cs

using Game.Shared.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Game.Shared.Util;

namespace Game.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ClientSetCommandTargetSystem : ISystem
    {
        private bool _done;

        public void OnUpdate(ref SystemState s)
        {
            if (_done) return;

            var em = s.EntityManager;

            var connQ = SystemAPI.QueryBuilder().WithAll<NetworkStreamConnection, NetworkId>().Build();
            if (connQ.IsEmpty) return;

            var conn = connQ.GetSingletonEntity();

            var localQ = SystemAPI.QueryBuilder()
                .WithAll<GhostOwnerIsLocal, PlayerInput>()
                .Build();

            using var ents = localQ.ToEntityArray(Allocator.Temp);
            if (ents.Length == 0) return;

            var target = ents[0];
            var ct = em.HasComponent<CommandTarget>(conn) ? em.GetComponentData<CommandTarget>(conn) : new CommandTarget();

            if (ct.targetEntity != target)
            {
                ct.targetEntity = target;
                em.SetComponentData(conn, ct);
                s.LogInfo("Client", $"Set CommandTarget → {target.Index}");
                _done = true;
            }
        }
    }
}