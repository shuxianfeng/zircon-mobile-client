using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zircon.Mobile.Game.Entities;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Map
{
    public sealed class ZirconMiniMapPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconSystemCatalogBehaviour catalog;
        [SerializeField] private RectTransform markerRoot;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform entityMarkerTemplate;
        [SerializeField] private TMP_Text mapText;
        [SerializeField] private TMP_Text coordinateText;
        [SerializeField] private float pixelsPerCell = 3f;
        [SerializeField] private int visibleRadius = 24;

        private readonly Dictionary<uint, RectTransform> markers = new Dictionary<uint, RectTransform>();
        private readonly HashSet<uint> visible = new HashSet<uint>();
        private float nextRefresh;

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.1f;
            Refresh(session?.GetWorldSnapshot());
        }

        private void Refresh(ZirconWorldSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.HasLocalPlayer) return;
            if (mapText != null) mapText.text = catalog?.GetMap(snapshot.MapIndex)?.Description ?? $"Map #{snapshot.MapIndex}";
            if (coordinateText != null) coordinateText.text = $"{snapshot.Location.X}, {snapshot.Location.Y}";
            if (playerMarker != null) playerMarker.anchoredPosition = Vector2.zero;

            visible.Clear();
            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                if (entity.ObjectId == snapshot.LocalPlayer.ObjectId) continue;
                int dx = entity.Location.X - snapshot.Location.X;
                int dy = entity.Location.Y - snapshot.Location.Y;
                if (System.Math.Abs(dx) > visibleRadius || System.Math.Abs(dy) > visibleRadius) continue;
                RectTransform marker = GetMarker(entity.ObjectId);
                if (marker == null) continue;
                marker.gameObject.SetActive(true);
                marker.anchoredPosition = new Vector2(dx * pixelsPerCell, -dy * pixelsPerCell);
                visible.Add(entity.ObjectId);
            }

            foreach (KeyValuePair<uint, RectTransform> pair in markers)
                if (!visible.Contains(pair.Key)) pair.Value.gameObject.SetActive(false);
        }

        private RectTransform GetMarker(uint objectId)
        {
            if (markers.TryGetValue(objectId, out RectTransform marker)) return marker;
            if (markerRoot == null || entityMarkerTemplate == null) return null;
            marker = Instantiate(entityMarkerTemplate, markerRoot);
            markers.Add(objectId, marker);
            return marker;
        }
    }
}