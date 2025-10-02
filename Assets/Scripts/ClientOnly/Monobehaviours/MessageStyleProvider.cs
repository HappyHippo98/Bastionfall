using Shared.UI;
using UnityEngine;
using ClientOnly.ScriptableObjects;

namespace ClientOnly.Monobehaviours
{
    public class MessageStyleProvider : MonoBehaviour
    {
        [SerializeField] private MessageStyleSetSO styleSet;

        public MessageStyleSO Get(MessageType t)
        {
            if (styleSet == null) return null;
            return styleSet.Get(t);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            // Kannst du im Inspector zuweisen
        }
#endif
    }
}