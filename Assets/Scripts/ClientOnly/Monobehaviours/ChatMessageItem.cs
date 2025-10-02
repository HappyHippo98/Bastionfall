using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ClientOnly.ScriptableObjects;

namespace ClientOnly.Monobehaviours
{
    public class ChatMessageItem : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Image    background;
        [SerializeField] private TMP_Text authorText;
        [SerializeField] private TMP_Text contentText;

        // Ziel-Farben aus dem Style (ohne Fade)
        private Color _bgTarget, _authorTarget, _contentTarget;

        // Fade-Dauern
        private float _fadeIn;
        private float _hold;
        private float _fadeOut;

        private Coroutine _fadeRoutine;

        public void Setup(string author, string content, MessageStyleSO style)
        {
            if (authorText)  authorText.text  = author;
            if (contentText) contentText.text = content;
            ApplyStyle(style);
        }

        public void ApplyStyle(MessageStyleSO style)
        {
            if (style == null) return;

            // Ziel-Farben übernehmen (mit voller, im Style definierter Alpha)
            _bgTarget      = style.background;
            _authorTarget  = style.authorColor;
            _contentTarget = style.contentColor;

            // Fonts/Typo
            if (authorText)
            {
                if (style.authorFont) authorText.font = style.authorFont;
                authorText.fontSize   = style.authorFontSize;
                authorText.fontStyle  = style.authorFontStyle; // TMP enum
            }

            if (contentText)
            {
                if (style.contentFont) contentText.font = style.contentFont;
                contentText.fontSize   = style.contentFontSize;
                contentText.fontStyle  = style.contentFontStyle; // TMP enum
            }

            _fadeIn  = Mathf.Max(0f, style.fadeIn);
            _hold    = Mathf.Max(0f, style.hold);
            _fadeOut = Mathf.Max(0f, style.fadeOut);
        }

        public void Play()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(Run());
        }

        public void KillImmediate()
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;

            // Sofort unsichtbar machen
            if (background) background.color = WithAlpha(_bgTarget, 0f);
            if (authorText) authorText.color = WithAlpha(_authorTarget, 0f);
            if (contentText) contentText.color = WithAlpha(_contentTarget, 0f);

            gameObject.SetActive(false);
        }

        private IEnumerator Run()
        {
            // Start: Alpha 0
            if (background) background.color = WithAlpha(_bgTarget, 0f);
            if (authorText) authorText.color = WithAlpha(_authorTarget, 0f);
            if (contentText) contentText.color = WithAlpha(_contentTarget, 0f);

            // Fade In
            if (_fadeIn > 0f)
            {
                float t = 0f;
                while (t < _fadeIn)
                {
                    t += Time.deltaTime;
                    float a = Mathf.Clamp01(t / _fadeIn);
                    ApplyAlpha(a);
                    yield return null;
                }
            }
            else
            {
                ApplyAlpha(1f);
            }

            // Hold
            float hold = _hold;
            while (hold > 0f)
            {
                hold -= Time.deltaTime;
                yield return null;
            }

            // Fade Out
            if (_fadeOut > 0f)
            {
                float t = 0f;
                while (t < _fadeOut)
                {
                    t += Time.deltaTime;
                    float a = Mathf.Clamp01(1f - (t / _fadeOut));
                    ApplyAlpha(a);
                    yield return null;
                }
            }
            else
            {
                ApplyAlpha(0f);
            }

            gameObject.SetActive(false);
        }

        public void ApplyAlpha(float a)
        {
            if (background) background.color = WithAlpha(_bgTarget, a);
            if (authorText) authorText.color = WithAlpha(_authorTarget, a);
            if (contentText) contentText.color = WithAlpha(_contentTarget, a);
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
