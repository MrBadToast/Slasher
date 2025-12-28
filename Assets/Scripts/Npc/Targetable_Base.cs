using UnityEngine;

[RequireComponent(typeof(Collider2D)),RequireComponent(typeof(Rigidbody2D))]
public class Targetable_Base : MonoBehaviour
{
    public virtual void OnDamaged(float damageAmount, Vector2 pushForce) { }

}
