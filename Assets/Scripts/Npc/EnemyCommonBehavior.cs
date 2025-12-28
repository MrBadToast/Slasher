using ReadOnlyDrawer;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class EnemyCommonBehavior : EnemyBehaviorBase
{
    [SerializeField] private bool startBehaviorOnStart = true;
    [SerializeField] private float MaxHealth = 100f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeedMult = 1.5f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] float detectionRadius = 5f;
    [SerializeField] float attackStartRadius = 1f;
    [SerializeField] private LayerMask obstacleLayer;

    [SerializeField,MinMaxRangeSlider(0f,10f)] Vector2 roamDurationRange;
    [SerializeField,MinMaxRangeSlider(0f,10f)] Vector2 idleDurationRange;
    [SerializeField, MinMaxRangeSlider(0f, 5f)] Vector2 chaseDirectionChangeRange;

    [Header("ChildRefernce")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D mainCollider;
    [SerializeField] private SimpleSoundModule sound;
    [SerializeField] private BoxCollider2D attackBoxCollider;
    [SerializeField] private Rigidbody2D rBody;
    [SerializeField] private HealthBar healthBar;


    IEnumerator currentBehavior;
    IEnumerator nextBehavior;

    [SerializeField, ReadOnly] protected float currentHealth;
    public float CurrentHealth { get { return currentHealth; } }

#pragma warning disable CS0414

    [SerializeField, ReadOnly] BehaviorType debug_currentType;

#pragma warning restore CS0414

    public enum BehaviorType
    {
        None,
        Idle,
        Roam,
        Chase,
        Attack,
        Hurt,
        Dead,
    }

    float playerDetectionCheckTimer = 0f;
    const float playerDetectionCheckInterval = 0.5f;

    private void Start()
    {
        if (startBehaviorOnStart)
        {
            StartCoroutine(Cor_MainBehaviorLoop());
        }

        currentHealth = MaxHealth;
    }

    const float speedDamping = 0.3f;

    private void FixedUpdate()
    {
        if (rBody.bodyType == RigidbodyType2D.Dynamic)
            rBody.linearVelocityX = Mathf.Lerp(rBody.linearVelocityX, 0f, speedDamping);
    }

    public override void OnHurt(float damageAmount, Vector2 pushForce)
    {
        if (currentHealth < 0) return;

        currentHealth -= damageAmount;
        healthBar.SetValue(currentHealth/MaxHealth);

        StopAllCoroutines();


        rBody.linearVelocity = pushForce.normalized * 20f;

        if (currentHealth > 0)
        {
            animator.SetTrigger("Hurt");
            StartCoroutine(Cor_Hurt(pushForce));
        }
        else
        {
            StartCoroutine(Cor_Die());
        }
    }

    IEnumerator Cor_MainBehaviorLoop()
    {
        currentBehavior = Cor_Behavior_Roam();

        while (true)
        {
            if (nextBehavior != null)
            {
                currentBehavior = nextBehavior;
                nextBehavior = null;
            }
            else
            {
                currentBehavior = Cor_Behavior_Roam();
            }

            yield return StartCoroutine(currentBehavior);
        }
    }

    IEnumerator Cor_Behavior_Roam()
    {
        debug_currentType = BehaviorType.Roam;

        animator.SetBool("Move", true);

        playerDetectionCheckTimer = 0f;

        float roamDuration = Random.Range(roamDurationRange.x, roamDurationRange.y);

        bool roamForRight = Random.value < 0.5;

        if (roamForRight)
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        for (float t = 0; t < roamDuration; t += Time.fixedDeltaTime)
        {
            playerDetectionCheckTimer += Time.fixedDeltaTime;

            if (playerDetectionCheckTimer >= playerDetectionCheckInterval)
            {
                playerDetectionCheckTimer = 0f;

                if (CheckPlayerInDetectionRadius())
                {
                    nextBehavior = Cor_Behavior_ChasePlayer();
                    yield break;
                }
            }

            rBody.linearVelocityX = transform.right.x * moveSpeed;

            yield return new WaitForFixedUpdate();
        }


        nextBehavior = Cor_Behavior_Idle();

        yield break;
    }

    IEnumerator Cor_Behavior_Idle()
    {
        debug_currentType = BehaviorType.Idle;

        animator.SetBool("Move", false);

        playerDetectionCheckTimer = 0f;

        float idleDuration = Random.Range(idleDurationRange.x, idleDurationRange.y);

        for (float t = 0; t < idleDuration; t += Time.fixedDeltaTime)
        {
            playerDetectionCheckTimer += Time.fixedDeltaTime;
            if (playerDetectionCheckTimer >= playerDetectionCheckInterval)
            {
                playerDetectionCheckTimer = 0f;
                if (CheckPlayerInDetectionRadius())
                {
                    nextBehavior = Cor_Behavior_ChasePlayer();
                    yield break;
                }
            }
            yield return new WaitForFixedUpdate();
        }

        nextBehavior = Cor_Behavior_Roam();
    }

    IEnumerator Cor_Behavior_ChasePlayer()
    {
        debug_currentType = BehaviorType.Chase;

        playerDetectionCheckTimer = 0f;
        float chaseDirectionChangeTimer = Random.Range(chaseDirectionChangeRange.x, chaseDirectionChangeRange.y);
        float alertedUntil = 5f;
        float alertTimer = alertedUntil;
        
        while (alertTimer > 0f)
        {
            alertTimer -= Time.fixedDeltaTime;

            if(CheckPlayerInAttackRadius())
            {
                nextBehavior = Cor_TryAttack();
                yield break;
            }

            if(CheckPlayerInAttackRadius())
            {
                alertTimer = alertedUntil;
            }

            chaseDirectionChangeTimer -= Time.fixedDeltaTime;

            if (chaseDirectionChangeTimer < 0f)
            {
                chaseDirectionChangeTimer = Random.Range(chaseDirectionChangeRange.x, chaseDirectionChangeRange.y);

                Vector3 playerPos = PlayerController.Instance.transform.position;

                if(playerPos.x > transform.position.x)
                {
                    transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }
            }

            rBody.linearVelocityX = transform.right.x * moveSpeed * chaseSpeedMult;

            yield return new WaitForFixedUpdate();
        }

        nextBehavior = Cor_Behavior_Idle();

    }

    const float postAttackTime = 0.2f;
    const float attackingTime = 1f;
    const float hurtPushForce = 20f;

    IEnumerator Cor_TryAttack()
    {
        debug_currentType = BehaviorType.Attack;

        sound.Play("Attack");
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(postAttackTime);

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackBoxCollider.bounds.center, attackBoxCollider.size, 0f);

        foreach (Collider2D hit in hits)
        {
            if(hit.CompareTag("Player"))
            {
                Vector3 playerPos = hit.transform.position;
                PlayerController.Instance.HurtPlayer((playerPos - transform.position).normalized * hurtPushForce, attackDamage);
            }
        }

        yield return new WaitForSeconds(attackingTime);

        nextBehavior = Cor_Behavior_ChasePlayer();
    }



    const float hurtTime = 0.5f;

    IEnumerator Cor_Hurt(Vector2 pushForce)
    {
        debug_currentType = BehaviorType.Hurt;
        sound.Play("Hurt");
        TimeManager.Instance.ChangeTimeScale(0.2f, 0.1f);

        if (pushForce.x > 0)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        yield return new WaitForSeconds(hurtTime);

        StartCoroutine(Cor_MainBehaviorLoop());
    }

    IEnumerator Cor_Die()
    {
        debug_currentType = BehaviorType.Dead;
        animator.SetTrigger("Die");
        sound.Play("Die");

        rBody.bodyType = RigidbodyType2D.Static;
        mainCollider.enabled = false;

        yield return new WaitForSeconds(1f);

        Destroy(gameObject);
    }

    private bool CheckPlayerInDetectionRadius()
    {

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Vector3 playerPos = hit.gameObject.transform.position;
                if (Physics2D.Raycast(transform.position, (playerPos - transform.position).normalized, Vector3.Distance(playerPos, transform.position),obstacleLayer)) // check for obstructed to player
                {
                    Debug.DrawLine(transform.position, playerPos, Color.red);
                    return false;
                }
                else
                {
                    Debug.DrawLine(transform.position, playerPos,Color.green);
                    return true;
                }
            }
        }
        return false;
    }

    private bool CheckPlayerInAttackRadius()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackStartRadius);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Vector3 playerPos = hit.gameObject.transform.position;
                if (Physics2D.Raycast(transform.position, (playerPos - transform.position).normalized, Vector3.Distance(playerPos, transform.position), obstacleLayer)) // check for obstructed to player
                {
                    Debug.DrawLine(transform.position, playerPos, Color.red);
                    return false;
                }
                else
                {
                    Debug.DrawLine(transform.position, playerPos, Color.green);
                    return true;
                }
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackStartRadius);
    }

}

// ============= comment =============
// 코드가 많이 복잡하지 않을 것 같아 행동제어에 코루틴을 사용했지만
// 만들고 보니 생각보다 복잡해져 리팩토링을 할 때 state pattern으로 바꾸는 것을 고려해보는게 좋을 것 같습니다.
// ===================================