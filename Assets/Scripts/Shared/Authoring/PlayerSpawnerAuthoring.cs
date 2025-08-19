using Unity.Entities;
using UnityEngine;

namespace Game.Shared.Authoring
{
    public struct PlayerSpawner : IComponentData
    {
        public Entity PlayerPrefab;
    }

    [DisallowMultipleComponent]
    public class PlayerSpawnerAuthoring : MonoBehaviour
    {
        public GameObject PlayerPrefab;

        class Baker : Unity.Entities.Baker<PlayerSpawnerAuthoring>
        {
            public override void Bake(PlayerSpawnerAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new PlayerSpawner {
                    PlayerPrefab = GetEntity(a.PlayerPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}