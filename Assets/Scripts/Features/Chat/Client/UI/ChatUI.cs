using BastionFall.Features.Chat.Client.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// ChatClientApi

namespace BastionFall.Features.Chat.Client.UI
{
    /// <summary>
    ///     Reine UI-Logik. Öffnen/Schließen wird extern (UiHotkeyBridge) getriggert.
    /// </summary>
    public class ChatUI : MonoBehaviour
    {
        [Header("Refs")] [SerializeField] private GameObject chatInputBar;

        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private ChatFeedUI feed;

        public bool IsTyping =>
            chatInputBar && chatInputBar.activeSelf && chatInput && chatInput.isFocused;

        private void Awake()
        {
            if (chatInputBar) chatInputBar.SetActive(false);
            if (feed) feed.SetTyping(false);

            if (chatInput)
            {
                chatInput.lineType = TMP_InputField.LineType.SingleLine;
                chatInput.characterLimit = 512;
                chatInput.onSubmit.AddListener(OnSubmit);
            }
        }

        private void OnDestroy()
        {
            if (chatInput) chatInput.onSubmit.RemoveListener(OnSubmit);
        }

        public void Open()
        {
            if (!chatInputBar || !chatInput || !feed) return;

            chatInputBar.SetActive(true);
            feed.SetTyping(true);

            chatInput.text = string.Empty;
            chatInput.ActivateInputField();

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(chatInput.gameObject);
        }

        public void Close()
        {
            if (!chatInputBar || !feed) return;

            chatInputBar.SetActive(false);
            feed.SetTyping(false);

            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == chatInput.gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }

        // TMP_InputField.SubmitEvent liefert den Text
        private void OnSubmit(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Close();
                return;
            }

            ChatClientApi.Send(text);
            chatInput.text = string.Empty;
            Close();
        }
    }
}