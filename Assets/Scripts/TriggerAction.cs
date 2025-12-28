using UnityEngine;
using UnityEngine.Events;

public class TriggerAction : MonoBehaviour
{
    public UnityEvent OnTriggered;
    public bool triggerOnce = true; 

    bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggerOnce && hasTriggered)
            return;

        if (collision.CompareTag("Player"))
        {
            hasTriggered = true;
            OnTriggered?.Invoke();
        }
    }
}
