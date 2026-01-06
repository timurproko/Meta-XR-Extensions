using System.Reflection;
using Oculus.Interaction.Input;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-999)]
public class UITKCameraRigBinder : MonoBehaviour
{
    [SerializeField] private OVRCameraRigRef _OVRCameraRigRef;

    private void Awake()
    {
        var cameraRig = FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include);
        
        if (!_OVRCameraRigRef)
        {
            _OVRCameraRigRef = FindAnyObjectByType<OVRCameraRigRef>(FindObjectsInactive.Include);
        }
        
        if (_OVRCameraRigRef && cameraRig)
        {
            
            _OVRCameraRigRef.InjectInteractionOVRCameraRig(cameraRig);
        }
    }
}
