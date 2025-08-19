// Assets/Scripts/Shared/Config/NetGameSettings.cs
using UnityEngine;

namespace Game.Shared.Config
{
    public enum EditorPlayProfile
    {
        Client,
        DedicatedServer,
        DedicatedLocalServer
    }

    public enum RunMode
    {
        AutoDetect,
        Client,
        Server,
        Listen
    }

    [CreateAssetMenu(menuName = "Bastionfall/NetGameSettings", fileName = "NetGameSettings")]
    public class NetGameSettings : ScriptableObject
    {
        [Header("Editor")]
        public EditorPlayProfile PlayProfile = EditorPlayProfile.Client;

        [Tooltip("Nur Editor-Hinweis: Wenn aktiv, setzt der Inspector die NetCode-PlayTools auf 'Client'.")]
        public bool ForceClientPlayType = true;

        [Header("Network")]
        public string ConnectAddress = "127.0.0.1";
        public ushort Port = 7979;

        [Header("Server Only")]
        public bool DespawnOnDisconnect = true;

        [Header("Runtime Defaults (non-Editor)")]
        public RunMode PlayerRunMode = RunMode.Client;

        [Tooltip("Bei AutoDetect: Lokale Ziele -> ListenServer statt Client.")]
        public bool PreferListenWhenLocal = true;
    }
}