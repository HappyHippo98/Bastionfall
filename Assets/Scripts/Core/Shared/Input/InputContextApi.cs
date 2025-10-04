using Unity.Entities;
using UnityEngine;
using BastionFall.Core.Shared.Authoring;
using Unity.NetCode;

namespace BastionFall.Core.Shared.Input
{
    
    public static class InputContextApi
    {
        public static void Set(InputContext next, World world = null)
        {
            world ??= ClientServerBootstrap.ClientWorld ?? World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[InputContextApi] No world available to set context '{next}'.");
#endif
                return;
            }

            var em = world.EntityManager;
            var e  = em.CreateEntity(typeof(InputContextChangeRequest));
            em.SetComponentData(e, new InputContextChangeRequest { Next = next });
        }
    }
}