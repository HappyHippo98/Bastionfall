using System.Collections.Generic;
using BastionFall.Features.Chat.Shared.Constants;
using UnityEngine;

namespace BastionFall.Features.Chat.Client.UI.ScriptableObjects
{
    [CreateAssetMenu(fileName = "MessageStyleSet", menuName = "Bastionfall/Chat/Message Style Set", order = 11)]
    public class MessageStyleSetSo : ScriptableObject
    {
        public List<MessageStyleSO> styles = new();

        public MessageStyleSO Get(MessageType t)
        {
            for (var i = 0; i < styles.Count; i++)
                if (styles[i] != null && styles[i].type == t)
                    return styles[i];
            return null;
        }
    }
}