using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BastionFall.Features.Player.Shared.Authoring.Monitoring
{
    public sealed class PlayerIdentityAuthoring : MonoBehaviour
    {
        private sealed class Baker : Baker<PlayerIdentityAuthoring>
        {
            public override void Bake(PlayerIdentityAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new PlayerIdentity
                {
                    DisplayName = default,
                    Guid = default
                });
            }
        }
    }


    [GhostComponent(PrefabType = GhostPrefabType.All)]
    public struct PlayerIdentity : IComponentData
    {
        [GhostField] public FixedString64Bytes DisplayName;
        [GhostField] public FixedString128Bytes Guid;
    }
}