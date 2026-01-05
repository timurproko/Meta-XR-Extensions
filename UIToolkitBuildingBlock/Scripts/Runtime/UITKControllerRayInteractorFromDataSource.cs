using Oculus.Interaction.Input;
using UnityEngine;

public class UITKControllerRayInteractorFromDataSource : MonoBehaviour
{
    [SerializeField] private Controller _controller;

    public bool TryGetRay(out Ray ray)
    {
        ray = default;

        if (!_controller) return false;
        if (!_controller.TryGetPointerPose(out var pose)) return false;

        ray = new Ray(pose.position, pose.forward);
        return true;
    }
}