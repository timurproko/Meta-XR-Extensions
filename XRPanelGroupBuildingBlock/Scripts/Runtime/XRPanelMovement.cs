using Sirenix.OdinInspector;
using UnityEngine;

public class XRPanelMovement : MonoBehaviour
{
    private enum Mode { Follow, Billboard }

    [SerializeField] private bool _useEnvironmentSpecificMode;
    [HideIf(nameof(_useEnvironmentSpecificMode))]
    [SerializeField] private Mode _mode = Mode.Billboard;
    [ShowIf(nameof(_useEnvironmentSpecificMode))]
    [SerializeField] private Mode _editorMode = Mode.Billboard;
    [ShowIf(nameof(_useEnvironmentSpecificMode))]
    [SerializeField] private Mode _buildMode = Mode.Follow;
    
    [Header("Constraints")]
    [SerializeField] private bool _lockVertical = true;
    [ShowIf(nameof(_lockVertical))] [SerializeField] private float _minY = 0.5f;
    [ShowIf(nameof(_lockVertical))] [SerializeField] private float _maxY = 2f;

    [Header("Pitch Limits")]
    [SerializeField] private float _minPitchAngle = -30f;
    [SerializeField] private float _maxPitchAngle = 20f;
    
    [Header("Follow")]
    [ShowIf(nameof(ShowFollowSettings))]
    [SerializeField] private Vector3 _localOffset = new(0f, 0f, 0.8f);
    [ShowIf(nameof(ShowFollowSettings))]
    [Range(0f, 20f)]
    [SerializeField] private float _followSpeed = 3f;

    [Header("Billboard")]
    [ShowIf(nameof(ShowBillboardSettings))]
    [SerializeField] private float _relativeYFactor = 1f;
    [ShowIf(nameof(ShowBillboardSettings))]
    [SerializeField] private float _rotationLerpSpeed = 5f;

    private Transform _cameraTransform;
    private bool _enableRelativeMovement;

    private Vector3 _followVelocity;
    private Vector3 _lastAllowedPosition;
    private Quaternion _lastAllowedRotation;
    private float _lastAllowedY;

    private float _baseCameraY;
    private float _baseLocalY;
    private float _billboardYVelocity;
    private Quaternion _billboardTargetRotation;

    private bool _snapNextFollow;

    private void Awake()
    {
        _cameraTransform = Camera.main.transform;
    }
    
    private void Start()
    {
        if (!_cameraTransform) return;

        if (GetActiveMode() == Mode.Follow)
            Init(new Vector3(0f, 1f, 0.88f), false);
        else
            CacheBillboardBaseline();
    }

    private void LateUpdate()
    {
        if (!_cameraTransform) return;

        Mode activeMode = GetActiveMode();

        if (activeMode == Mode.Follow)
            RunFollowStep();
        else
            RunBillboardStep();
    }

    private Mode GetActiveMode()
    {
        if (_useEnvironmentSpecificMode)
            return Application.isEditor ? _editorMode : _buildMode;

        return _mode;
    }

    private bool ShowFollowSettings => UsesMode(Mode.Follow);

    private bool ShowBillboardSettings => UsesMode(Mode.Billboard);

    private bool UsesMode(Mode targetMode)
    {
        if (_useEnvironmentSpecificMode)
            return _editorMode == targetMode || _buildMode == targetMode;

        return _mode == targetMode;
    }

    private void Init(Vector3 offset, bool faceCameraHorizontally = true)
    {
        Vector3 targetPosition = _cameraTransform.TransformPoint(offset);
        if (_lockVertical) targetPosition.y = Mathf.Clamp(targetPosition.y, _minY, _maxY);
        transform.position = targetPosition;

        Vector3 directionToCamera = _cameraTransform.position - transform.position;
        if (faceCameraHorizontally) directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(directionToCamera.normalized, Vector3.up);

        _lastAllowedPosition = transform.position;
        _lastAllowedRotation = transform.rotation;
    }

    private void RunFollowStep()
    {
        float pitchAngle = GetCameraPitchAngle();
        bool pitchLimited = pitchAngle > _maxPitchAngle || pitchAngle < _minPitchAngle;

        ComputeFollowTarget(pitchLimited, out Vector3 desiredPosition, out Quaternion desiredRotation);
        ApplyFollow(desiredPosition, desiredRotation);
    }

    private void ComputeFollowTarget(bool pitchLimited, out Vector3 desiredPosition, out Quaternion desiredRotation)
    {
        if (pitchLimited)
        {
            desiredPosition = _lastAllowedPosition;
            desiredRotation = _lastAllowedRotation;
            return;
        }

        desiredPosition = _cameraTransform.position + _cameraTransform.rotation * _localOffset;
        if (_lockVertical)
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, _minY, _maxY);

        Vector3 forwardNoRoll = _cameraTransform.forward;
        Vector3 up = Vector3.up;
        desiredRotation = Quaternion.LookRotation(forwardNoRoll, up);

        _lastAllowedPosition = desiredPosition;
        _lastAllowedRotation = desiredRotation;
    }

    private void ApplyFollow(Vector3 desiredPosition, Quaternion desiredRotation)
    {
        if (_snapNextFollow)
        {
            transform.position = desiredPosition;
            transform.rotation = desiredRotation;
            _snapNextFollow = false;
            return;
        }

        transform.position =
            Vector3.SmoothDamp(transform.position, desiredPosition, ref _followVelocity, 1f / _followSpeed);

        transform.rotation =
            Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * _followSpeed);
    }

    private void RunBillboardStep()
    {
        float targetY = ComputeBillboardTargetY();
        ApplyBillboardY(targetY);

        UpdateBillboardTargetRotation();
        ApplyBillboardRotation();
    }

    private void CacheBillboardBaseline()
    {
        _baseCameraY = _cameraTransform.position.y;
        _baseLocalY = transform.localPosition.y;
        _billboardTargetRotation = transform.rotation;
    }

    private float ComputeBillboardTargetY()
    {
        float cameraDeltaY = _cameraTransform.position.y - _baseCameraY;
        return _baseLocalY + cameraDeltaY * _relativeYFactor;
    }

    private void ApplyBillboardY(float targetY)
    {
        Vector3 localPos = transform.localPosition;
        float newY = Mathf.SmoothDamp(localPos.y, targetY, ref _billboardYVelocity, 1f / _followSpeed);
        transform.localPosition = new Vector3(localPos.x, newY, localPos.z);
    }

    private void UpdateBillboardTargetRotation()
    {
        Vector3 lookDirection = _cameraTransform.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.0001f)
            _billboardTargetRotation = Quaternion.LookRotation(-lookDirection.normalized, Vector3.up);
    }

    private void ApplyBillboardRotation()
    {
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _billboardTargetRotation,
            Time.deltaTime * _rotationLerpSpeed
        );
    }

    private float GetCameraPitchAngle()
    {
        Vector3 cameraForward = _cameraTransform.forward;
        Vector3 flatForward = Vector3.ProjectOnPlane(cameraForward, Vector3.up).normalized;
        return Vector3.SignedAngle(flatForward, cameraForward, _cameraTransform.right);
    }
}
