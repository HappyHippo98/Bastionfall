using System;
using Unity.Mathematics;

namespace Shared.Chat
{
    public enum ChatCommandType
    {
        None = 0,
        ChangeColor = 1,
    }

    public static class ChatCommandParser
    {
        public static bool TryParse(string text, out ChatCommandType type, out string arg)
        {
            type = ChatCommandType.None;
            arg = null;
            if (string.IsNullOrWhiteSpace(text) || text[0] != '/') return false;

            var parts = text.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();
            arg = parts.Length > 1 ? parts[1] : string.Empty;

            switch (cmd)
            {
                case "/changecolor":
                case "/color":
                case "/colour":
                    type = ChatCommandType.ChangeColor;
                    return true;
                default:
                    return false;
            }
        }
    }

    public static class ColorUtil
    {
        public static bool TryParseColor(string s, out float4 rgba)
        {
            rgba = new float4(1, 1, 1, 1);
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().ToLowerInvariant();

            if (s[0] == '#')
            {
                var hex = s.Substring(1);
                if (hex.Length is 6 or 8 &&
                    uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var val))
                {
                    float r, g, b, a = 1f;
                    if (hex.Length == 6)
                    {
                        r = ((val >> 16) & 0xFF) / 255f;
                        g = ((val >> 8) & 0xFF) / 255f;
                        b = (val & 0xFF) / 255f;
                    }
                    else
                    {
                        r = ((val >> 24) & 0xFF) / 255f;
                        g = ((val >> 16) & 0xFF) / 255f;
                        b = ((val >> 8) & 0xFF) / 255f;
                        a = (val & 0xFF) / 255f;
                    }
                    rgba = new float4(r, g, b, a); return true;
                }
                return false;
            }

            switch (s)
            {
                case "red": rgba = new float4(1,0,0,1); return true;
                case "green": rgba = new float4(0,1,0,1); return true;
                case "blue": rgba = new float4(0,0,1,1); return true;
                case "yellow": rgba = new float4(1,1,0,1); return true;
                case "cyan": rgba = new float4(0,1,1,1); return true;
                case "magenta": rgba = new float4(1,0,1,1); return true;
                case "white": rgba = new float4(1,1,1,1); return true;
                case "black": rgba = new float4(0,0,0,1); return true;
                case "orange": rgba = new float4(1,0.5f,0,1); return true;
                case "purple": rgba = new float4(0.5f,0,0.5f,1); return true;
                case "pink": rgba = new float4(1,0.4f,0.7f,1); return true;
            }

            var parts = s.Split(',');
            if (parts.Length is 3 or 4)
            {
                float[] vals = new float[parts.Length];
                for (int i=0;i<parts.Length;i++)
                    if (!float.TryParse(parts[i], out vals[i])) return false;

                bool scale255 = vals[0] > 1.5f || vals[1] > 1.5f || vals[2] > 1.5f || (parts.Length==4 && vals[3] > 1.5f);
                var r = scale255 ? vals[0]/255f : vals[0];
                var g = scale255 ? vals[1]/255f : vals[1];
                var b = scale255 ? vals[2]/255f : vals[2];
                var a = parts.Length==4 ? (scale255 ? vals[3]/255f : vals[3]) : 1f;
                rgba = new float4(r,g,b,a); return true;
            }

            return false;
        }
    }
}
