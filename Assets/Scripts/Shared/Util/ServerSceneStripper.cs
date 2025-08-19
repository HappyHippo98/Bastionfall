using UnityEngine;

namespace Game.Shared.Util
{
    public static class ServerSceneStripper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StripForServer()
        {
            // Nur in echten Dedicated/Headless-Starts säubern
            if (!Application.isBatchMode && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;

            // Kamera/Audio/Follow-Skripte entfernen, falls sie in der Szene sind
            foreach (var cam in Object.FindObjectsOfType<Camera>())
                Object.Destroy(cam);

            foreach (var al in Object.FindObjectsOfType<AudioListener>())
                Object.Destroy(al);

            // Falls die Client-Komponente doch mitgebaut wurde, ebenfalls entfernen
            var type = System.Type.GetType("Game.Client.FollowLocalPlayer, Game.Client");
            if (type != null)
            {
                foreach (var c in Object.FindObjectsOfType(type, true))
                    Object.Destroy((Object)c);
            }
        }
    }
}
