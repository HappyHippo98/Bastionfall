using System;

namespace Game.Shared.Util
{
    public static class CommandLine
    {
        public static bool TryGetString(string key, out string value)
        {
            value = null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-" + key, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    value = args[i + 1];
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetUShort(string key, out ushort value)
        {
            value = 0;
            if (TryGetString(key, out var s) && ushort.TryParse(s, out var p))
            {
                value = p;
                return true;
            }
            return false;
        }
    }
}