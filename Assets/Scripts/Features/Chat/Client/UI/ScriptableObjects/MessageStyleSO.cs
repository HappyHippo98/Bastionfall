using BastionFall.Features.Chat.Shared.Constants;
using TMPro;
using UnityEngine;

namespace BastionFall.Features.Chat.Client.UI.ScriptableObjects
{
    [CreateAssetMenu(fileName = "MessageStyle", menuName = "Bastionfall/Chat/Message Style", order = 10)]
    public class MessageStyleSO : ScriptableObject
    {
        [Header("Type")] public MessageType type = MessageType.Info;

        [Header("Colors")] public Color background = new(0, 0, 0, 0.35f);

        public Color authorColor = Color.white;
        public Color contentColor = Color.white;

        [Header("Typography (TMP)")] public TMP_FontAsset authorFont; // optional

        public TMP_FontAsset contentFont; // optional
        public int authorFontSize = 14;
        public int contentFontSize = 14;
        public FontStyles authorFontStyle = FontStyles.Bold; // <-- TMP
        public FontStyles contentFontStyle = FontStyles.Normal; // <-- TMP

        [Header("Timings (seconds)")] public float fadeIn = 0.08f;

        public float hold = 5.0f;
        public float fadeOut = 0.4f;
    }
}