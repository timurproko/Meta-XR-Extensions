using System.Reflection;
using Oculus.Interaction.Input;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1)]
public class UITKDataSourceBinder : MonoBehaviour
{
    [Header("Controller Data Sources")]
    [SerializeField] private FromOVRControllerDataSource _leftController;
    [SerializeField] private FromOVRControllerDataSource _rightController;

    [Header("Hand Data Sources")]
    [SerializeField] private FromOVRHandDataSource _leftHand;
    [SerializeField] private FromOVRHandDataSource _rightHand;

    private OVRCameraRigRef _rigRef;
    private TrackingToWorldTransformerOVR _tracking;
    private HandSkeletonOVR _skeleton;

    private void Awake()
    {
        _rigRef = FindAnyObjectByType<OVRCameraRigRef>(FindObjectsInactive.Include);
        if (!_rigRef) return;

        _tracking = _rigRef.GetComponentInChildren<TrackingToWorldTransformerOVR>(true);
        _skeleton = _rigRef.GetComponentInChildren<HandSkeletonOVR>(true);

        ApplyBindings();
    }

    private void ApplyBindings()
    {
        BindController(_leftController,  _rigRef, _tracking, Handedness.Left);
        BindController(_rightController, _rigRef, _tracking, Handedness.Right);

        BindHand(_leftHand,  _rigRef, _tracking, Handedness.Left,  _skeleton);
        BindHand(_rightHand, _rigRef, _tracking, Handedness.Right, _skeleton);
    }

    #region CONTROLLERS

