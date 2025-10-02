using Shared.UI;
using UnityEngine;

namespace ClientOnly.Monobehaviours
{
    public class ChatFeedUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private ChatMessageItem templateItem;
        [SerializeField] private MessageStyleProvider styleProvider;

        [Header("Limits")]
        [SerializeField] private int maxOnScreen = 50;

        private void Awake()
        {
            if (templateItem) templateItem.gameObject.SetActive(false);
        }

        public void SetTyping(bool isTyping)
        {
            foreach(Transform child in contentRoot)
            {
                ChatMessageItem cm = child.GetComponent<ChatMessageItem>();
                if(cm!=null)
                {
                    cm.ApplyAlpha(255);
                }
                child.gameObject.SetActive(isTyping);
            }
        }

        public void AddItem(string author, string text, MessageType type)
        {
            if (contentRoot == null || templateItem == null) return;

            var style = styleProvider ? styleProvider.Get(type) : null;

            var go = Instantiate(templateItem.gameObject, contentRoot);
            go.SetActive(true);

            var item = go.GetComponent<ChatMessageItem>();
            item.Setup(author, text, style);
            item.Play();

            Trim(maxOnScreen);
        }

        private void Trim(int max)
        {
            int count = contentRoot.childCount;
            for (int i = 0; i < count - max; i++)
            {
                var child = contentRoot.GetChild(i);
                var item = child.GetComponent<ChatMessageItem>();
                if (item) item.KillImmediate();
                Destroy(child.gameObject);
            }
        }
    }
}