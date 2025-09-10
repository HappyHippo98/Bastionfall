using System;
using System.Linq;
using Shared.Network;

namespace Shared.Bootstrap
{
    /// <summary>Minimaler Argument-Parser für Bootstrap-Optionen.</summary>
    public static class NetArgs
    {
        public static bool IsServer { get; private set; }
        public static bool IsClient { get; private set; }
        public static int ThinClientCount { get; private set; }
        public static string Host { get; private set; } = NetConfig.DefaultHost;
        public static ushort Port { get; private set; } = NetConfig.DefaultPort;
        public static bool NoGui { get; private set; }

        public static void ParseOnce()
        {
            if (_parsed) return;
            _parsed = true;

            var args = Environment.GetCommandLineArgs();

            bool Has(string flag)
            {
                for (int i = 0; i < args.Length; i++)
                    if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }

            string Val(string flag)
            {
                for (int i = 0; i < args.Length - 1; i++)
                    if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                        return args[i + 1];
                return null;
            }

            IsServer = Has("-server");
            IsClient = Has("-client");
            NoGui    = Has("-nogui");

            if (int.TryParse(Val("-thinclients"), out var thin) && thin > 0)
                ThinClientCount = thin;

            var portStr = Val("-port");
            if (ushort.TryParse(portStr, out var p) && p > 0) Port = p;

            var hostStr = Val("-host");
            if (!string.IsNullOrWhiteSpace(hostStr)) Host = hostStr;

#if UNITY_EDITOR
            if (!IsServer && !IsClient && ThinClientCount == 0)
            {
                IsServer = true;
                IsClient = true;
            }
#else
            if (!IsServer && !IsClient && ThinClientCount == 0)
                IsClient = true;
#endif
        }

        private static bool _parsed;
    }
}
