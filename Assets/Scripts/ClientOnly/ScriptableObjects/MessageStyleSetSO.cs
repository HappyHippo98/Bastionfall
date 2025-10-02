using System.Collections.Generic;
using Shared.UI;
using UnityEngine;

namespace ClientOnly.ScriptableObjects
{
    [CreateAssetMenu(fileName = "MessageStyleSet", menuName = "Bastionfall/Chat/Message Style Set", order = 11)]
    public class MessageStyleSetSO : ScriptableObject
    {
        public List<MessageStyleSO> styles = new();

        public MessageStyleSO Get(MessageType t)
        {
            for (int i = 0; i < styles.Count; i++)
            {
                if (styles[i] != null && styles[i].type == t)
                    return styles[i];
            }
            return null;
        }
    }
}