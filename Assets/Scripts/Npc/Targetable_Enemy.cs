using UnityEngine;

public class Targetable_Enemy : Targetable_Base
{
    [SerializeField] private EnemyBehaviorBase enemyBehavior;

    public override void OnDamaged(float damageAmount, Vector2 pushForce)
    {
        enemyBehavior.OnHurt(damageAmount, pushForce);
    }

}
