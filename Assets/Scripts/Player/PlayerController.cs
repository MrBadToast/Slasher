using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ReadOnlyAttribute = ReadOnlyDrawer.ReadOnlyAttribute;

public class PlayerController : StaticMonoBehaviour<PlayerController>
{
    #region Properties

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField,Range(0f,1f)] private float moveDamping = 0.5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float playerMaxHealth = 100f;

    [Header("GeneralMoveSetting")]
    [SerializeField] private float jumpHoldTime = 0.4f;
    [SerializeField] private AnimationCurve jumpHoldCurve;
    [SerializeField] private float coyoteTimeThreshold = 0.05f;
    [SerializeField] private float jumpBufferThreshold = 0.05f;
    [SerializeField, Range(0f, 1f)] private float wallSlideDrag = 0.5f;

    [Header("DashSetting")]
    [SerializeField] private float dashDistance = 1f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCooldown = 0.5f;
    [SerializeField] private AnimationCurve dashCurve;

    [Header("NormalAttackSettings")]
    [SerializeField] private float normalAttackInterval = 0.3f;
    [SerializeField] private float normalAttackPushForce = 2f;
    [SerializeField] private float normalAttackDamage = 1f;

    [Header("SkillAttackSettings")]
    [SerializeField] private GameObject skillAttackObject;
    [SerializeField] private float skillAttackCooldown = 10f;

    [Header("PlayerHurt")]
    [SerializeField] private float immobilityAfterHurt = 0.5f;
    [SerializeField] private float invincibleAfterHurt = 2f;

    [Header("Physic Hits")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask halfGroundLayer;
    [SerializeField] private float footRaycastLength = 0.1f;
    [SerializeField] private float forwardRaycastLength = 0.1f;
    
    [Header("ChildReferences")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform RCO_Foot_L;
    [SerializeField] private Transform RCO_Foot_R;
    [SerializeField] private Transform RCO_Forward;
    [SerializeField] private Transform RCO_Head;
    [SerializeField] private SimpleSoundModule sound;
    [SerializeField] private SimpleSoundModule longSound;
    [SerializeField] private SprintStepAnimationEvent sprintStepEvent;
    [SerializeField] private BoxCollider2D attackRange;
    [SerializeField] private ParticleSystem deathParticle;
    [SerializeField] private GameObject slashEffectObject;
    [SerializeField] private GameObject focusCamera;

    #endregion

    private InputSystemActions input;
    public InputSystemActions Input { get { return input; } }
    private Rigidbody2D rBody;

    public bool Grounded { get { return hit_foot_L || hit_foot_R; } }
    public bool ObstacleForward { get { return hit_forward && hit_head; } }

    [HideInInspector] public UnityEvent OnPlayerDied;

    RaycastHit2D hit_head;
    RaycastHit2D hit_forward;
    RaycastHit2D hit_foot_L;
    RaycastHit2D hit_foot_R;

    public enum JumpType
    {
        Normal,
        Coyote,
        Buffered
    }

#pragma warning disable CS0414 

    [Header("Debug")]
    [ReadOnly, SerializeField] private bool debug_Grounded;
    [ReadOnly, SerializeField] private bool debug_ObstacleForward;
    [ReadOnly, SerializeField] private JumpType debug_LastJumpType;

#pragma warning restore CS0414

    private MoveState currentMoveState_hidden;

    [ReadOnly,SerializeField] private float currentPlayerHealth;
    public float CurrentPlayerHealth { get {  return currentPlayerHealth; }  }

    [ReadOnly, SerializeField] private float currentSkillAttackCooldown;
    public float CurrentSkillAttackCooldown { get { return currentSkillAttackCooldown; } }

    private MoveState currentMoveState
    {
        get { return currentMoveState_hidden; }
        set
        {
            if(value == null)
                return;

            if (currentMoveState_hidden != null)
            {
                if (value.GetType() == currentMoveState_hidden.GetType())
                    return;

                if (currentMoveState_hidden != null)
                    currentMoveState_hidden.OnStateExit(this);
            }

            currentMoveState_hidden = value;
            currentMoveState_hidden.OnStateEnter(this);
        }
    }

    #region =========== UnityEvents =============

    protected override void Awake()
    {
        base.Awake();
        rBody = GetComponent<Rigidbody2D>();
        input = new InputSystemActions();
        input.Enable();
        currentMoveState_hidden = new GeneralMoveState();
    }

    private void Start()
    {
        currentPlayerHealth = playerMaxHealth;
        UI_PlayerHealth.Instance.SetHealthbarValue(currentPlayerHealth / playerMaxHealth);
    }

    private void OnEnable()
    {
        sprintStepEvent.OnSprintStep.AddListener(SprintStepEvent);
    }

    float focusCameraTimer = 0f;

    private void FixedUpdate()
    {
        UpdateHits();

        // ================= MoveState FixedUpdate ================
        currentMoveState.OnFixedUpdate(this);
        ProcessScheduledMoveState();
        // ========================================================

        if(focusCamera.activeInHierarchy)
        {
            focusCameraTimer += Time.fixedDeltaTime;

            if (focusCameraTimer >= 0.1f)
            {
                focusCameraTimer = 0f;
                focusCamera.SetActive(false);
            }
        }
    }

    private void Update()
    {
        // ================= MoveState Update ================
        currentMoveState.OnUpdate(this);
        ProcessScheduledMoveState();
        // ===================================================

        if (currentSkillAttackCooldown > 0f)
            currentSkillAttackCooldown -= Time.deltaTime;
        else
            currentSkillAttackCooldown = 0f;

        UI_PlayerHealth.Instance.SetSkilRadialValue(1f - (currentSkillAttackCooldown / skillAttackCooldown));

    }

    private void OnDisable()
    {
        sprintStepEvent.OnSprintStep.RemoveListener(SprintStepEvent);
    }

    #endregion

    protected float airTimeCounter = 0f;
    protected float jumpButtonTimer = 0f;
    protected float jumpHoldCounter = 0f;
    protected float walljumpCounter = 0f;
    protected float wallContactTimer = 0f;
    protected bool jumpHolding = false;
    protected bool wallContact = false;

    protected float dashTimer = 0f;
    protected float normalAttackTimer = 0f;

    protected float HurtTimer = 0f;
    protected float immobilityAternalTimer = 0f;

    const float walljumpControlDisable = 0.25f;
    const float attackControlDisable = 0.2f;

    bool prevFrameGrounded = false;

    #region =============== MoveState ================

    private MoveStateType scheduledMoveState = MoveStateType.None;

    private void ProcessScheduledMoveState()
    {
        if (scheduledMoveState == MoveStateType.None)
            return;

        switch (scheduledMoveState)
        {
            case MoveStateType.General:
                currentMoveState = new GeneralMoveState();
                break;
            case MoveStateType.Dash:
                currentMoveState = new DashMoveState();
                break;
            default:
                break;
        }
        scheduledMoveState = MoveStateType.None;
    }

    public enum MoveStateType
    {
        None,
        General,
        Dash,
        Dead
    }

    protected class MoveState
    {
        public MoveStateType type;

        /// <summary>
        /// MoveState 클래스 내에서 MoveState 전환을 예약합니다.
        /// </summary>
        protected void ScheduleMoveStateChange(PlayerController player,MoveStateType type)
        {
            player.scheduledMoveState = type;
        }

        /// <summary>
        /// 해당 State에 진입했을 때 호출됩니다.
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnStateEnter(PlayerController player) { }
        /// <summary>
        /// Update루프 때 호출됩니다.
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnUpdate(PlayerController player) { }
        /// <summary>
        /// FixedUpdate루프 때 호출됩니다.
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnFixedUpdate(PlayerController player) { }
        /// <summary>
        /// 다른 State로 전환되기 전 호출됩니다.
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnStateExit(PlayerController player) { }
    }

    /// <summary>
    /// 플레이어가 일반적으로 움직일 수 있는 상태입니다.
    /// </summary>
    protected class GeneralMoveState : MoveState
    {
        public GeneralMoveState()
        {
            type = MoveStateType.General;
        }

        private bool IsMoveAvailable(PlayerController player)
        {
            bool walljumping = Time.time - player.walljumpCounter > walljumpControlDisable;
            bool attacking = Time.time - player.normalAttackTimer > attackControlDisable;
            bool hurt = Time.time - player.HurtTimer > player.immobilityAfterHurt;

            return walljumping && attacking && hurt;
        }

        public override void OnStateEnter(PlayerController player)
        {
            base.OnStateEnter(player);

            player.input.Enable();

            if (!player.rBody.IsAwake())
                player.rBody.WakeUp();

            player.rBody.bodyType = RigidbodyType2D.Dynamic;
        }

        public override void OnUpdate(PlayerController player)
        {
            base.OnUpdate(player);

            bool isMoveAvailable = IsMoveAvailable(player);

            if (player.Grounded)
                player.airTimeCounter = 0f;
            else
                player.airTimeCounter += Time.deltaTime;

            player.wallContactTimer += Time.deltaTime;

            // Dash

            if (player.input.Player.Dash.WasPressedThisFrame() && Time.time - player.dashTimer >= player.dashCooldown)
            {
                player.dashTimer = Time.time;
                ScheduleMoveStateChange(player, MoveStateType.Dash);
                return;
            }

            // Normal Attack

            if(player.input.Player.Attack.WasPressedThisFrame() && Time.time - player.normalAttackTimer >= player.normalAttackInterval)
            {
                player.normalAttackTimer = Time.time;
                player.NormalAttackOnce();
            }

            // Skill Attack
            if( player.input.Player.Skill.WasPressedThisFrame() && player.currentSkillAttackCooldown <= 0f)
            {
                player.currentSkillAttackCooldown = player.skillAttackCooldown;
                player.SkillAtttack();

            }

            // Jump Mechanics

            if (isMoveAvailable)
            {
                if (player.input.Player.Jump.WasPressedThisFrame())
                {
                    if (player.IsJumpAvailable())
                    {
                        player.StartJump();
                    }
                    player.jumpButtonTimer = 0f;
                }
                else
                {
                    player.jumpButtonTimer += Time.deltaTime;
                }

                if (player.Grounded && player.prevFrameGrounded == false)
                {
                    if (player.jumpButtonTimer < player.jumpBufferThreshold)
                    {
                        player.debug_LastJumpType = JumpType.Buffered;
                        player.StartJump();
                    }

                    player.OnGroundEnter();
                }


                if (player.jumpHoldCounter < player.jumpHoldTime && player.jumpHolding)
                {
                    player.jumpHoldCounter += Time.deltaTime;

                    if (player.input.Player.Jump.IsPressed())
                        player.rBody.linearVelocityY = player.jumpForce * player.jumpHoldCurve.Evaluate(player.jumpHoldCounter / player.jumpHoldTime);
                    else
                        player.jumpHolding = false;
                }
                else
                {
                    player.jumpHolding = false;
                }
            }
            else
            {
                player.jumpHolding = false;
            }

            Vector2 movementInput = player.input.Player.Move.ReadValue<Vector2>();

            player.wallContact = false;

            // Wall Conatact

            if (movementInput.x != 0)
            {
                if (player.ObstacleForward)
                {
                    player.rBody.linearVelocityY = player.rBody.linearVelocityY * (1f - player.wallSlideDrag);
                    player.wallContact = true;
                    player.wallContactTimer = 0f;

                    if (player.input.Player.Jump.WasPressedThisFrame() && player.rBody.linearVelocityY > 0f && !player.Grounded)
                    {
                        player.walljumpCounter = Time.time;
                        player.rBody.linearVelocityX = -player.transform.right.x * player.moveSpeed;
                        player.StartJump();
                    }
                }
            }

            // Animation, Sound Updates

            if (player.Grounded)
                player.animator.SetBool("Grounded", true);
            else
                player.animator.SetBool("Grounded", false);

            
            if (movementInput.x != 0)
            {
                if (isMoveAvailable)
                {
                    if (player.ObstacleForward)
                        player.animator.SetBool("WallContact", true);
                    else
                        player.animator.SetBool("WallContact", false);

                    player.animator.SetBool("Running", true);
                }
                else
                {
                    player.animator.SetBool("WallContact", false);
                    player.animator.SetBool("Running", false);
                }
            }
            else
            {
                player.animator.SetBool("Running", false);
                player.animator.SetBool("WallContact", false);
            }

            player.animator.SetFloat("ySpeed", player.rBody.linearVelocityY);

            player.prevFrameGrounded = player.Grounded;
        }

        public override void OnFixedUpdate(PlayerController player)
        {
            // Horizontal Locomotion
            Vector2 movementInput = player.input.Player.Move.ReadValue<Vector2>();
            bool isMoveAvailable = IsMoveAvailable(player);

            if (isMoveAvailable)
            {
                if (movementInput.x > 0)
                {
                    player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    player.UpdateHits();
                }
                else if (movementInput.x < 0)
                {
                    player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    player.UpdateHits();
                }
                player.rBody.linearVelocityX = Mathf.Lerp(player.rBody.linearVelocityX, movementInput.x * player.moveSpeed, player.moveDamping);
            }
            else
            {
                player.rBody.linearVelocityX = Mathf.Lerp(player.rBody.linearVelocityX, 0f, player.moveDamping);
            }
        }

        public override void OnStateExit(PlayerController player)
        {
            player.jumpHoldCounter = 0f;
            player.jumpHolding = false;
            player.wallContact = false;
        }
    }

    /// <summary>
    /// 플레이어가 대시 능력을 사용하는 상태입니다.
    /// </summary>
    protected class DashMoveState : MoveState
    {
        const float dashTimeScale = 0.2f;
        const float dashDistanceBuffer = 0.2f;
        const float minDashDistance = 0.5f;

        public DashMoveState()
        {
            type = MoveStateType.Dash;
        }

        public override void OnStateEnter(PlayerController player)
        {
            base.OnStateEnter(player);
            player.rBody.bodyType = RigidbodyType2D.Kinematic;

            player.animator.SetBool("Dash", true);
            player.longSound.Play("Dash");

            UTsk_Dash(player).Forget();
        }

        private async UniTaskVoid UTsk_Dash(PlayerController player)
        {
            TimeManager.Instance.ChangeTimeScale(dashTimeScale);

            Vector3 startPos = player.transform.position;
            Vector3 targetPos = player.CheckForDashClear() - player.transform.right * dashDistanceBuffer;

            if(Vector3.Distance(startPos, targetPos) < minDashDistance)
            {
                targetPos = startPos;
            }

            for (float t = 0f; t < player.dashDuration; t += Time.unscaledDeltaTime)
            {
                float normalizedTime = t / player.dashDuration;
                player.transform.position = Vector3.LerpUnclamped(startPos, targetPos, player.dashCurve.Evaluate(normalizedTime));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            player.transform.position = targetPos;

            TimeManager.Instance.ChangeTimeScale(1f);

            ScheduleMoveStateChange(player, MoveStateType.General);
        }

        public override void OnStateExit(PlayerController player)
        {
            base.OnStateExit(player);

            player.rBody.bodyType = RigidbodyType2D.Dynamic;
            player.rBody.linearVelocity = player.transform.right * player.moveSpeed;

            player.animator.SetBool("Dash", false);
        }
    }

    /// <summary>
    /// 플레이어가 사망했을시의 상태입니다.
    /// </summary>
    protected class DeadMoveState : MoveState
    {
        public DeadMoveState() 
        {
            type = MoveStateType.Dead;
        }

        public override void OnStateEnter(PlayerController player)
        {
            player.rBody.bodyType = RigidbodyType2D.Static;

            List<Collider2D> col = new List<Collider2D>();
            player.rBody.GetAttachedColliders(col);
            col.ForEach(c => c.enabled = false);

            player.StartCoroutine(Cor_Dead(player));
            player.sound.Play("Die");
            player.animator.SetTrigger("Die");
        }

        IEnumerator Cor_Dead(PlayerController player)
        {
            yield return new WaitForSeconds(0.5f);
            player.deathParticle.Play();
            GameplayManager.Instance.GameOver();
        }
    }

    #endregion

    /// <summary>
    /// 플레이어게 대미지를 입힙니다.
    /// </summary>
    /// <param name="force"> 가하는 물리력 </param>
    /// <param name="damage"> 피해량 </param>
    public void HurtPlayer(Vector2 force,float damage)
    {
        if (Time.time - HurtTimer < invincibleAfterHurt)
            return;

        currentPlayerHealth -= damage;
        UI_PlayerHealth.Instance.SetHealthbarValue(currentPlayerHealth / playerMaxHealth, true);

        if (currentPlayerHealth <= 0)
        {
            KillPlayer();
            return;
        }

        rBody.linearVelocity = force;

        animator.SetTrigger("Hurt");
        sound.Play("Hurt");

        HurtTimer = Time.time;

    }

    /// <summary>
    /// 플레이어의 체력을 회복하거나 감소시킵니다.
    /// </summary>
    /// <param name="value"><param>
    public void AddPlayerHealth(float value)
    {
        currentPlayerHealth += value;

        if(currentPlayerHealth < 0) { KillPlayer(); }
        else if(currentPlayerHealth > playerMaxHealth) { currentPlayerHealth = playerMaxHealth; }

    }

    public void SprintStepEvent()
    {
        sound.Play("Step");
    }


    private void KillPlayer()
    {
        OnPlayerDied.Invoke();
        currentMoveState = new DeadMoveState();
    }

    private void OnGroundEnter()
    {
        sound.Play("Step");
    }

    private Vector3 CheckForDashClear()
    {
        RaycastHit2D top = Physics2D.Raycast(RCO_Head.position, transform.right, dashDistance, groundLayer);
        RaycastHit2D bottom = Physics2D.Raycast(RCO_Forward.position, transform.right, dashDistance, groundLayer);

        if (top)
            return new Vector3(top.point.x,transform.position.y);
        if(bottom)
            return new Vector3(bottom.point.x, transform.position.y);
        else            
            return transform.position + transform.right * dashDistance;

    }

    private void StartJump()
    {

        jumpHolding = true;
        jumpHoldCounter = 0f;
        rBody.linearVelocityY = jumpForce;
    }

    private void NormalAttackOnce()
    {
        animator.SetTrigger("Attack");
        sound.Play("Attack");

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackRange.bounds.center, attackRange.bounds.size, 0f);

        foreach (var hit in hits)
        {
            Vector3 pushDirection = (hit.transform.position - transform.position).normalized;
            Targetable_Base target;

            if (hit.TryGetComponent<Targetable_Base>(out target))
            {
                focusCamera.SetActive(true);
                target.OnDamaged(normalAttackDamage, pushDirection);
                Instantiate(slashEffectObject, hit.transform.position, Quaternion.identity);
            }
        }

        Vector2 pushDir = transform.right;
        rBody.linearVelocityX = pushDir.x * normalAttackPushForce;

    }

    private void SkillAtttack()
    {
        currentSkillAttackCooldown = skillAttackCooldown;
        GameObject skillObj = GameObject.Instantiate(skillAttackObject, RCO_Head.position + transform.right * 0.5f, Quaternion.identity);
        longSound.Play("SkillAttack");
        animator.SetTrigger("Attack");
        skillObj.transform.up = transform.right;
    }

    private bool IsJumpAvailable()
    {
        if (Grounded)
        {
            debug_LastJumpType = JumpType.Normal;
            return true;
        }

        if (airTimeCounter < coyoteTimeThreshold && rBody.linearVelocityY < 0) // coyote time
        {
            debug_LastJumpType = JumpType.Coyote;
            return true;
        }

        if(wallContactTimer < coyoteTimeThreshold && rBody.linearVelocityY < 0) // walljump coyote
        {
            if (Time.time - walljumpCounter < coyoteTimeThreshold)
                return false;

            debug_LastJumpType = JumpType.Coyote; 
            return true;
        }

        // if we reach here, jump is not available
        return false;

    }

    private void UpdateHits()
    {
        hit_head = Physics2D.Raycast(RCO_Head.position, transform.right, forwardRaycastLength, groundLayer);
        hit_forward = Physics2D.Raycast(RCO_Forward.position, transform.right, forwardRaycastLength, groundLayer);
        hit_foot_L = Physics2D.Raycast(RCO_Foot_L.position, Vector2.down, footRaycastLength, groundLayer | halfGroundLayer);
        hit_foot_R = Physics2D.Raycast(RCO_Foot_R.position, Vector2.down, footRaycastLength, groundLayer | halfGroundLayer);

        debug_Grounded = Grounded;
        debug_ObstacleForward = hit_forward.collider != null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(RCO_Foot_L.position, RCO_Foot_L.position + Vector3.down * footRaycastLength);
        Gizmos.DrawLine(RCO_Foot_R.position, RCO_Foot_R.position + Vector3.down * footRaycastLength);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(RCO_Forward.position, RCO_Forward.position + transform.right * forwardRaycastLength);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(RCO_Head.position, RCO_Head.position + transform.right * forwardRaycastLength);

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        DrawArrow.ForGizmo(transform.position, Vector3.right * dashDistance,dashDistance * 0.3f);
    }
}
