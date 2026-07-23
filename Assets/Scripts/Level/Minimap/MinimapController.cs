using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Level.Generation.Graph;
using Level.Generation.Layout;

namespace Level.Minimap
{
    public class MinimapController : MonoBehaviour
    {
        [Header("Corner mode")]
        public Vector2 cornerSize = new Vector2(220f, 220f);
        public Vector2 cornerMargin = new Vector2(20f, 20f);

        [Header("Fullscreen mode")]
        public Vector2 fullscreenSize = new Vector2(720f, 720f);

        [Header("Style")]
        public Color entryColor = new Color(0.4f, 0.8f, 1f, 1f);
        public Color bossColor = new Color(1f, 0.35f, 0.35f, 1f);
        public Color hallColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        public Color treasureColor = new Color(1f, 0.9f, 0.35f, 1f);
        public Color shopColor = new Color(0.55f, 1f, 0.55f, 1f);
        public Color secretColor = new Color(0.75f, 0.5f, 1f, 1f);
        public Color restColor = new Color(0.55f, 0.95f, 0.95f, 1f);
        public Color npcColor = new Color(0.95f, 0.75f, 0.55f, 1f);
        public Color defaultColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        public Color edgeColor = new Color(1f, 1f, 1f, 0.6f);
        public Color playerColor = Color.white;

        private Canvas _canvas;
        private RectTransform _panel;
        private RectTransform _content;
        private RectTransform _playerMarker;
        private AssembledLevel _level;
        private Transform _player;
        private bool _fullscreen;

        private readonly List<RoomMarker> _roomMarkers = new List<RoomMarker>();
        private readonly Dictionary<Zone, List<RoomMarker>> _markersByZone = new Dictionary<Zone, List<RoomMarker>>();
        private readonly HashSet<Zone> _explored = new HashSet<Zone>();
        private readonly List<EdgeMarker> _edgeMarkers = new List<EdgeMarker>();

        private class RoomMarker
        {
            public PlacedRoom room;
            public Zone zone;
            public RectTransform rt;
            public Image image;
        }

        private class EdgeMarker
        {
            public ZoneEdge edge;
            public RectTransform rt;
            public Image image;
        }

        private Vector2Int _worldMin;
        private Vector2Int _worldMax;

        public void Initialize(AssembledLevel level, Transform player)
        {
            _level = level;
            _player = player;
            BuildCanvas();
            ComputeWorldBounds();
            BuildGraphUI();
            ApplyLayout();
        }

        public void ToggleFullscreen()
        {
            if (_panel == null) return;
            _fullscreen = !_fullscreen;
            ApplyLayout();
        }

        private void Update()
        {
            if (_player == null || _playerMarker == null) return;
            var world = new Vector2(_player.position.x, _player.position.y);
            _playerMarker.anchoredPosition = WorldToMinimap(world);
        }

