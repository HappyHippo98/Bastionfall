using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Game.Shared.Authoring
{
    public struct PlayerTag : IComponentData {}
    public struct MoveSpeed : IComponentData { public float Value; }

    public struct PlayerInput : IInputComponentData
    {
        public float Horizontal;
        public float Vertical;
        public InputEvent Jump;
    }

    [DisallowMultipleComponent]
    public class PlayerAuthoring : MonoBehaviour
    {
        [Min(0)] public float Speed = 7f;

        // Wichtig: Unity.Entities.Hybrid als Assembly-Reference, und using Unity.Entities oben!
        class Baker : Unity.Entities.Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerTag>(e);
                AddComponent(e, new MoveSpeed { Value = a.Speed });
                AddComponent<PlayerInput>(e);
            }
        }
    }
}