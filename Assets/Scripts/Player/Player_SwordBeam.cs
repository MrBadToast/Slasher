using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_SwordBeam : MonoBehaviour
{
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private float trevelSpeed = 5f;
    [SerializeField] private float damageAmount = 100f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask targetLayer;

    Rigidbody2D rBody;
    SimpleSoundModule soundModule;

    List<Targetable_Base> targetHits;

    float exsitanceDurationMax = 50f;
    float lifeTime;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody2D>();
        soundModule = GetComponent<SimpleSoundModule>();
        targetHits = new List<Targetable_Base>();
    }

    private void Start()
    {
        lifeTime = Time.time;
    }

    private void FixedUpdate()
    {
        if (Time.time - lifeTime > exsitanceDurationMax)  
            Destroy(gameObject);

        Collider2D[] obstacles = Physics2D.OverlapCircleAll(transform.position, hitRadius,obstacleLayer);

        if(obstacles.Length > 0)
        {
            Destroy(gameObject);
            return;
        }

        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, hitRadius,targetLayer);

        foreach (Collider2D target in targets)
        {
            Targetable_Base targetable;

            if(target.TryGetComponent<Targetable_Base>(out targetable))
            {
                if(targetHits.Contains(targetable))
                    continue;

                targetable.OnDamaged(damageAmount, transform.up);
                soundModule.Play("Hit");
                targetHits.Add(targetable);
            }

        }

        transform.Translate(transform.up * trevelSpeed,Space.World);

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
