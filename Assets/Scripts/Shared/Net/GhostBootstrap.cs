using System.Collections.Generic;
using UnityEngine;

namespace Unity.NetCode
{
    public static class GhostBootstrap
    {
        public static bool DidRegister;

        public static void RegisterClientServerGhostPrefabs(List<GameObject> prefabs)
        {
            DidRegister = true;
            var player = Resources.Load<GameObject>("Ghosts/Player");
            if (player != null)
            {
                Debug.Log("[GhostBootstrap] Registered ghost prefab: Ghosts/Player");
                prefabs.Add(player);
            }
            else
            {
                Debug.LogWarning("[GhostBootstrap] Resources/Ghosts/Player.prefab not found!");
            }
        }
    }
}
