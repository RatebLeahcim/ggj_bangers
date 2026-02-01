using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum horizontal movement speed")]
    [SerializeField] private float moveSpeed = 8f;
    
    [Tooltip("How quickly the player accelerates to max speed")]
    [SerializeField] private float acceleration = 50f;
    
    [Tooltip("How quickly the player decelerates when no input is applied")]
    [SerializeField] private float deceleration = 40f;
    
    [Tooltip("Speed multiplier when in the air")]
    [SerializeField] private float airControlMultiplier = 0.1f;
    
    [Header("Rolling Rotation")]
    [Tooltip("If true, player rotates based on horizontal displacement (rolling effect)")]
    [SerializeField] private bool enableRollingRotation = true;
    
    [Tooltip("Degrees to rotate per unit of horizontal distance traveled. Positive = clockwise when moving right.")]
    [SerializeField] private float degreesPerUnit = 180f;
    
    [Tooltip("If true, freezes Rigidbody2D rotation to prevent physics momentum. Rotation is controlled purely by displacement.")]
    [SerializeField] private bool freezePhysicsRotation = true;

    [Header("Jump Settings")]
    [Tooltip("Initial upward velocity when jumping")]
    [SerializeField] private float jumpForce = 8f;
    
    [Tooltip("Multiplier applied to gravity when falling (for better game feel)")]
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    
    [Tooltip("Multiplier applied when jump is released early (for variable jump height)")]
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Ground Detection")]
    [Tooltip("Layer mask for ground detection")]
    [SerializeField] private LayerMask groundLayer;
    
    [Tooltip("Radius of the ground check circle")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    
    [Tooltip("Offset from transform position for ground check")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);

    [Header("References")]
    [Tooltip("Optional: Assign a Rigidbody2D manually, otherwise it will be auto-detected")]
    [SerializeField] private Rigidbody2D rb;
    
    [Tooltip("Optional: Assign a GrapplingHook manually, otherwise it will be auto-detected")]
    [SerializeField] private GrapplingHook grapplingHook;

    private PlayerStateMachine _stateMachine;
    private PlayerInputBridge _inputBridge;
    
    private bool _jumpRequested;
    private bool _jumpHeld;
    
    private float _jumpBufferTime = 0.15f;
    private float _jumpBufferCounter;
    
    // Rolling rotation state
    private float _previousPositionX;
    private float _accumulatedRotation;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        if (grapplingHook == null)
        {
            grapplingHook = GetComponent<GrapplingHook>();
        }
        
        // Apply zero-friction material to prevent wall sticking
        ApplyZeroFrictionMaterial();
    }
    
    private void ApplyZeroFrictionMaterial()
    {
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider != null && playerCollider.sharedMaterial == null)
        {
            PhysicsMaterial2D noFriction = new PhysicsMaterial2D("PlayerNoFriction");
            noFriction.friction = 0f;
            noFriction.bounciness = 0f;
            playerCollider.sharedMaterial = noFriction;
        }
    }

    private void Start()
    {
        _stateMachine = PlayerStateMachine.Instance;
        _inputBridge = PlayerInputBridge.Instance;
        
        // Initialize rolling state
        _previousPositionX = transform.position.x;
        _accumulatedRotation = transform.eulerAngles.z;
        
        if (freezePhysicsRotation && rb != null)
        {
            rb.constraints = rb.constraints | RigidbodyConstraints2D.FreezeRotation;
            rb.angularVelocity = 0f;
        }
    }

    private void Update()
    {
        if(PlayerStateMachine.Instance.Conditions.IsInteracting){ return; }
        if (_inputBridge != null)
        {
            if (_inputBridge.Consume(PlayerInputType.Space, out bool jumpPressed))
            {
                if (jumpPressed)
                {
                    HandleSpacePressed();
                }
            }
            
            _jumpHeld = UnityEngine.InputSystem.Keyboard.current?.spaceKey.isPressed ?? false;
        }
        
        if (_jumpBufferCounter > 0)
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
        
        if (_jumpBufferCounter > 0 && IsGrounded() && !IsHanging())
        {
            _jumpRequested = true;
            _jumpBufferCounter = 0;
        }
        
        if (_stateMachine != null)
        {
            _stateMachine.Conditions.IsGrounded = IsGrounded();
        }
    }

    private void HandleSpacePressed()
    {
        if (IsHanging())
        {
            grapplingHook?.ReleaseHook();
        }
        else if (IsGrounded())
        {
            _jumpBufferCounter = _jumpBufferTime;
        }
        else
        {
            if (grapplingHook != null && !grapplingHook.IsActive)
            {
                grapplingHook.TryShootHook();
            }
        }
    }

    private void FixedUpdate()
    {
        if(PlayerStateMachine.Instance.Conditions.IsInteracting){ return; }
        if (IsHanging()) return;
        
        Vector2 moveInput = Vector2.zero;
        if (_inputBridge != null)
        {
            _inputBridge.Consume(PlayerInputType.Move, out moveInput);
        }
        
        ApplyHorizontalMovement(moveInput);
        HandleJump();
        ApplyGravityModifiers();
        ApplyRollingRotation();
        
        if (_stateMachine != null)
        {
            _stateMachine.Conditions.Move = moveInput;
        }
    }
    
    private void ApplyRollingRotation()
    {
        if (!enableRollingRotation) return;
        
        if (grapplingHook != null && grapplingHook.IsActive) return;
        
        float currentX = transform.position.x;
        float displacement = currentX - _previousPositionX;
        _previousPositionX = currentX;
        
        if (IsGrounded())
        {
            float rotationDelta = -displacement * degreesPerUnit;
            _accumulatedRotation += rotationDelta;
            
            transform.rotation = Quaternion.Euler(0f, 0f, _accumulatedRotation);
        }
        else
        {
            _accumulatedRotation = transform.eulerAngles.z;
            if (_accumulatedRotation > 180f) _accumulatedRotation -= 360f;
        }
    }

    private void ApplyHorizontalMovement(Vector2 moveInput)
    {
        float targetSpeed = moveInput.x * moveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;
        
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        
        if (!IsGrounded())
        {
            accelRate *= airControlMultiplier;
        }
        
        float movement = speedDifference * accelRate * Time.fixedDeltaTime;
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
        
        float clampedX = Mathf.Clamp(rb.linearVelocity.x, -moveSpeed, moveSpeed);
        rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);
        
        if (_stateMachine != null)
        {
            _stateMachine.Conditions.IsMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        }
    }

    private void HandleJump()
    {
        if (_jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            
            _jumpRequested = false;
            
            if (_stateMachine != null)
            {
                _stateMachine.Conditions.IsJumping = true;
            }
        }
    }

    private void ApplyGravityModifiers()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1) * Time.fixedDeltaTime;
            
            if (_stateMachine != null)
            {
                _stateMachine.Conditions.IsJumping = false;
            }
        }
        else if (rb.linearVelocity.y > 0 && !_jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private bool IsHanging()
    {
        return grapplingHook != null && grapplingHook.IsHooked;
    }

    public bool IsGrounded()
    {
        Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
        return Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer) != null;
    }

    public Vector2 Velocity => rb.linearVelocity;

    private void OnDrawGizmosSelected()
    {
        Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
        
        Gizmos.color = IsGrounded() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
    }
}
