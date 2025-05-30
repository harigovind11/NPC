using UnityEngine;

public class TestNPCStarter : MonoBehaviour
{
    public Transform target;
    private NPCBehaviorController controller;

    void Start()
    {
        controller = GetComponent<NPCBehaviorController>();
        if (controller != null && target != null)
        {
            controller.UpdateCurrentTarget(target);
            controller.UpdateState(0); // Move state
        }
    }
}