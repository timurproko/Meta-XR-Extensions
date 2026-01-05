using System;
using UnityEngine;

public class UITKHandTriggerInteraction : MonoBehaviour
{
    [SerializeField] private UITKElementPicker _elementPicker;
    [SerializeField] private UITKHandTriggerFromDataSource _triggerSourceLeft;
    [SerializeField] private UITKHandTriggerFromDataSource _triggerSourceRight;

    private Action _leftSelectAction;
    private Action _leftUnselectAction;
    private Action _rightSelectAction;
    private Action _rightUnselectAction;

    private void OnEnable()
    {
        if (_triggerSourceLeft)
        {
            _leftSelectAction ??= () => OnTriggerDown("Left");
            _leftUnselectAction ??= () => OnTriggerUp("Left");
            _triggerSourceLeft.WhenSelected += _leftSelectAction;
            _triggerSourceLeft.WhenUnselected += _leftUnselectAction;
        }

        if (_triggerSourceRight)
        {
            _rightSelectAction ??= () => OnTriggerDown("Right");
            _rightUnselectAction ??= () => OnTriggerUp("Right");
            _triggerSourceRight.WhenSelected += _rightSelectAction;
            _triggerSourceRight.WhenUnselected += _rightUnselectAction;
        }
    }

    private void OnDisable()
    {
        if (_triggerSourceLeft)
        {
            if (_leftSelectAction != null)
                _triggerSourceLeft.WhenSelected -= _leftSelectAction;
            if (_leftUnselectAction != null)
                _triggerSourceLeft.WhenUnselected -= _leftUnselectAction;
        }

        if (_triggerSourceRight)
        {
            if (_rightSelectAction != null)
                _triggerSourceRight.WhenSelected -= _rightSelectAction;
            if (_rightUnselectAction != null)
                _triggerSourceRight.WhenUnselected -= _rightUnselectAction;
        }
    }

    private void OnTriggerDown(string hand)
    {
        if (UITKInteractionBlocker.IsBlocked)
        {
            return;
        }
        if (!_elementPicker)
        {
            return;
        }
        _elementPicker.Press(hand);
    }

    private void OnTriggerUp(string hand)
    {
        if (UITKInteractionBlocker.IsBlocked)
        {
            return;
        }
        if (!_elementPicker)
        {
            return;
        }
        _elementPicker.Release(hand);
    }
}