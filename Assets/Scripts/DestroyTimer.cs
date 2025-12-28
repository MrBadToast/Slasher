using UnityEngine;

public class DestroyTimer : MonoBehaviour
{
    [SerializeField] float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
