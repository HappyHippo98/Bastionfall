using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Shared.Authoring.Monitoring
{
    public class PlayerIdentityAuthoring : MonoBehaviour
    {
        public string defaultName = "Player";

        class Baker : Baker<PlayerIdentityAuthoring>
        {
            public override void Bake(PlayerIdentityAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic); // Ghost wird gespawned
                AddComponent(e, new PlayerIdentity
                {
                    Guid = default, // Server füllt via RPC
                    Name = a.defaultName
                });
            }
        }
    }

    // Replizierte Ghost-Daten (Server-autorisiert)
    public struct PlayerIdentity : IComponentData
    {
        [GhostField] public FixedString128Bytes Guid;
        [GhostField] public FixedString64Bytes  Name;
    }
}