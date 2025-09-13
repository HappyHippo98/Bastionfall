using Unity.Entities;
using UnityEngine;

namespace Shared.Authoring
{
    public class PlayerPrefabAuthoring : MonoBehaviour
    {
        [Tooltip("Ghost-Player-Prefab (hat GhostAuthoringComponent).")]
        public GameObject playerGhostPrefab;

        class Baker : Baker<PlayerPrefabAuthoring>
        {
            public override void Bake(PlayerPrefabAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.None);
                var prefabEntity = GetEntity(a.playerGhostPrefab, TransformUsageFlags.Dynamic);
                AddComponent(e, new PlayerPrefabRef { Prefab = prefabEntity });
            }
        }
    }

    public struct PlayerPrefabRef : IComponentData
    {
        public Entity Prefab;
    }
}