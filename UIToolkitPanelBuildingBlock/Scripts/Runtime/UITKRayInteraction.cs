using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Oculus.Interaction.Surfaces;

[DisallowMultipleComponent]
public class UITKRayInteraction : MonoBehaviour
{
    public Vector2 PanelCoord { get; private set; }
    public bool IsRayOverPanel { get; private set; }

    [Header("Controller Ray Interactors")]
    [SerializeField] private UITKControllerRayInteractorFromDataSource _controllerRaySourceLeft;
    [SerializeField] private UITKControllerRayInteractorFromDataSource _controllerRaySourceRight;
    
    [Header("Hand Ray Interactors")]
    [SerializeField] private UITKHandRayInteractorFromDataSource _handRaySourceLeft;
    [SerializeField] private UITKHandRayInteractorFromDataSource _handRaySourceRight;
    
    [Header("UI Setup")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private PlaneSurface _planeSurface;
    [SerializeField] private BoxCollider _collider;

    public struct RayHitInfo
    {
        public string hand;
        public Vector2 panelCoord;
    }

    public List<RayHitInfo> Hits { get; } = new();
    
    private VisualElement _root;

    private void Awake()
    {
        UpdateRootReference();
    }

    private void OnEnable()
    {
        UpdateRootReference();
    }

    private void UpdateRootReference()
    {
        _root = _uiDocument ? _uiDocument.rootVisualElement : null;
    }

    private void Update()
    {
        Hits.Clear();
        IsRayOverPanel = false;
        
        if (_root?.panel == null && _uiDocument)
        {
            UpdateRootReference();
        }
        
        if (_root?.panel == null || !_collider) return;
        if (_root?.panel == null)
        {
            return;
        }
        
        if (!_collider)
        {
            return;
        }

        Vector2 size = _root.layout.size;
        if (size.x <= 0f || size.y <= 0f) return;
        if (size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        TryControllerRay(_controllerRaySourceLeft, "Left", size);
        TryControllerRay(_controllerRaySourceRight, "Right", size);
        
        TryHandRay(_handRaySourceLeft, "Left", size);
        TryHandRay(_handRaySourceRight, "Right", size);
    }

    private void TryControllerRay(UITKControllerRayInteractorFromDataSource src, string hand, Vector2 size)
    {
        if (!src) return;
        if (!src.TryGetRay(out Ray ray)) return;
        ProcessRaycast(ray, hand, size);
    }
    
    private void TryHandRay(UITKHandRayInteractorFromDataSource src, string hand, Vector2 size)
    {
        if (!src) return;
        if (!src.TryGetRay(out Ray _)) return;
        if (!src)
        {
            return;
        }
        if (!src.TryGetRay(out Ray ray))
        {
            return;
        }
        
        ProcessRaycast(ray, hand, size);
    }

    private void ProcessRaycast(Ray ray, string hand, Vector2 size)
    {

        if (_planeSurface && !_planeSurface.DoubleSided)
        {
            Vector3 n = _planeSurface.Normal;
            if (Vector3.Dot(ray.direction, n) >= 0f) return;
            if (Vector3.Dot(ray.direction, n) >= 0f)
            {
                return;
            }
        }

        if (_collider.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            Vector3 local = _collider.transform.InverseTransformPoint(hit.point) - _collider.center;
            Vector3 sizeWorld = _collider.size;
            Vector3 half = 0.5f * sizeWorld;

            float u = (local.x + half.x) / sizeWorld.x;
            float v = (local.y + half.y) / sizeWorld.y;

            var coord = new Vector2(u * size.x, v * size.y);
            Hits.Add(new RayHitInfo { hand = hand, panelCoord = coord });
            IsRayOverPanel = true;
        }
    }
}