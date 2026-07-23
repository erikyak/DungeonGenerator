using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Level.Generation.Support;

namespace Level.Generation.Scoring
{
    public class MetricsDashboard : MonoBehaviour
    {
        [Header("Setup")]
        public GenerationConfig config;
        public int attemptCount = 30;

        [Header("Layout")]
        public Vector2 panelSize = new Vector2(720f, 640f);
        public int histogramBinCount = 12;

        [Header("Colors")]
        public Color panelColor = new Color(0f, 0f, 0f, 0.85f);
        public Color barColor = new Color(0.4f, 0.85f, 1f, 1f);
        public Color textColor = Color.white;
        public Color axisColor = new Color(1f, 1f, 1f, 0.5f);

        private Canvas _canvas;
        private RectTransform _panel;
        private RectTransform _content;
        private Text _summaryText;
        private readonly List<HistogramWidget> _widgets = new List<HistogramWidget>();
        private MultiAttemptGenerator _generator;
        private ScoringFunction _scoring;

        private void Awake()
        {
            _scoring = new ScoringFunction();
            _generator = new MultiAttemptGenerator();
            BuildUI();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("MetricsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panelGo = new GameObject("DashboardPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            _panel = panelGo.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0f, 0f);
            _panel.anchorMax = new Vector2(0f, 0f);
            _panel.pivot = new Vector2(0f, 0f);
            _panel.sizeDelta = panelSize;
            _panel.anchoredPosition = new Vector2(20f, 20f);
            var bg = panelGo.GetComponent<Image>();
            bg.color = panelColor;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 30f);
            titleRt.anchoredPosition = new Vector2(0f, -8f);
            var titleText = titleGo.GetComponent<Text>();
            titleText.text = "Metrics Dashboard";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = textColor;
            titleText.fontSize = 20;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var buttonGo = new GameObject("RunButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(panelGo.transform, false);
            var btnRt = buttonGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 1f);
            btnRt.anchorMax = new Vector2(0f, 1f);
            btnRt.pivot = new Vector2(0f, 1f);
            btnRt.sizeDelta = new Vector2(180f, 36f);
            btnRt.anchoredPosition = new Vector2(12f, -50f);
            buttonGo.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f, 1f);
            var btnLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            btnLabelGo.transform.SetParent(buttonGo.transform, false);
            var lblRt = btnLabelGo.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
            var lblText = btnLabelGo.GetComponent<Text>();
            lblText.text = $"Run {attemptCount} attempts";
            lblText.alignment = TextAnchor.MiddleCenter;
            lblText.color = textColor;
            lblText.fontSize = 15;
            lblText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonGo.GetComponent<Button>().onClick.AddListener(RunAttempts);

            var summaryGo = new GameObject("Summary", typeof(RectTransform), typeof(Text));
            summaryGo.transform.SetParent(panelGo.transform, false);
            var sumRt = summaryGo.GetComponent<RectTransform>();
            sumRt.anchorMin = new Vector2(0f, 1f);
            sumRt.anchorMax = new Vector2(1f, 1f);
            sumRt.pivot = new Vector2(0f, 1f);
            sumRt.sizeDelta = new Vector2(-220f, 40f);
            sumRt.anchoredPosition = new Vector2(210f, -50f);
            _summaryText = summaryGo.GetComponent<Text>();
            _summaryText.text = "No data.";
            _summaryText.alignment = TextAnchor.MiddleLeft;
            _summaryText.color = textColor;
            _summaryText.fontSize = 13;
            _summaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var contentGo = new GameObject("Histograms", typeof(RectTransform));
            contentGo.transform.SetParent(panelGo.transform, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 0f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.offsetMin = new Vector2(12f, 12f);
            _content.offsetMax = new Vector2(-12f, -100f);

            CreateHistogramWidget("Score", 0);
            CreateHistogramWidget("Compactness", 1);
            CreateHistogramWidget("Avg corridor length", 2);
            CreateHistogramWidget("Total turns", 3);
            CreateHistogramWidget("Room count", 4);
        }

