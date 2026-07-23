using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Level.Generation.Support;

namespace Level.Generation.Runtime
{
    public class ProfileSelector : MonoBehaviour
    {
        [Serializable]
        public class Profile
        {
            public string name = "Profile";
            public GenerationConfig config;
        }

        [Header("Setup")]
        public DungeonGenerator generator;
        public List<Profile> profiles = new List<Profile>();

        [Header("Layout")]
        public Vector2 panelSize = new Vector2(220f, 260f);
        public Vector2 panelMargin = new Vector2(20f, 20f);

        [Header("Colors")]
        public Color panelColor = new Color(0f, 0f, 0f, 0.6f);
        public Color buttonColor = new Color(0.2f, 0.5f, 0.8f, 1f);
        public Color activeButtonColor = new Color(0.9f, 0.6f, 0.2f, 1f);
        public Color textColor = Color.white;

        private Canvas _canvas;
        private RectTransform _panel;
        private readonly List<Image> _buttonImages = new List<Image>();
        private int _activeIndex = -1;

        private void Start()
        {
            BuildUI();
            if (generator != null && generator.config != null)
            {
                for (int i = 0; i < profiles.Count; i++)
                    if (profiles[i].config == generator.config) { HighlightActive(i); break; }
            }
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("ProfileSelectorCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 150;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            _panel = panelGo.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0f, 1f);
            _panel.anchorMax = new Vector2(0f, 1f);
            _panel.pivot = new Vector2(0f, 1f);
            _panel.sizeDelta = panelSize;
            _panel.anchoredPosition = new Vector2(panelMargin.x, -panelMargin.y);
            panelGo.GetComponent<Image>().color = panelColor;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 28f);
            titleRt.anchoredPosition = new Vector2(0f, -6f);
            var titleText = titleGo.GetComponent<Text>();
            titleText.text = "Profiles";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = textColor;
            titleText.fontSize = 16;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            float y = -40f;
            for (int i = 0; i < profiles.Count; i++)
            {
                int idx = i;
                var buttonGo = new GameObject($"Btn_{profiles[i].name}", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonGo.transform.SetParent(panelGo.transform, false);
                var btnRt = buttonGo.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0f, 1f);
                btnRt.anchorMax = new Vector2(1f, 1f);
                btnRt.pivot = new Vector2(0.5f, 1f);
                btnRt.sizeDelta = new Vector2(-16f, 36f);
                btnRt.anchoredPosition = new Vector2(0f, y);
                var img = buttonGo.GetComponent<Image>();
                img.color = buttonColor;
                _buttonImages.Add(img);

                var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                lblGo.transform.SetParent(buttonGo.transform, false);
                var lblRt = lblGo.GetComponent<RectTransform>();
                lblRt.anchorMin = Vector2.zero;
                lblRt.anchorMax = Vector2.one;
                lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
                var lblText = lblGo.GetComponent<Text>();
                lblText.text = profiles[i].name;
                lblText.color = textColor;
                lblText.alignment = TextAnchor.MiddleCenter;
                lblText.fontSize = 14;
                lblText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                buttonGo.GetComponent<Button>().onClick.AddListener(() => SelectProfile(idx));
                y -= 42f;
            }
        }

        public void SelectProfile(int idx)
        {
            if (idx < 0 || idx >= profiles.Count) return;
            if (generator == null || profiles[idx].config == null) return;
            generator.config = profiles[idx].config;
            HighlightActive(idx);
            generator.Regenerate();
        }

        private void HighlightActive(int idx)
        {
            _activeIndex = idx;
            for (int i = 0; i < _buttonImages.Count; i++)
                _buttonImages[i].color = (i == idx) ? activeButtonColor : buttonColor;
        }
    }
}
