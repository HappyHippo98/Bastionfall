// Assets/Scripts/ClientOnly/ClientCameraBootstrap.cs
using UnityEngine;

/// <summary>
/// Erzeugt zur Laufzeit eine einfache Kamera + AudioListener + FollowLocalPlayer,
/// aber nur wenn wir NICHT headless/batch laufen (also nur im Client/Listen-Client).
/// </summary>
public static class ClientCameraBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureClientCamera()
    {
        // Auf Dedicated/Headless-Servern keine Kamera erzeugen
        if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            return;

        // Falls bereits eine Kamera existiert, nichts tun
        if (Object.FindObjectOfType<Camera>() != null)
            return;

        var go = new GameObject("Client Camera");
        go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        go.AddComponent<FollowLocalPlayer>();
    }
}