        private void CreateHistogramWidget(string title, int slot)
        {
            var widgetGo = new GameObject($"Hist_{title}", typeof(RectTransform), typeof(Image));
            widgetGo.transform.SetParent(_content, false);
            widgetGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
            var rt = widgetGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            float widgetHeight = 90f;
            rt.sizeDelta = new Vector2(0f, widgetHeight);
            rt.anchoredPosition = new Vector2(0f, -slot * (widgetHeight + 6f));

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(widgetGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 18f);
            titleRt.anchoredPosition = new Vector2(6f, -2f);
            var titleText = titleGo.GetComponent<Text>();
            titleText.text = title;
            titleText.color = textColor;
            titleText.fontSize = 12;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var barsGo = new GameObject("Bars", typeof(RectTransform));
            barsGo.transform.SetParent(widgetGo.transform, false);
            var barsRt = barsGo.GetComponent<RectTransform>();
            barsRt.anchorMin = new Vector2(0f, 0f);
            barsRt.anchorMax = new Vector2(1f, 1f);
            barsRt.offsetMin = new Vector2(6f, 4f);
            barsRt.offsetMax = new Vector2(-6f, -22f);

            _widgets.Add(new HistogramWidget
            {
                title = title,
                bars = barsRt,
                metricSlot = slot
            });
        }

        private void RunAttempts()
        {
            if (config == null)
            {
                _summaryText.text = "No config assigned.";
                return;
            }

            var prevFilter = Debug.unityLogger.filterLogType;
            Debug.unityLogger.filterLogType = LogType.Exception;
            MultiAttemptGenerator.Result best;
            List<MultiAttemptGenerator.Result> attempts;
            try
            {
                best = _generator.GenerateBest(config, _scoring, attemptCount);
                attempts = _generator.allAttempts;
            }
            finally
            {
                Debug.unityLogger.filterLogType = prevFilter;
            }

            if (attempts.Count == 0)
            {
                _summaryText.text = "All attempts failed.";
                return;
            }

            float avgScore = 0f;
            foreach (var a in attempts) avgScore += a.score;
            avgScore /= attempts.Count;
            _summaryText.text = $"Runs: {attempts.Count} | best score: {best.score:F1} (seed {best.seed}) | avg: {avgScore:F1}";

            foreach (var w in _widgets)
                UpdateHistogram(w, attempts);
        }

        private void UpdateHistogram(HistogramWidget widget, List<MultiAttemptGenerator.Result> attempts)
        {
            for (int i = widget.bars.childCount - 1; i >= 0; i--)
                DestroyImmediate(widget.bars.GetChild(i).gameObject);

            var values = new float[attempts.Count];
            for (int i = 0; i < attempts.Count; i++)
                values[i] = SelectMetric(attempts[i], widget.metricSlot);

            float min = float.MaxValue, max = float.MinValue;
            foreach (var v in values) { if (v < min) min = v; if (v > max) max = v; }
            if (Mathf.Approximately(min, max)) max = min + 1f;

            var bins = new int[histogramBinCount];
            foreach (var v in values)
            {
                int idx = Mathf.Clamp(Mathf.FloorToInt((v - min) / (max - min) * histogramBinCount), 0, histogramBinCount - 1);
                bins[idx]++;
            }
            int maxBin = 1;
            foreach (var b in bins) if (b > maxBin) maxBin = b;

            for (int i = 0; i < histogramBinCount; i++)
            {
                float h = (float)bins[i] / maxBin;
                var barGo = new GameObject($"Bar_{i}", typeof(RectTransform), typeof(Image));
                barGo.transform.SetParent(widget.bars, false);
                var img = barGo.GetComponent<Image>();
                img.color = barColor;
                var rt = barGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2((float)i / histogramBinCount, 0f);
                rt.anchorMax = new Vector2((float)(i + 1) / histogramBinCount, h);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.offsetMin = new Vector2(1f, 0f);
                rt.offsetMax = new Vector2(-1f, 0f);
            }
        }

        private float SelectMetric(MultiAttemptGenerator.Result result, int slot)
        {
            switch (slot)
            {
                case 0: return result.score;
                case 1: return result.metrics.compactness;
                case 2: return result.metrics.avgCorridorLength;
                case 3: return result.metrics.totalTurns;
                case 4: return result.metrics.roomCount;
                default: return 0f;
            }
        }

        private class HistogramWidget
        {
            public string title;
            public RectTransform bars;
            public int metricSlot;
        }
    }
}
