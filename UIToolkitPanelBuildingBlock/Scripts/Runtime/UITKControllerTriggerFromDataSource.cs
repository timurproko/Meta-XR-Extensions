using System;
using Oculus.Interaction.Input;
using UnityEngine;

public class UITKControllerTriggerFromDataSource : MonoBehaviour
{
    [SerializeField] private Controller _controller;

    public event Action WhenSelected;
    public event Action WhenUnselected;
    
    private bool _pressedPrev;

    private void OnEnable()
    {
        if (_controller) _controller.WhenUpdated += OnUpdated;
    }

    private void OnDisable()
    {
        if (_controller) _controller.WhenUpdated -= OnUpdated;
    }

    private void OnUpdated()
    {
        bool pressedNow = _controller.IsButtonUsageAnyActive(ControllerButtonUsage.TriggerButton);

        if (pressedNow && !_pressedPrev) WhenSelected?.Invoke();
        if (!pressedNow && _pressedPrev) WhenUnselected?.Invoke();
        _pressedPrev = pressedNow;
    }
}