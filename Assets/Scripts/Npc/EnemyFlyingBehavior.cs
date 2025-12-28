using System.Collections;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

public class EnemyFlyingBehavior : EnemyBehaviorBase
{
    [SerializeField] private float maxHealth = 40f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField, Range(0f,1f)] private float moveDrag = 0.5f;
    [SerializeField] private GameObject projectileToSpawn;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private Vector2 preferingDistanceFromPlayer;
    [SerializeField, MinMaxRangeSlider(0f, 5f)] private Vector2 roamingCycleDurationRange;
    [SerializeField] private float attackInterval = 3f;
    [SerializeField] private LayerMask obstacleLayer;

    [SerializeField] private Rigidbody2D rBody;
    [SerializeField] private Animator animator;
    [SerializeField] private SimpleSoundModule sound;
    [SerializeField] private HealthBar healthBar;

    [SerializeField, ReadOnly] protected float currentHealth;
    public float CurrentHealth { get { return currentHealth; } }

    BehaviorState currentState_hidden;
    BehaviorState currentState
    {
        get { return currentState_hidden; }
        set 
        {
            currentState_hidden.OnStateExit(this);
            currentState_hidden = value;
            currentState_hidden.OnStateEnter(this);
        }
    }

    BehaviorState nextState;

    private float lastHurtTime = 0f;
    private float hurtImmobilityDuration = 1f;

    private void Start()
    {
        currentState_hidden = new State_Roam();
        currentState_hidden.OnStateEnter(this);

        currentHealth = maxHealth;
    }

    private void Update()
    {
        rBody.linearVelocity = Vector2.Lerp(rBody.linearVelocity, Vector2.zero, moveDrag);

        currentState.OnUpdate(this);

        if(nextState != null)
        {
            currentState = nextState;
            nextState = null;
        }
    }

    private void FixedUpdate()
    {
        currentState.OnFixedUpdate(this);

        if (nextState != null)
        {
            currentState = nextState;
            nextState = null;
        }
    }

    protected class BehaviorState
    {
        public virtual void OnStateEnter(EnemyFlyingBehavior npc) { }
        public virtual void OnUpdate(EnemyFlyingBehavior npc) { }
        public virtual void OnFixedUpdate(EnemyFlyingBehavior npc) { }
        public virtual void OnStateExit(EnemyFlyingBehavior npc) { }
    }

    protected class State_Roam : BehaviorState
    {

        bool isMoving = false;

        float roamingTimer;
        float nextRoamingCycle;

        const float noiseScrollSpeed = 4.0f;

        private void RerollRoamingCycleTime(EnemyFlyingBehavior npc) { nextRoamingCycle = Random.Range(npc.roamingCycleDurationRange.x, npc.roamingCycleDurationRange.y); }

        public override void OnStateEnter(EnemyFlyingBehavior npc)
        {
            roamingTimer = Time.time;
            RerollRoamingCycleTime(npc);
        }

        public override void OnFixedUpdate(EnemyFlyingBehavior npc)
        {
            if(npc.CheckPlayerInDetection(npc.detectionRange))
            {
                npc.nextState = new State_Alerted();
                return;
            }

            if(Time.time - roamingTimer > nextRoamingCycle)
            {
                roamingTimer = Time.time;
                RerollRoamingCycleTime(npc);

                isMoving = !isMoving;
            }
            else
            {
                if (isMoving)
                {
                    // set velocity as perlin noise movement
                    npc.rBody.linearVelocity = new Vector2(
                        (Mathf.PerlinNoise1D(Time.time * noiseScrollSpeed) - 0.5f), 
                        (Mathf.PerlinNoise1D(Time.time * noiseScrollSpeed + 10f ) - 0.5f)) 
                        * npc.moveSpeed;
                }

                if(npc.rBody.linearVelocityX > 0)
                {
                    npc.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                }
                else
                {
                    npc.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }

                    return;
            }
        }
    }

    protected class State_Alerted : BehaviorState
    {
        float alertTimer;
        float attackTimer;
        float alertedUntil = 10f;


        public override void OnStateEnter(EnemyFlyingBehavior npc)
        {
            alertTimer = alertedUntil;
            attackTimer = npc.attackInterval;
        }

        public override void OnFixedUpdate(EnemyFlyingBehavior npc)
        {
            if (Time.time - npc.lastHurtTime < npc.hurtImmobilityDuration)
                return;

            Vector2 track = PlayerController.Instance.transform.position;
            Vector2 npcPos = new Vector2(npc.transform.position.x, npc.transform.position.y);

            alertTimer -= Time.fixedDeltaTime;
            attackTimer -= Time.fixedDeltaTime;

            if(alertTimer < 0)
            {
                npc.nextState = new State_Roam();
                return;
            }

            if(npc.CheckPlayerInDetection(npc.preferingDistanceFromPlayer.y))
            {
                if(Vector2.Distance(track,npcPos) < npc.preferingDistanceFromPlayer.x)
                {
                    attackTimer = npc.attackInterval;
                    npc.rBody.linearVelocity = (npcPos - track).normalized * npc.moveSpeed;

                    if (npc.rBody.linearVelocityX > 0)
                        npc.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    else
                        npc.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }
                else
                {
                    PlayerController target = PlayerController.Instance;

                    if (target.transform.position.x > npc.transform.position.x)
                        npc.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    else
                        npc.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                    if (attackTimer < 0)
                    {
                        attackTimer = npc.attackInterval;

                        npc.Attack(PlayerController.Instance.transform);
                    }
                }
            }
            else
            {
                attackTimer = npc.attackInterval;

                npc.rBody.linearVelocity = (track - npcPos).normalized * npc.moveSpeed;

                if (npc.rBody.linearVelocityX > 0)
                    npc.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                else
                    npc.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

        }
    }

    protected class State_Hurt : BehaviorState
    {
        const float hurtDuration = 0.5f;

        public override void OnStateEnter(EnemyFlyingBehavior npc)
        {
            npc.sound.Play("Hurt");
            npc.StartCoroutine(Cor_Hurt(npc));
        }

        IEnumerator Cor_Hurt(EnemyFlyingBehavior npc)
        {
            yield return new WaitForSeconds(hurtDuration);
            npc.nextState = new State_Alerted();
        }
    }

    protected class State_Dead : BehaviorState
    {
        const float deathDuration = 1.5f;

        public override void OnStateEnter(EnemyFlyingBehavior npc)
        {
            npc.StartCoroutine(Cor_Dead(npc));
            npc.animator.SetTrigger("Dead");
            npc.sound.Play("Dead");
        }

        IEnumerator Cor_Dead(EnemyFlyingBehavior npc)
        {
            yield return new WaitForSeconds(deathDuration);

            Destroy(npc.gameObject);
        }
    }

    const float hurtPushForce = 20f;

    public override void OnHurt(float damageAmount, Vector2 pushForce)
    {
        if(currentState is State_Dead)
            return;

        currentHealth -= damageAmount;
        lastHurtTime = Time.time;
        rBody.linearVelocity = pushForce * hurtPushForce;

        if (currentHealth <= 0)
        {
            healthBar.SetValue(0f);
            currentState = new State_Dead();
        }
        else
        {
            healthBar.SetValue(currentHealth/maxHealth);
            currentState = new State_Hurt();
        }
    }

    private bool CheckPlayerInDetection(float radius)
    {
        Vector3 playerPosition = PlayerController.Instance.transform.position;

        if (Vector3.Distance(playerPosition, transform.position) < radius)
        {
            if (Physics2D.Raycast(transform.position, (playerPosition - transform.position).normalized, Vector3.Distance(playerPosition, transform.position), obstacleLayer))
            {
                Debug.DrawLine(transform.position, playerPosition, Color.red);
                return false;
            }
            else
            {
                Debug.DrawLine(transform.position, playerPosition, Color.green);
                return true;
            }
        }
        else
            return false;
    }

    private void Attack(Transform target)
    {
        if (target.transform.position.x > transform.position.x)
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        
        GameObject proj = Instantiate(projectileToSpawn, transform.position, Quaternion.identity);
        proj.transform.up = (PlayerController.Instance.transform.position - transform.position).normalized;
        proj.GetComponent<Enemy_Projectile>().sender = this;

        animator.SetTrigger("Attack");
        sound.Play("Attack");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, preferingDistanceFromPlayer.x);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, preferingDistanceFromPlayer.y);
    }
}
