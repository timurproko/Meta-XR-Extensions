using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Oculus.Interaction.Surfaces;

namespace Oculus.Interaction
{
    [ExecuteAlways]
    public sealed class UITKBoundsDriver : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private BoundsClipper _boundsClipper;
        [SerializeField] private BoxCollider _collider;

        private DocState _last;
        private bool _subscribed;
        private static FieldInfo _ppuField;

        private void OnEnable()
        {
            _last = ReadState();
            TryApply(_last, force:true);
            SubscribeGeometry();
        }

        private void OnDisable()
        {
            UnsubscribeGeometry();
        }

        private void LateUpdate()
        {
            var now = ReadState();
            TryApply(now);
        }

        private void SubscribeGeometry()
        {
            if (_subscribed || !_uiDocument) return;
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _subscribed = true;
        }

        private void UnsubscribeGeometry()
        {
            if (!_subscribed || !_uiDocument) return;
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _subscribed = false;
        }

        private void OnGeometryChanged(GeometryChangedEvent _)
        {
            var now = ReadState();
            TryApply(now);
        }

        private struct DocState
        {
            public PanelSettings ps;
            public Vector2 sizePx;
            public Pivot pivot;
            public float ppu;

            public bool Equals(in DocState other) =>
                ps == other.ps &&
                pivot == other.pivot &&
                sizePx == other.sizePx &&
                Mathf.Approximately(ppu, other.ppu);
        }

        private DocState ReadState()
        {
            var ps = _uiDocument ? _uiDocument.panelSettings : null;
            var size = GetDocSizePixels(_uiDocument);
            var pivot = _uiDocument ? _uiDocument.pivot : Pivot.Center;
            var ppu = GetEffectivePPU(_uiDocument, ps);
            return new DocState { ps = ps, sizePx = size, pivot = pivot, ppu = ppu };
        }

        private void TryApply(in DocState now, bool force = false)
        {
            if (!_uiDocument || !_boundsClipper || !_collider) return;
            if (!force && _last.Equals(now)) return;

            _last = now;

            var ppu = Mathf.Max(0.0001f, now.ppu);
            var sizeMeters = now.sizePx / ppu;
            var centerPx   = GetCenterOffsetPixels(now.sizePx, now.pivot);
            var centerM    = centerPx / ppu;

            var size3 = new Vector3(sizeMeters.x, sizeMeters.y, 0.01f);
            var ctr3  = new Vector3(centerM.x, centerM.y, 0f);

            _boundsClipper.Size     = size3;
            _boundsClipper.Position = ctr3;

            _collider.size   = size3;
            _collider.center = ctr3;
        }

        private static Vector2 GetDocSizePixels(UIDocument doc)
        {
            if (!doc) return Vector2.zero;

            if (doc.worldSpaceSizeMode == UIDocument.WorldSpaceSizeMode.Fixed)
                return doc.worldSpaceSize;

            var root = doc.rootVisualElement;
            if (root == null) return Vector2.zero;

            var r = root.worldBound; // px
            var w = float.IsNaN(r.width)  || r.width  < 0 ? 0f : r.width;
            var h = float.IsNaN(r.height) || r.height < 0 ? 0f : r.height;
            return new Vector2(w, h);
        }

        private static float GetEffectivePPU(UIDocument doc, PanelSettings ps)
        {
            if (doc && doc.worldSpaceSizeMode == UIDocument.WorldSpaceSizeMode.Dynamic)
                return 1f;
            return GetPixelsPerUnit(ps);
        }

        private static float GetPixelsPerUnit(PanelSettings ps)
        {
            if (!ps) return 1f;

            if (_ppuField == null)
            {
                _ppuField = typeof(PanelSettings).GetField("m_PixelsPerUnit", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (_ppuField != null)
            {
                try
                {
                    var val = _ppuField.GetValue(ps);
                    if (val is float f && !float.IsNaN(f) && float.IsFinite(f) && f > 0f)
                    {
                        return f;
                    }
                }
                catch
                {
                }
            }

            return 1f;
        }

        private static Vector2 GetCenterOffsetPixels(in Vector2 sizePx, Pivot pivot)
        {
            (float px, float py) = pivot switch
            {
                Pivot.BottomLeft   => (0f,   0f),
                Pivot.BottomCenter => (0.5f, 0f),
                Pivot.BottomRight  => (1f,   0f),
                Pivot.LeftCenter   => (0f,   0.5f),
                Pivot.Center       => (0.5f, 0.5f),
                Pivot.RightCenter  => (1f,   0.5f),
                Pivot.TopLeft      => (0f,   1f),
                Pivot.TopCenter    => (0.5f, 1f),
                Pivot.TopRight     => (1f,   1f),
                _ => (0.5f, 0.5f)
            };
            return new Vector2((0.5f - px) * sizePx.x, (0.5f - py) * sizePx.y);
        }
    }
}