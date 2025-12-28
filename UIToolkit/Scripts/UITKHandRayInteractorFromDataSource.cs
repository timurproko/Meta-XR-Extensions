using Oculus.Interaction.Input;
using UnityEngine;

public class UITKHandRayInteractorFromDataSource : MonoBehaviour
{
    [SerializeField] private Hand _hand;
    
    private const float InvalidPoseLogInterval = 2f;
    
    private float _lastInvalidPoseLogTime;
    private bool _lastPoseValidState;

    private void OnEnable()
    {
        _lastPoseValidState = false;
    }

    public bool TryGetRay(out Ray ray)
    {
        ray = default;

        if (!_hand)
        {
            return false;
        }

        bool isValid = _hand.IsPointerPoseValid;
        if (!isValid)
        {
            if (_lastPoseValidState != isValid || Time.time - _lastInvalidPoseLogTime > InvalidPoseLogInterval)
            {
                _lastInvalidPoseLogTime = Time.time;
            }
            _lastPoseValidState = isValid;
            return false;
        }

        _lastPoseValidState = isValid;

        if (!_hand.GetPointerPose(out Pose pose))
        {
            return false;
        }

        ray = new Ray(pose.position, pose.forward);
        return true;
    }
}