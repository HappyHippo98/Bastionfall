using Unity.Entities;
using UnityEngine;

namespace BastionFall.Core.Shared.Authoring
{
    public class InputContextStateAuthoring : MonoBehaviour
    {
        [Header("InputContext bei Start")]
        public InputContext inputContext = InputContext.InGame;
        private class Baker : Baker<InputContextStateAuthoring>
        {
            public override void Bake(InputContextStateAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(e, new InputContextState
                {
                    Context = a.inputContext
                });
            }
        }
    }
    
    public enum InputContext : byte { InGame, Chat, Inventory }

    public struct InputContextState : IComponentData
    {
        public InputContext Context;
    }
    public struct InputContextChangeRequest : IComponentData
    {
        public InputContext Next;
    }
}