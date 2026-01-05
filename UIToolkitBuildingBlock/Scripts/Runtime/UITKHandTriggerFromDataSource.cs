using System;
using Oculus.Interaction.Input;
using UnityEngine;

public class UITKHandTriggerFromDataSource : MonoBehaviour
{
    [SerializeField] private Hand _hand;

    public event Action WhenSelected;
    public event Action WhenUnselected;
    
    private bool _pressedPrev;

    private void OnEnable()
    {
        if (_hand)
        {
            _pressedPrev = _hand.GetIndexFingerIsPinching();
            _hand.WhenHandUpdated += OnUpdated;
        }
    }

    private void OnDisable()
    {
        if (_hand)
        {
            _hand.WhenHandUpdated -= OnUpdated;
        }
    }

    private void OnUpdated()
    {
        if (!_hand)
        {
            return;
        }

        bool pressedNow = _hand.GetIndexFingerIsPinching();

        if (pressedNow && !_pressedPrev)
        {
            WhenSelected?.Invoke();
        }
        if (!pressedNow && _pressedPrev)
        {
            WhenUnselected?.Invoke();
        }
        
        _pressedPrev = pressedNow;
    }
}