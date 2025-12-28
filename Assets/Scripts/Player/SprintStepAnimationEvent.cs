using UnityEngine;
using UnityEngine.Events;

public class SprintStepAnimationEvent : MonoBehaviour
{
    public UnityEvent OnSprintStep;

    public void SprintStepEvent()
    {
        OnSprintStep.Invoke();
    }
}
