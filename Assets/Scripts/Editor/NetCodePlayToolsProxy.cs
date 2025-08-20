// Assets/Scripts/Editor/NetCodePlayToolsProxy.cs

using System;
using System.Linq;
using System.Reflection;

namespace Game.Editor
{
    public static class NetCodePlayToolsProxy
    {
        // Versucht, in Unity.NetCode.Editor die PlayTools auf Client zu stellen.
        public static bool TrySetClient()  => TrySet("Client");
        public static bool TrySetServer()  => TrySet("Server");
        public static bool TrySetClientAndServer() => TrySet("ClientAndServer");

        static bool TrySet(string enumName)
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Unity.NetCode.Editor");
                if (asm == null) return false;

                var tools = asm.GetType("Unity.NetCode.Editor.NetCodePlayModeTools")
                            ?? asm.GetType("Unity.NetCode.Editor.PlayModeTools")
                            ?? asm.GetType("Unity.NetCode.Editor.NetCodeBootstrapTools");
                if (tools == null) return false;

                // Enum-Typ finden
                var enumType =
                    asm.GetType("Unity.NetCode.Editor.PlayType") ??
                    asm.GetType("Unity.NetCode.Editor.NetCodePlayType") ??
                    asm.GetTypes().FirstOrDefault(t => t.IsEnum && t.Name.Contains("Play", StringComparison.OrdinalIgnoreCase));

                object enumVal = enumType != null ? Enum.Parse(enumType, enumName, true) : null;

                // Property 'RequestedPlayType'
                var prop = tools.GetProperty("RequestedPlayType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && enumVal != null) { prop.SetValue(null, enumVal); return true; }

                // Methode 'SetRequestedPlayType(enum)'
                var meth = tools.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name.Contains("SetRequestedPlayType") && m.GetParameters().Length == 1);
                if (meth != null && enumVal != null) { meth.Invoke(null, new[] { enumVal }); return true; }

                // Fallback: Property 'PlayType'
                var prop2 = tools.GetProperty("PlayType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop2 != null && enumVal != null) { prop2.SetValue(null, enumVal); return true; }
            }
            catch { /* still ok */ }

            return false;
        }
    }
}
