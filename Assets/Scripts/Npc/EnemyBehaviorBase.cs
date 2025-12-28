using UnityEngine;

public class EnemyBehaviorBase : MonoBehaviour
{
    public virtual void OnHurt(float damageAmount, Vector2 pushForce) { }
    public virtual void OnDeath() { }

    private void OnDestroy()
    {
        WaveManager.Instance.RequestRemoveFromEnemylist();
    }
}
