// Assets/Scripts/Shared/Net/MultiplayerBootstrap.cs
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Scripting; // Preserve
using Game.Shared.Config;
using Game.Shared.Util;

namespace Game.Shared.Net
{
    [Preserve]
    public class MultiplayerBootstrap : ClientServerBootstrap
    {
        // Frühester Marker: nur in Debug/Editor loggen
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void EarlyMarker()
        {
#if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log("[Bootstrap] Discovered MultiplayerBootstrap (BeforeSplashScreen)");
#endif
        }

        public override bool Initialize(string defaultWorldName)
        {
#if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log("[Bootstrap] Initialize() entered");
#endif
            var s = Resources.Load<NetGameSettings>("NetGameSettings");

            // Defaults aus Settings
            string address = s && !string.IsNullOrWhiteSpace(s.ConnectAddress) ? s.ConnectAddress.Trim() : "127.0.0.1";
            ushort port    = s && s.Port != 0 ? s.Port : (ushort)7979;
            var    mode    = s ? s.PlayerRunMode : RunMode.Client;
            bool   preferListenWhenLocal = s ? s.PreferListenWhenLocal : true;

#if UNITY_EDITOR
            if (s)
            {
                switch (s.PlayProfile)
                {
                    case EditorPlayProfile.DedicatedServer:
                        mode = RunMode.Server;
                        break;
                    case EditorPlayProfile.DedicatedLocalServer:
                        mode = RunMode.Server; address = "127.0.0.1";
                        break;
                    default:
                        mode = RunMode.Client;
                        break;
                }
            }
#endif
            // CLI-Overrides + Fallback
            if (CommandLine.TryGetString("mode", out var m1) && !string.IsNullOrWhiteSpace(m1)) mode = ParseMode(m1);
            else if (TryGetArgEnv("mode", out var m2)) mode = ParseMode(m2);

            if (CommandLine.TryGetString("address", out var a1) && !string.IsNullOrWhiteSpace(a1)) address = a1.Trim();
            else if (TryGetArgEnv("address", out var a2)) address = a2.Trim();

            if (CommandLine.TryGetUShort("port", out var p1) && p1 != 0) port = p1;
            else if (TryGetArgEnv("port", out var p2) && ushort.TryParse(p2, out var p3) && p3 != 0) port = p3;

            ExtractPortFromAddressSafely(ref address, ref port);
            if (port == 0) { port = 7979; Debug.LogWarning("[Bootstrap] Port was 0 – fallback=7979"); }

#if !UNITY_EDITOR && !UNITY_SERVER
            if (mode == RunMode.AutoDetect)
            {
                bool isLocal = IsLoopbackOrPrivate(address);
                mode = (isLocal && preferListenWhenLocal) ? RunMode.Listen : RunMode.Client;
            }
#endif

#if UNITY_EDITOR
            TrySetRequestedPlayType(mode);
#endif

            AutoConnectPort = port;
            switch (mode)
            {
                case RunMode.Listen:
                    DefaultConnectAddress = NetworkEndpoint.LoopbackIpv4.WithPort(port);
                    DefaultListenAddress  = NetworkEndpoint.AnyIpv4.WithPort(port);
                    break;
                case RunMode.Client:
                    DefaultConnectAddress = SafeParse(address, port);
                    DefaultListenAddress  = NetworkEndpoint.AnyIpv4.WithPort(port);
                    break;
                case RunMode.Server:
                default:
                    DefaultConnectAddress = NetworkEndpoint.LoopbackIpv4.WithPort(port);
                    DefaultListenAddress  = NetworkEndpoint.AnyIpv4.WithPort(port);
                    break;
            }

            if (DefaultConnectAddress.Port == 0 && (mode == RunMode.Client || mode == RunMode.Listen))
            {
                DefaultConnectAddress = SafeParse(address, port);
                AutoConnectPort = port;
                Debug.LogWarning($"[Bootstrap] Connect had port=0 – fixed -> {address}:{port}");
            }

#if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log($"[Bootstrap] Mode={mode} Address={address} Port={port}");
            Debug.Log($"[Bootstrap] Connect={DefaultConnectAddress} Listen={DefaultListenAddress} AutoConnectPort={AutoConnectPort}");
#endif

            switch (mode)
            {
                case RunMode.Server:
                    CreateServerWorld(defaultWorldName);
                    return true;
                case RunMode.Client:
                    CreateClientWorld(defaultWorldName);
                    return true;
                case RunMode.Listen:
                    CreateServerWorld(defaultWorldName + "-Server");
                    CreateClientWorld(defaultWorldName + "-Client");
                    return true;
                default:
                    CreateClientWorld(defaultWorldName);
                    return true;
            }
        }

        // helpers (unverändert) ...
        static RunMode ParseMode(string m) => m.ToLowerInvariant() switch
        {
            "server" => RunMode.Server, "client" => RunMode.Client,
            "listen" => RunMode.Listen, "auto" => RunMode.AutoDetect, _ => RunMode.Client
        };

        static bool TryGetArgEnv(string key, out string value)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i=0;i<args.Length-1;i++)
                if (string.Equals(args[i].TrimStart('-','/'), key, StringComparison.OrdinalIgnoreCase))
                { value = args[i+1]; return true; }
            value = null; return false;
        }

        static void ExtractPortFromAddressSafely(ref string address, ref ushort port)
        {
            var idx = address.LastIndexOf(':');
            if (idx > 0 && idx == address.IndexOf(':'))
            {
                var right = address[(idx+1)..];
                if (ushort.TryParse(right, out var p) && p != 0)
                { address = address[..idx]; port = p; }
            }
        }

        static bool IsLoopbackOrPrivate(string host)
        {
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;
            if (IPAddress.TryParse(host, out var ip))
            {
                var b = ip.GetAddressBytes();
                if (b.Length==4)
                { if (b[0]==127 || b[0]==10 || (b[0]==172 && b[1]>=16 && b[1]<=31) || (b[0]==192 && b[1]==168)) return true; }
            }
            return false;
        }

        static NetworkEndpoint SafeParse(string host, ushort port)
        {
            try { return NetworkEndpoint.Parse(host, port); }
            catch
            {
                Debug.LogWarning($"[Bootstrap] Parse failed for '{host}:{port}', using loopback.");
                return NetworkEndpoint.LoopbackIpv4.WithPort(port==0 ? (ushort)7979 : port);
            }
        }

#if UNITY_EDITOR
        static void TrySetRequestedPlayType(RunMode mode)
        {
            try
            {
                var t = typeof(ClientServerBootstrap);
                var prop = t.GetProperty("RequestedPlayType", BindingFlags.Public | BindingFlags.Static);
                if (prop != null && prop.CanWrite)
                {
                    string want = mode switch
                    {
                        RunMode.Listen => "ClientAndServer",
                        RunMode.Server => "Server",
                        _ => "Client"
                    };
                    var enumVal = Enum.Parse(prop.PropertyType, want);
                    prop.SetValue(null, enumVal);
                }
            } catch { /* ignore in editor */ }
        }
#endif
    }
}
