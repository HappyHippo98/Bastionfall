using System;
using Shared.Logging;
using Shared.Network;

namespace Shared.Bootstrap
{
    public static class NetArgs
    {
        private static bool _parsed;
        public static bool IsServer { get; private set; }
        public static bool IsClient { get; private set; }
        public static int ThinClientCount { get; private set; }
        public static string Host { get; private set; } = NetConfig.DefaultHost;
        public static ushort Port { get; private set; } = NetConfig.DefaultPort;
        public static bool NoGui { get; private set; }

        public static string AppLogFile { get; private set; } // -applogfile
        public static string UnityLogFile { get; private set; } // -unitylogfile
        public static bool UnityCapture { get; private set; } = true; // -unitycapture true|false
        public static LogLevel AppLogLevel { get; private set; } = LogLevel.Info; // -apploglevel
        public static LogLevel UnityLogLevel { get; private set; } = LogLevel.Warn; // -unityloglevel

        public static void ParseOnce()
        {
            if (_parsed) return;
            _parsed = true;

            var args = Environment.GetCommandLineArgs();

            bool Has(string flag)
            {
                for (var i = 0; i < args.Length; i++)
                    if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }

            string Val(string flag)
            {
                for (var i = 0; i < args.Length - 1; i++)
                    if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                        return args[i + 1];
                return null;
            }

            IsServer = Has("-server");
            IsClient = Has("-client");
            NoGui = Has("-nogui");

            if (int.TryParse(Val("-thinclients"), out var thin) && thin > 0)
                ThinClientCount = thin;

            if (ushort.TryParse(Val("-port"), out var p) && p > 0) Port = p;

            var hostStr = Val("-host");
            if (!string.IsNullOrWhiteSpace(hostStr)) Host = hostStr;

            AppLogFile = Val("-applogfile");
            UnityLogFile = Val("-unitylogfile");

            var cap = Val("-unitycapture");
            if (!string.IsNullOrWhiteSpace(cap))
                UnityCapture = cap.Trim().ToLowerInvariant() is "1" or "true" or "yes" or "y";

            if (LogLevelUtil.TryParse(Val("-apploglevel"), out var al)) AppLogLevel = al;
            if (LogLevelUtil.TryParse(Val("-unityloglevel"), out var ul)) UnityLogLevel = ul;

#if UNITY_EDITOR
            if (!IsServer && !IsClient && ThinClientCount == 0)
            {
                // Im Editor bequem: beides an, damit du mit Play/Multiplayer Play Mode testen kannst
                IsServer = true;
                IsClient = true;
            }
#elif UNITY_SERVER
    // Dedicated Server Build: ohne Flags -> server only
    if (!IsServer && !IsClient && ThinClientCount == 0)
        IsServer = true;
#else
    // Normaler Player Build: ohne Flags -> client
    if (!IsServer && !IsClient && ThinClientCount == 0)
        IsClient = true;
#endif
        }
    }
}