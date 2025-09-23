using Unity.Entities;
using UnityEngine;

namespace Shared.Authoring
{
    public class PlayerPrefabAuthoring : MonoBehaviour
    {
        [Tooltip("Ghost-Player-Prefab (hat GhostAuthoringComponent).")]
        public GameObject playerGhostPrefab;

        private class Baker : Baker<PlayerPrefabAuthoring>
        {
            public override void Bake(PlayerPrefabAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.None);
                var prefabEntity = GetEntity(a.playerGhostPrefab, TransformUsageFlags.Dynamic);
                AddComponent(e, new PlayerPrefab { Prefab = prefabEntity });
            }
        }
    }

    public struct PlayerPrefab : IComponentData
    {
        public Entity Prefab;
    }
}