using BastionFall.Features.Chat.Client.UI.ScriptableObjects;
using BastionFall.Features.Chat.Shared.Constants;
using UnityEngine;

namespace BastionFall.Features.Chat.Client.UI
{
    public class MessageStyleProvider : MonoBehaviour
    {
        [SerializeField] private MessageStyleSetSo styleSet;

#if UNITY_EDITOR
        private void Reset()
        {
            // Kannst du im Inspector zuweisen
        }
#endif

        public MessageStyleSO Get(MessageType t)
        {
            if (styleSet == null) return null;
            return styleSet.Get(t);
        }
    }
}