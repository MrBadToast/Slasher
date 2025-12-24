using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : StaticMonoBehaviour<PlayerController>
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;

    [SerializeField] private float jumpHoldTime = 0.2f;
    [SerializeField] private float coyoteTimeThreshold = 0.05f;
    [SerializeField] private float jumpBufferThreshold = 0.05f;

    [Header("Hits")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float footRaycastLength = 0.1f;
    [SerializeField] private float forwardRaycastLength = 0.1f;

    [Header("ChildReferences")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform RCO_Foot_L;
    [SerializeField] private Transform RCO_Foot_R;
    [SerializeField] private Transform RCO_Forward;
    [SerializeField] private Transform RCO_Head;
    [SerializeField] private SimpleSoundModule sound;

    private InputSystemActions input;
    private Rigidbody2D rBody;

    public bool Grounded { get { return hit_foot_L || hit_foot_R; } }
    public bool ObstacleForward { get { return hit_forward || hit_head; } }
    public bool LedgeHolding { get { return !hit_head || hit_forward; } }

    RaycastHit2D hit_head;
    RaycastHit2D hit_forward;
    RaycastHit2D hit_foot_L;
    RaycastHit2D hit_foot_R;

    [Header("Debug")]
    public bool debug_Grounded;
    public bool debug_ObstacleForward;

    protected override void Awake()
    {
        base.Awake();
        rBody = GetComponent<Rigidbody2D>();
        input = new InputSystemActions();
        input.Enable();
    }

    [SerializeField] float airTimeCounter = 0f;
    [SerializeField] float jumpButtonTimer = 0f;
    [SerializeField] float jumpHoldCounter = 0f;
    [SerializeField] bool jumpHolding = false;
    [SerializeField] bool prevFrameGrounded = false;

    private void FixedUpdate()
    {
        UpdateHits();

        if (Grounded)
            airTimeCounter = 0f;
        else
            airTimeCounter += Time.fixedDeltaTime;


        if (input.Player.Jump.WasPressedThisFrame())
        {
            if (IsJumpAvailable())
            {
                StartJump();
            }
            jumpButtonTimer = 0f;
        }
        else
        {
            jumpButtonTimer += Time.fixedDeltaTime;
        }


        if(Grounded && prevFrameGrounded == false)
        {
            if (jumpButtonTimer < jumpBufferThreshold)
            {
                Debug.Log("Buffered Jump Triggered");
                StartJump();
            }
               
            OnGroundEnter();
        }


        if (jumpHoldCounter < jumpHoldTime && jumpHolding)
        {
            jumpHoldCounter += Time.fixedDeltaTime;

            if (input.Player.Jump.IsPressed())
                rBody.linearVelocityY = jumpForce;
            else
                jumpHolding = false;
        }
        else
        {
            jumpHolding = false;
        }


        Vector2 movementInput = input.Player.Move.ReadValue<Vector2>();

        if (movementInput.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (movementInput.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);


        rBody.linearVelocityX = movementInput.x * moveSpeed * Time.fixedDeltaTime * 50f;


        prevFrameGrounded = Grounded;
    }

    private void OnGroundEnter()
    {

    }

    private void StartJump()
    {
        Debug.Log("Jumped");
        jumpHolding = true;
        jumpHoldCounter = 0f;
        rBody.linearVelocityY = jumpForce;
    }

    private bool IsJumpAvailable()
    {
        if (Grounded)
        {
            Debug.Log("Normal Jump Triggered");
            return true;
        }

        if (airTimeCounter < coyoteTimeThreshold)
        {
            Debug.Log("Coyote Time Triggered");
            return true;
        }

        // if we reach here, jump is not available
        return false;

    }


    private void UpdateHits()
    {
        hit_head = Physics2D.Raycast(RCO_Head.position, transform.right, footRaycastLength, groundLayer);
        hit_forward = Physics2D.Raycast(RCO_Forward.position, transform.right, forwardRaycastLength, groundLayer);
        hit_foot_L = Physics2D.Raycast(RCO_Foot_L.position, Vector2.down, footRaycastLength, groundLayer);
        hit_foot_R = Physics2D.Raycast(RCO_Foot_R.position, Vector2.down, footRaycastLength, groundLayer);

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

}
