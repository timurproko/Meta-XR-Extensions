using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

public class UITKElementPicker : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private UITKRayInteraction _rayInteraction;
    
    private const float MinPixelsPerUnit = 0.0001f;
    private const float MinPressInterval = 0.05f;
    private const float MinReleaseInterval = 0.1f;
    private const float ClickCooldown = 0.2f;
    private const float HoverActivationDelay = 0.1f;

    private static readonly int LeftHandPointerId = PointerId.touchPointerIdBase;
    private static readonly int RightHandPointerId = PointerId.touchPointerIdBase + 1;
    private static readonly Vector2 OffPanelPosition = new(-10000f, -10000f);

    private readonly Dictionary<string, PointerState> _pointerStates = new(StringComparer.Ordinal);
    private readonly HashSet<string> _activeHands = new(StringComparer.Ordinal);
    private readonly List<VisualElement> _activeElementsToClear = new();
    private readonly Dictionary<VisualElement, Coroutine> _activeClearCoroutines = new();

    private float _pixelsPerUnit = 1f;
    private VisualElement _currentRoot;
    private UnityEngine.UIElements.IPanel _currentPanel;

    private VisualElement Root => _uiDocument ? _uiDocument.rootVisualElement : null;

    private sealed class PointerState
    {
        public int PointerId;
        public Vector2 PanelPosition = OffPanelPosition;
        public bool HasPosition;
        public bool IsOverPanel;
        public bool IsPressed;
        public VisualElement CurrentHoveredElement;
        public VisualElement PressedElement;
        public float LastReleaseTime;
        public float LastPressTime;
        public bool HasCompletedClick;
        public VisualElement PendingHoverElement;
        public float PendingHoverStartTime;
    }

    private void OnDisable()
    {
        CancelAllPointers();

        foreach (var coroutine in _activeClearCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }

        _activeClearCoroutines.Clear();

        foreach (var element in _activeElementsToClear)
        {
            if (element != null)
            {
                UITKUtils.TrySetActive(element, false);
            }
        }

        _activeElementsToClear.Clear();
    }

    private void Update()
    {
        if (!_uiDocument || !_rayInteraction)
        {
            return;
        }

        var root = Root;
        if (root == null)
        {
            if (_currentRoot != null || _currentPanel != null)
            {
                CancelAllPointers();
                _currentRoot = null;
                _currentPanel = null;
            }

            return;
        }

        var panel = root.panel;
        if (panel == null)
        {
            if (_currentPanel != null)
            {
                CancelAllPointers();
                _currentPanel = null;
            }

            _currentRoot = root;
            return;
        }

        if (_currentRoot != root || _currentPanel != panel)
        {
            CancelAllPointers();
            _currentRoot = root;
            _currentPanel = panel;
        }

        if (!UITKUtils.TryGetPixelsPerUnit(_uiDocument, out _pixelsPerUnit, 1f) ||
            !float.IsFinite(_pixelsPerUnit) ||
            _pixelsPerUnit <= 0f)
        {
            _pixelsPerUnit = 1f;
        }

        if (UITKInteractionBlocker.IsBlocked || !UITKSettings.IsInteractive(_uiDocument))
        {
            CancelAllPointers();
            return;
        }

        _activeHands.Clear();

        var hits = _rayInteraction.Hits;
        if (hits != null)
        {
            for (int i = 0; i < hits.Count; i++)
            {
                var hit = hits[i];
                if (ProcessPointerMove(hit.hand, hit.panelCoord))
                {
                    _activeHands.Add(hit.hand);
                }
            }
        }

        foreach (var kvp in _pointerStates)
        {
            if (!_activeHands.Contains(kvp.Key))
            {
                ProcessPointerExit(kvp.Key);
            }
        }
    }

    public void Press(string hand)
    {
        if (!_pointerStates.TryGetValue(hand, out var state))
        {
            return;
        }

        if (!state.IsOverPanel)
        {
            return;
        }

        if (!state.HasPosition)
        {
            return;
        }

        if (state.IsPressed)
        {
            return;
        }

        float currentTime = Time.unscaledTime;
        if (currentTime - state.LastPressTime < MinPressInterval)
        {
            return;
        }

        state.LastPressTime = currentTime;

        state.IsPressed = true;
        state.HasCompletedClick = false;
        state.PressedElement = state.CurrentHoveredElement;
        SendPointerDown(state);
    }

    public void Release(string hand)
    {
        if (!_pointerStates.TryGetValue(hand, out var state))
        {
            return;
        }

        if (!state.IsPressed)
        {
            return;
        }

        float currentTime = Time.unscaledTime;
        if (currentTime - state.LastReleaseTime < MinReleaseInterval)
        {
            return;
        }

        state.LastReleaseTime = currentTime;

        state.IsPressed = false;
        VisualElement pressedElement = state.PressedElement;

        SendPointerUp(state);

        state.HasCompletedClick = true;

        state.PressedElement = null;

        StartCoroutine(SendPointerCancelAfterDelay(state, pressedElement));

        if (pressedElement != null)
        {
            if (_activeClearCoroutines.TryGetValue(pressedElement, out var existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
                _activeClearCoroutines.Remove(pressedElement);
                _activeElementsToClear.Remove(pressedElement);
            }

            UITKUtils.TrySetActive(pressedElement, true);
        }
    }

    private IEnumerator SendPointerCancelAfterDelay(PointerState state, VisualElement pressedElement)
    {
        yield return null;

        using var cancelEvt = PointerCancelEvent.GetPooled();
        PopulatePointerEvent(cancelEvt, state, state.PanelPosition, Vector2.zero);
        VisualElement cancelTarget = pressedElement ?? state.CurrentHoveredElement ?? (_currentRoot ?? Root);
        if (cancelTarget != null)
        {
            cancelEvt.target = cancelTarget;
            SetLocalPositionForElement(cancelEvt, state.PanelPosition, cancelTarget);
        }

        Dispatch(cancelEvt);

        if (state.HasPosition)
        {
            SendPointerMove(state, state.PanelPosition, Vector2.zero);
        }

        if (pressedElement != null)
        {
            if (_activeClearCoroutines.TryGetValue(pressedElement, out var existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
                _activeClearCoroutines.Remove(pressedElement);
                _activeElementsToClear.Remove(pressedElement);
            }

            UITKUtils.TrySetActive(pressedElement, true);

            if (!_activeElementsToClear.Contains(pressedElement))
            {
                _activeElementsToClear.Add(pressedElement);
            }

            var coroutine = StartCoroutine(ClearActiveStateDelayed(pressedElement, 0.1f));
            _activeClearCoroutines[pressedElement] = coroutine;
        }
    }

    private IEnumerator ClearActiveStateDelayed(VisualElement element, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (element != null)
        {
            UITKUtils.TrySetActive(element, false);
            _activeElementsToClear.Remove(element);
            _activeClearCoroutines.Remove(element);
        }
    }

    private bool ProcessPointerMove(string hand, Vector2 rawCoord)
    {
        if (!TryGetPanelPosition(rawCoord, out var panelPosition))
        {
            return false;
        }

        var state = GetOrCreateState(hand);
        var delta = state.HasPosition ? panelPosition - state.PanelPosition : Vector2.zero;

        state.PanelPosition = panelPosition;
        state.HasPosition = true;

        bool wasOverPanel = state.IsOverPanel;
        state.IsOverPanel = true;

        var root = _currentRoot ?? Root;
        
        var pickedElement = FindInteractiveElementAtPosition(root, panelPosition);

        if (pickedElement != state.CurrentHoveredElement)
        {
            if (pickedElement == state.PendingHoverElement)
            {
                float timeOverElement = Time.unscaledTime - state.PendingHoverStartTime;
                if (timeOverElement < HoverActivationDelay)
                {
                    pickedElement = state.CurrentHoveredElement;
                }
                else
                {
                    state.PendingHoverElement = null;
                }
            }
            else
            {
                state.PendingHoverElement = pickedElement;
                state.PendingHoverStartTime = Time.unscaledTime;
                pickedElement = state.CurrentHoveredElement;
            }
        }
        else
        {
            state.PendingHoverElement = null;
        }

        if (state.CurrentHoveredElement != null && state.CurrentHoveredElement != pickedElement)
        {
            SendPointerLeave(state, state.CurrentHoveredElement);
            state.CurrentHoveredElement = null;
        }

        if (pickedElement != null && pickedElement != state.CurrentHoveredElement)
        {
            if (state.HasCompletedClick)
            {
                float timeSinceRelease = Time.unscaledTime - state.LastReleaseTime;

                if (timeSinceRelease < ClickCooldown)
                {
                    var previousElement = state.CurrentHoveredElement;
                    if (previousElement != null && previousElement != pickedElement)
                    {
                        UITKUtils.TrySetHover(previousElement, false);
                    }

                    state.CurrentHoveredElement = pickedElement;
                }
                else
                {
                    state.HasCompletedClick = false;

                    if (state.CurrentHoveredElement != null && state.CurrentHoveredElement != pickedElement)
                    {
                        SendPointerLeave(state, state.CurrentHoveredElement);
                    }

                    SendPointerEnter(state, pickedElement);
                    state.CurrentHoveredElement = pickedElement;
                }
            }
            else
            {
                SendPointerEnter(state, pickedElement);
                state.CurrentHoveredElement = pickedElement;
            }
        }

        SendPointerMove(state, panelPosition, wasOverPanel ? delta : Vector2.zero);
        return true;
    }

    private void ProcessPointerExit(string hand)
    {
        if (!_pointerStates.TryGetValue(hand, out var state)) return;
        if (!state.IsOverPanel) return;

        if (state.CurrentHoveredElement != null)
        {
            SendPointerLeave(state, state.CurrentHoveredElement);
            state.CurrentHoveredElement = null;
        }

        state.IsOverPanel = false;
        state.HasPosition = false;
        state.PanelPosition = OffPanelPosition;
        SendPointerMove(state, OffPanelPosition, Vector2.zero);
    }

    private void CancelAllPointers()
    {
        foreach (var state in _pointerStates.Values)
        {
            CancelPointer(state);
        }
    }

    private void CancelPointer(PointerState state)
    {
        if (state.CurrentHoveredElement != null)
        {
            SendPointerLeave(state, state.CurrentHoveredElement);
            state.CurrentHoveredElement = null;
        }

        if (state.IsOverPanel)
        {
            state.IsOverPanel = false;
            state.HasPosition = false;
            SendPointerMove(state, OffPanelPosition, Vector2.zero);
        }

        if (state.IsPressed)
        {
            if (state.PressedElement != null)
            {
                if (_activeClearCoroutines.TryGetValue(state.PressedElement, out var existingCoroutine))
                {
                    StopCoroutine(existingCoroutine);
                    _activeClearCoroutines.Remove(state.PressedElement);
                    _activeElementsToClear.Remove(state.PressedElement);
                }

                UITKUtils.TrySetActive(state.PressedElement, false);
            }

            using var cancelEvt = PointerCancelEvent.GetPooled();
            PopulatePointerEvent(cancelEvt, state, state.PanelPosition, Vector2.zero);
            Dispatch(cancelEvt);
            state.IsPressed = false;
            state.PressedElement = null;
        }

        state.PanelPosition = OffPanelPosition;
        state.HasPosition = false;
    }

    private PointerState GetOrCreateState(string hand)
    {
        if (!_pointerStates.TryGetValue(hand, out var state))
        {
            int pointerId = hand == "Left" ? LeftHandPointerId : RightHandPointerId;

            state = new PointerState
            {
                PointerId = pointerId
            };
            _pointerStates[hand] = state;
        }

        return state;
    }

    private static bool IsInteractiveElement(VisualElement element, VisualElement root)
    {
        if (element == null || element == root) return false;

        if (element is Button) return true;
        if (element.focusable) return true;
        if (element.ClassListContains("button")) return true;
        if (HasClickableManipulator(element)) return true;

        return false;
    }

    private static bool HasClickableManipulator(VisualElement ve)
    {
        if (ve == null) return false;
        try
        {
            var f = typeof(VisualElement).GetField("m_Clickable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return f != null && f.GetValue(ve) != null;
        }
        catch
        {
            return false;
        }
    }

    private static VisualElement FindInteractiveParent(VisualElement element, VisualElement root)
    {
        if (element == null || root == null) return null;

        VisualElement documentRoot = null;
        for (var current = element; current != null; current = current.parent)
        {
            if (current == root)
            {
                documentRoot = root;
                break;
            }
        }

        if (documentRoot == null) return null;

        if (IsInteractiveElement(element, root))
        {
            return element;
        }

        for (var current = element.parent; current != null && current != root; current = current.parent)
        {
            if (IsInteractiveElement(current, root))
            {
                return current;
            }
        }

        return null;
    }

    private static VisualElement FindInteractiveElementAtPosition(VisualElement root, Vector2 position)
    {
        if (root == null) return null;

        var rootBounds = root.worldBound;
        if (!rootBounds.Contains(position))
        {
            return null;
        }

        return FindInteractiveElementRecursive(root, root, position);
    }

    private static VisualElement FindInteractiveElementRecursive(VisualElement element, VisualElement documentRoot, Vector2 position)
    {
        if (element == null) return null;

        var bounds = element.worldBound;
        if (!bounds.Contains(position))
        {
            return null;
        }

        for (int i = element.childCount - 1; i >= 0; i--)
        {
            var child = element[i];
            var result = FindInteractiveElementRecursive(child, documentRoot, position);
            if (result != null)
            {
                return result;
            }
        }

        if (IsInteractiveElement(element, documentRoot))
        {
            return element;
        }

        return null;
    }

    private bool TryGetPanelPosition(Vector2 rawCoord, out Vector2 panelPosition)
    {
        panelPosition = OffPanelPosition;

        var root = Root;
        if (root == null) return false;

        var (width, height) = UITKUtils.GetPanelSize(_uiDocument);
        if (width <= 0f || height <= 0f) return false;

        float ppu = Mathf.Max(_pixelsPerUnit, MinPixelsPerUnit);
        var panelSpace = rawCoord / ppu;
        panelSpace.x = Mathf.Clamp(panelSpace.x, 0f, width);
        panelSpace.y = Mathf.Clamp(panelSpace.y, 0f, height);

        var pivotOffset = PivotToBottomLeft(_uiDocument);
        panelPosition = panelSpace - pivotOffset;
        return true;
    }

    private void SendPointerMove(PointerState state, Vector2 position, Vector2 delta)
    {
        using var evt = PointerMoveEvent.GetPooled();
        PopulatePointerEvent(evt, state, position, delta);
        Dispatch(evt);
    }

    private void SendPointerEnter(PointerState state, VisualElement element)
    {
        using var evt = PointerEnterEvent.GetPooled();
        PopulatePointerEvent(evt, state, state.PanelPosition, Vector2.zero);
        evt.target = element;
        SetLocalPositionForElement(evt, state.PanelPosition, element);
        Dispatch(evt);

        UITKUtils.TrySetHover(element, true);
    }

    private void SendPointerLeave(PointerState state, VisualElement element)
    {
        using var evt = PointerLeaveEvent.GetPooled();
        PopulatePointerEvent(evt, state, state.PanelPosition, Vector2.zero);
        evt.target = element;
        SetLocalPositionForElement(evt, state.PanelPosition, element);
        Dispatch(evt);

        UITKUtils.TrySetHover(element, false);
    }

    private static void SetLocalPositionForElement(EventBase evt, Vector2 panelPosition, VisualElement element)
    {
        var elementWorldPos = (Vector2)element.worldBound.position;
        var pointerPos = panelPosition;
        var localPos2D = pointerPos - elementWorldPos;

        var evtType = evt.GetType();
        var localPositionProp = evtType.GetProperty("localPosition",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (localPositionProp != null)
        {
            var setter = localPositionProp.GetSetMethod(true);
            if (setter != null)
            {
                object valueToSet = localPos2D;
                if (localPositionProp.PropertyType == typeof(Vector3))
                {
                    valueToSet = new Vector3(localPos2D.x, localPos2D.y, 0f);
                }

                setter.Invoke(evt, new object[] { valueToSet });
            }
        }
    }

    private void SendPointerDown(PointerState state)
    {
        using var evt = PointerDownEvent.GetPooled();
        PopulatePointerEvent(evt, state, state.PanelPosition, Vector2.zero);
        PointerEventAccess.Set(evt, nameof(evt.button), (int)MouseButton.LeftMouse);
        PointerEventAccess.Set(evt, nameof(evt.pressedButtons), 1);

        VisualElement targetElement = state.PressedElement;
        if (targetElement != null)
        {
            evt.target = targetElement;
            SetLocalPositionForElement(evt, state.PanelPosition, targetElement);
        }

        Dispatch(evt);

        VisualElement elementToActivate = targetElement ?? (evt.target as VisualElement);
        if (elementToActivate != null)
        {
            if (_activeClearCoroutines.TryGetValue(elementToActivate, out var existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
                _activeClearCoroutines.Remove(elementToActivate);
                _activeElementsToClear.Remove(elementToActivate);
            }

            UITKUtils.TrySetActive(elementToActivate, true);
        }
    }

    private void SendPointerUp(PointerState state)
    {
        using var evt = PointerUpEvent.GetPooled();
        PopulatePointerEvent(evt, state, state.PanelPosition, Vector2.zero);
        PointerEventAccess.Set(evt, nameof(evt.button), (int)MouseButton.LeftMouse);
        PointerEventAccess.Set(evt, nameof(evt.pressedButtons), 0);

        VisualElement targetElement = state.PressedElement;
        if (targetElement != null)
        {
            evt.target = targetElement;
            SetLocalPositionForElement(evt, state.PanelPosition, targetElement);
        }

        Dispatch(evt);
    }

    private static void PopulatePointerEvent<T>(
        PointerEventBase<T> evt,
        PointerState state,
        Vector2 position,
        Vector2 delta) where T : PointerEventBase<T>, new()
    {
        PointerEventAccess.Set(evt, nameof(evt.pointerId), state.PointerId);
        PointerEventAccess.Set(evt, nameof(evt.pointerType), PointerType.pen);
        PointerEventAccess.Set(evt, nameof(evt.isPrimary), true);
        PointerEventAccess.Set(evt, nameof(evt.position), position);
        PointerEventAccess.Set(evt, nameof(evt.localPosition), position);
        PointerEventAccess.Set(evt, nameof(evt.deltaPosition), delta);
        PointerEventAccess.Set(evt, nameof(evt.button), (int)MouseButton.LeftMouse);
        PointerEventAccess.Set(evt, nameof(evt.pressedButtons), state.IsPressed ? 1 : 0);
        PointerEventAccess.Set(evt, nameof(evt.clickCount), 1);
        PointerEventAccess.Set(evt, nameof(evt.modifiers), EventModifiers.None);
        PointerEventAccess.Set(evt, nameof(evt.pressure), state.IsPressed ? 1f : 0f);
    }

    private void Dispatch(EventBase evt)
    {
        var root = _currentRoot ?? Root;
        if (root == null) return;
        var panel = _currentPanel;
        if (panel == null) return;

        if (evt is IPointerEvent pointerEvt)
        {
            var pickedElement = FindInteractiveElementAtPosition(root, pointerEvt.position);

            if (pickedElement != null)
            {
                evt.target = pickedElement;
                SetLocalPositionForElement(evt, pointerEvt.position, pickedElement);
            }
            else
            {
                evt.target = root;
            }
        }
        else
        {
            evt.target = root;
        }

        root.SendEvent(evt);
    }

    private static Vector2 PivotToBottomLeft(UIDocument doc)
    {
        var (W, H) = UITKUtils.GetPanelSize(doc);

        return doc.pivot switch
        {
            Pivot.Center => new Vector2(W * 0.5f, H * 0.5f),
            Pivot.TopLeft => new Vector2(0f, H),
            Pivot.TopCenter => new Vector2(W * 0.5f, H),
            Pivot.TopRight => new Vector2(W, H),
            Pivot.LeftCenter => new Vector2(0f, H * 0.5f),
            Pivot.RightCenter => new Vector2(W, H * 0.5f),
            Pivot.BottomLeft => new Vector2(0f, 0f),
            Pivot.BottomCenter => new Vector2(W * 0.5f, 0f),
            Pivot.BottomRight => new Vector2(W, 0f),
            _ => Vector2.zero
        };
    }

    private static class PointerEventAccess
    {
        private static readonly Dictionary<(Type type, string property), PropertyInfo> PropertyCache = new();
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static void Set<TEvent>(PointerEventBase<TEvent> evt, string propertyName, object value)
            where TEvent : PointerEventBase<TEvent>, new()
        {
            if (evt == null) return;
            var type = evt.GetType();
            var key = (type, propertyName);

            if (!PropertyCache.TryGetValue(key, out var propertyInfo))
            {
                propertyInfo = type.GetProperty(propertyName, Flags);
                PropertyCache[key] = propertyInfo;
            }

            if (propertyInfo == null) return;

            var propertyType = propertyInfo.PropertyType;
            var actualValue = value;

            if (propertyType == typeof(Vector3) && value is Vector2 v2)
            {
                actualValue = new Vector3(v2.x, v2.y, 0f);
            }

            var setter = propertyInfo.GetSetMethod(true);
            setter?.Invoke(evt, new[] { actualValue });
        }
    }
}