    private static void BindController(
        FromOVRControllerDataSource ds,
        OVRCameraRigRef rigRef,
        TrackingToWorldTransformerOVR tracking,
        Handedness handedness)
    {
        if (!ds || !rigRef || !tracking) return;

        typeof(FromOVRControllerDataSource)
            .GetField("_cameraRigRef", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(ds, rigRef);

        typeof(FromOVRControllerDataSource)
            .GetProperty("CameraRigRef", BindingFlags.Instance | BindingFlags.Public)
            ?.GetSetMethod(true)
            ?.Invoke(ds, new object[] { rigRef });

        ds.InjectTrackingToWorldTransformer(tracking);
        ds.InjectHandedness(handedness);
        ds.MarkInputDataRequiresUpdate();
    }

    #endregion

    #region HANDS

    private void BindHand(
        FromOVRHandDataSource ds,
        OVRCameraRigRef rigRef,
        TrackingToWorldTransformerOVR tracking,
        Handedness handedness,
        IHandSkeletonProvider skeletonProvider)
    {
        if (!ds)
        {
            return;
        }
        if (!rigRef)
        {
            return;
        }
        if (!tracking)
        {
            return;
        }

        typeof(FromOVRHandDataSource)
            .GetField("_cameraRigRef", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(ds, rigRef);

        var hand = FindOVRHand(handedness);
        if (!hand)
        {
            return;
        }

        ds.InjectOptionalOVRHand(hand);
        ds.InjectTrackingToWorldTransformer(tracking);
        ds.InjectHandedness(handedness);

        if (skeletonProvider != null)
        {
            ds.InjectHandSkeletonProvider(skeletonProvider);
        }

        ds.MarkInputDataRequiresUpdate();
    }
    
    private void Start()
    {
        BindHandsToUITKComponents();
    }
    
    private void BindHandsToUITKComponents()
    {
        Hand leftHand = GetHandFromDataSource(_leftHand, Handedness.Left);
        Hand rightHand = GetHandFromDataSource(_rightHand, Handedness.Right);
        
        var handRayInteractors = FindObjectsByType<UITKHandRayInteractorFromDataSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var handTriggers = FindObjectsByType<UITKHandTriggerFromDataSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        int boundCount = 0;
        
        Hand DetermineHandForComponent(MonoBehaviour component)
        {
            if (!component) return null;
            
            string objName = component.gameObject.name.ToLower();
            string parentName = component.transform.parent ? component.transform.parent.name.ToLower() : "";
            
            bool isLeft = objName.Contains("left") || parentName.Contains("left") || objName.StartsWith("l_");
            bool isRight = objName.Contains("right") || parentName.Contains("right") || objName.StartsWith("r_");
            
            if (isLeft && leftHand)
            {
                return leftHand;
            }
            if (isRight && rightHand)
            {
                return rightHand;
            }
            
            return leftHand ?? rightHand;
        }
        
        foreach (var interactor in handRayInteractors)
        {
            if (!interactor) continue;
            
            var handField = typeof(UITKHandRayInteractorFromDataSource)
                .GetField("_hand", BindingFlags.Instance | BindingFlags.NonPublic);
            
            if (handField != null)
            {
                var currentHand = handField.GetValue(interactor) as Hand;
                
                if (!currentHand)
                {
                    Hand handToBind = DetermineHandForComponent(interactor);
                    
                    if (handToBind)
                    {
                        handField.SetValue(interactor, handToBind);
                        boundCount++;
                    }
                }
            }
        }
        
        foreach (var trigger in handTriggers)
        {
            if (!trigger) continue;
            
            var handField = typeof(UITKHandTriggerFromDataSource)
                .GetField("_hand", BindingFlags.Instance | BindingFlags.NonPublic);
            
            if (handField != null)
            {
                var currentHand = handField.GetValue(trigger) as Hand;
                
                if (!currentHand)
                {
                    Hand handToBind = DetermineHandForComponent(trigger);
                    
                    if (handToBind)
                    {
                        handField.SetValue(trigger, handToBind);
                        boundCount++;
                    }
                }
            }
        }
    }
    
    private Hand GetHandFromDataSource(FromOVRHandDataSource ds, Handedness handedness)
    {
        return GetOrCreateHandComponent(ds, handedness);
    }
    
    private Hand GetOrCreateHandComponent(FromOVRHandDataSource ds, Handedness handedness)
    {
        if (!ds) return null;
        
        Hand handComponent = ds.GetComponent<Hand>();
        if (handComponent)
        {
            return handComponent;
        }
        
        var handFromDataSourceType = System.Type.GetType("Oculus.Interaction.Input.HandFromDataSource");
        if (handFromDataSourceType != null)
        {
            var handFromDataSource = ds.GetComponent(handFromDataSourceType);
            if (handFromDataSource)
            {
                var handProperty = handFromDataSourceType.GetProperty("Hand", BindingFlags.Instance | BindingFlags.Public);
                if (handProperty != null)
                {
                    handComponent = handProperty.GetValue(handFromDataSource) as Hand;
                    if (handComponent)
                    {
                        return handComponent;
                    }
                }
            }
        }
        
        
        var handType = typeof(Hand);
        if (handType != null)
        {
            handComponent = ds.gameObject.AddComponent(handType) as Hand;
            if (handComponent)
            {
                var injectDataSourceMethod = handType.GetMethod("InjectDataSource", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (injectDataSourceMethod != null)
                {
                    injectDataSourceMethod.Invoke(handComponent, new object[] { ds });
                }
                else
                {
                    var injectMethod = handType.GetMethod("Inject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (injectMethod != null)
                    {
                        injectMethod.Invoke(handComponent, new object[] { ds });
                    }
                }
                
                return handComponent;
            }
        }
        
        return null;
    }

    private OVRHand FindOVRHand(Handedness handedness)
    {
        if (!_rigRef)
        {
            return null;
        }

        var hands = _rigRef.GetComponentsInChildren<OVRHand>(true);

        var target = handedness == Handedness.Left
            ? OVRPlugin.Hand.HandLeft
            : OVRPlugin.Hand.HandRight;

        foreach (var h in hands)
        {
            var handType = h.GetHand();
            if (handType == target)
            {
                return h;
            }
        }

        return null;
    }

    #endregion
}
