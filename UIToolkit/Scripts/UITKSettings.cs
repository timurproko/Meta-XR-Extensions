using UnityEngine;
using UnityEngine.UIElements;
using Oculus.Interaction;

[RequireComponent(typeof(UIDocument))]
public class UITKSettings : MonoBehaviour
{
    [SerializeField] private bool _isInteractive = true;

    private RayInteractable _rayInteractable;
    private bool _lastInteractiveState = true;

    private void Awake()
    {
        _rayInteractable = GetComponentInChildren<RayInteractable>();
        _lastInteractiveState = _isInteractive;
        UpdateRayInteractableState();
    }

    private void OnEnable()
    {
        UpdateRayInteractableState();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && _lastInteractiveState != _isInteractive)
        {
            _lastInteractiveState = _isInteractive;
            UpdateRayInteractableState();
        }
    }

    private void UpdateRayInteractableState()
    {
        if (!Application.isPlaying) return;

        if (_rayInteractable != null)
        {
            _rayInteractable.enabled = _isInteractive;
        }
    }

    public static bool IsInteractive(UIDocument uiDocument)
    {
        if (!uiDocument) return true;
        
        var settings = uiDocument.GetComponent<UITKSettings>();
        return !settings || settings._isInteractive;
    }
}