        public void MarkZoneExplored(Zone zone)
        {
            if (zone == null || _explored.Contains(zone)) return;
            _explored.Add(zone);
            if (_markersByZone.TryGetValue(zone, out var markers))
            {
                foreach (var m in markers)
                {
                    var c = m.image.color;
                    c.a = 1f;
                    m.image.color = c;
                }
            }
            foreach (var em in _edgeMarkers)
            {
                if (em.edge.a != zone && em.edge.b != zone) continue;
                var c = em.image.color;
                c.a = edgeColor.a;
                em.image.color = c;
            }
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("MinimapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panelGo = new GameObject("MinimapPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            _panel = panelGo.GetComponent<RectTransform>();
            var bg = panelGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(panelGo.transform, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = Vector2.zero;
            _content.anchorMax = Vector2.one;
            _content.offsetMin = new Vector2(8f, 8f);
            _content.offsetMax = new Vector2(-8f, -8f);
        }

        private void ComputeWorldBounds()
        {
            _worldMin = new Vector2Int(int.MaxValue, int.MaxValue);
            _worldMax = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var room in _level.rooms)
            {
                var pos = room.worldPosition;
                var size = room.footprint.size;
                if (pos.x < _worldMin.x) _worldMin.x = pos.x;
                if (pos.y < _worldMin.y) _worldMin.y = pos.y;
                if (pos.x + size.x > _worldMax.x) _worldMax.x = pos.x + size.x;
                if (pos.y + size.y > _worldMax.y) _worldMax.y = pos.y + size.y;
            }
        }

        private void BuildGraphUI()
        {
            foreach (var edge in _level.graph.edges) BuildEdgeMarker(edge);

            foreach (var room in _level.rooms)
            {
                var zone = GetZoneForRoom(room);
                if (zone == null) continue;
                BuildRoomMarker(room, zone);
            }

            var playerGo = new GameObject("Player", typeof(RectTransform), typeof(Image));
            playerGo.transform.SetParent(_content, false);
            _playerMarker = playerGo.GetComponent<RectTransform>();
            _playerMarker.sizeDelta = new Vector2(10f, 10f);
            var img = playerGo.GetComponent<Image>();
            img.color = playerColor;
            img.raycastTarget = false;
        }

        private Zone GetZoneForRoom(PlacedRoom room)
        {
            foreach (var zone in _level.graph.zones)
            {
                if (zone.composite == null) continue;
                foreach (var node in zone.composite.nodes)
                    if (node == room.sourceNode) return zone;
            }
            return null;
        }

        private void BuildRoomMarker(PlacedRoom room, Zone zone)
        {
            var go = new GameObject($"Room_{zone.id}_{room.sourceNode.localId}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_content, false);
            var rt = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            var color = ColorForType(zone.type);
            color.a = 0f;
            img.color = color;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var marker = new RoomMarker { room = room, zone = zone, rt = rt, image = img };
            _roomMarkers.Add(marker);
            if (!_markersByZone.TryGetValue(zone, out var list))
            {
                list = new List<RoomMarker>();
                _markersByZone[zone] = list;
            }
            list.Add(marker);
        }

        private void BuildEdgeMarker(ZoneEdge edge)
        {
            var go = new GameObject($"Edge_{edge.a.id}_{edge.b.id}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_content, false);
            var rt = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();
            var color = edgeColor;
            color.a = 0f;
            img.color = color;
            img.raycastTarget = false;
            rt.name += "_line";
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            _edgeMarkers.Add(new EdgeMarker { edge = edge, image = img, rt = rt });
        }

        private Color ColorForType(ZoneType type)
        {
            switch (type)
            {
                case ZoneType.Entry: return entryColor;
                case ZoneType.Boss: return bossColor;
                case ZoneType.Hall: return hallColor;
                case ZoneType.Treasure: return treasureColor;
                case ZoneType.Shop: return shopColor;
                case ZoneType.SecretRoom: return secretColor;
                case ZoneType.RestArea: return restColor;
                case ZoneType.NpcRoom: return npcColor;
                default: return defaultColor;
            }
        }

        private void ApplyLayout()
        {
            if (_panel == null) return;

            if (_fullscreen)
            {
                _panel.anchorMin = new Vector2(0.5f, 0.5f);
                _panel.anchorMax = new Vector2(0.5f, 0.5f);
                _panel.pivot = new Vector2(0.5f, 0.5f);
                _panel.sizeDelta = fullscreenSize;
                _panel.anchoredPosition = Vector2.zero;
            }
            else
            {
                _panel.anchorMin = new Vector2(1f, 1f);
                _panel.anchorMax = new Vector2(1f, 1f);
                _panel.pivot = new Vector2(1f, 1f);
                _panel.sizeDelta = cornerSize;
                _panel.anchoredPosition = new Vector2(-cornerMargin.x, -cornerMargin.y);
            }

            var scale = GetContentScale();
            foreach (var m in _roomMarkers)
            {
                var size = m.room.footprint.size;
                var pos = m.room.worldPosition;
                m.rt.sizeDelta = new Vector2(size.x * scale, size.y * scale);
                m.rt.anchoredPosition = WorldToMinimap(new Vector2(pos.x + size.x * 0.5f, pos.y + size.y * 0.5f));
            }

            LayoutEdges();
        }

        private void LayoutEdges()
        {
            if (_level == null || _level.graph == null) return;
            var zoneCenters = new Dictionary<Zone, Vector2>();
            foreach (var kv in _markersByZone)
            {
                if (kv.Value.Count == 0) continue;
                var sum = Vector2.zero;
                foreach (var m in kv.Value)
                {
                    var size = m.room.footprint.size;
                    var pos = m.room.worldPosition;
                    sum += new Vector2(pos.x + size.x * 0.5f, pos.y + size.y * 0.5f);
                }
                zoneCenters[kv.Key] = sum / kv.Value.Count;
            }

            foreach (var em in _edgeMarkers)
            {
                if (!zoneCenters.ContainsKey(em.edge.a) || !zoneCenters.ContainsKey(em.edge.b)) continue;
                var a = WorldToMinimap(zoneCenters[em.edge.a]);
                var b = WorldToMinimap(zoneCenters[em.edge.b]);
                var delta = b - a;
                em.rt.sizeDelta = new Vector2(delta.magnitude, 2f);
                em.rt.anchoredPosition = (a + b) * 0.5f;
                em.rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        private float GetContentScale()
        {
            var contentSize = _fullscreen ? fullscreenSize - new Vector2(16f, 16f) : cornerSize - new Vector2(16f, 16f);
            var worldSize = new Vector2(_worldMax.x - _worldMin.x, _worldMax.y - _worldMin.y);
            if (worldSize.x <= 0f || worldSize.y <= 0f) return 1f;
            return Mathf.Min(contentSize.x / worldSize.x, contentSize.y / worldSize.y);
        }

        private Vector2 WorldToMinimap(Vector2 world)
        {
            var scale = GetContentScale();
            var worldCenter = new Vector2((_worldMin.x + _worldMax.x) * 0.5f, (_worldMin.y + _worldMax.y) * 0.5f);
            return (world - worldCenter) * scale;
        }
    }
}
