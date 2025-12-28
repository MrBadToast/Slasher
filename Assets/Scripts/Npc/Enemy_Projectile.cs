using System.Collections;
using UnityEngine;

public class Enemy_Projectile : Targetable_Base
{
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private float trevelSpeed = 5f;
    [SerializeField] private float reflectedtrevelTime = 0.2f;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float damageOnReflected = 50f;
    [SerializeField] private LayerMask obstacleLayer;
    [HideInInspector] public EnemyFlyingBehavior sender;

    Rigidbody2D rBody;
    bool refelected;
    SimpleSoundModule soundModule;

    float exsitanceDurationMax = 50f;
    float lifeTime;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody2D>();
        soundModule = GetComponent<SimpleSoundModule>();
    }

    private void Start()
    {
        lifeTime = Time.time;
    }

    private void FixedUpdate()
    {
        if (Time.time - lifeTime > exsitanceDurationMax)  
            Destroy(gameObject);

        if (refelected) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);

        foreach (Collider2D hit in hits)
        {
            if (((1<< hit.gameObject.layer) & obstacleLayer) == 1)
            {
                Destroy(gameObject);
            }

            if (hit.gameObject.CompareTag("Player"))
            {
                PlayerController.Instance.HurtPlayer(transform.up, projectileDamage);
                Destroy(gameObject);
            }
        }
        
        transform.Translate(transform.up * trevelSpeed,Space.World);

    }

    public override void OnDamaged(float damageAmount, Vector2 pushForce)
    {
        if (refelected) return;

        refelected = true;

        StartCoroutine(Cor_Refelct());
    }

    private IEnumerator Cor_Refelct()
    {
        Vector3 from = transform.position;
        Vector3 to;

        if (sender != null)
            to = sender.transform.position;
        else
            to = -transform.right * 10f;

        TimeManager.Instance.ChangeTimeScale(0.2f, 0.5f);
        soundModule.Play("Hit");

        for (float t = 0; t < reflectedtrevelTime; t += Time.fixedDeltaTime)
        {
            transform.position = Vector3.Lerp(from, to, t / reflectedtrevelTime);
            yield return new WaitForFixedUpdate();
        }

        if (sender != null)
            sender.OnHurt(damageOnReflected, -transform.up);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
