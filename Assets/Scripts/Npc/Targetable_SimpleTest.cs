using UnityEngine;

public class Targetable_SimpleTest : Targetable_Base
{
    public override void OnDamaged(float damageAmount, Vector2 pushForce)
    {
        Debug.Log($"Targetable_SimpleTest received {damageAmount} damage.");
        // Apply push force to the target
        GetComponent<Rigidbody2D>()?.AddForce(pushForce * 30f, ForceMode2D.Impulse);
    }
}
