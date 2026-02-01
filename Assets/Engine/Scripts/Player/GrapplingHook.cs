using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GrapplingHook : MonoBehaviour
{
    [Header("Grappling Hook Settings")]
    [Tooltip("Speed at which the hook travels upward")]
    [SerializeField] private float hookSpeed = 18f;
    
    [Tooltip("Layer mask for surfaces the hook can attach to")]
    [SerializeField] private LayerMask grappleableLayer;
    
    [Tooltip("Width of the hook raycast")]
    [SerializeField] private float hookWidth = 0.2f;
    
    [Tooltip("Offset from player center where the hook originates (e.g., left side of player)")]
    [SerializeField] private Vector2 hookOriginOffset = new Vector2(-0.5f, 0f);

    [Header("Swing Settings")]
    [Tooltip("Overall speed multiplier for all swing forces (higher = snappier swinging)")]
    [SerializeField] private float swingSpeedScale = 1.5f;
    
    [Tooltip("Base force applied when swinging left/right")]
    [SerializeField] private float swingForce = 12f;
    
    [Tooltip("Damping applied to swing motion (lower = faster deceleration, 1.0 = no damping)")]
    [SerializeField] private float swingDamping = 0.98f;
    
    [Tooltip("Maximum swing speed - caps how fast the player can swing")]
    [SerializeField] private float maxSwingSpeed = 10f;
    
    [Header("Pendulum Gravity")]
    [Tooltip("Gravity multiplier when swinging DOWN (higher = faster acceleration through bottom)")]
    [SerializeField] private float descendingGravityMultiplier = 2.0f;
    
    [Tooltip("Gravity multiplier when swinging UP (lower = more resistance, feels heavier)")]
    [SerializeField] private float ascendingGravityMultiplier = 1.5f;
    
    [Header("Momentum Building")]
    [Tooltip("Effectiveness when pushing WITH swing direction (0-1)")]
    [SerializeField] private float withMomentumEffectiveness = 0.6f;
    
    [Tooltip("Effectiveness when starting from stationary - allows initiating a swing (0-1)")]
    [SerializeField] private float stationaryEffectiveness = 0.7f;
    
    [Tooltip("Braking force when pushing AGAINST swing direction (higher = faster stop)")]
    [SerializeField] private float againstMomentumBraking = 0.1f;
    
    [Tooltip("Velocity threshold to consider player as stationary")]
    [SerializeField] private float stationaryThreshold = 0.5f;
    
    [Tooltip("How much height reduces swing effectiveness (0 = no reduction, 1 = full reduction when at top)")]
    [SerializeField] private float heightPenaltyStrength = 0.6f;
    
    [Header("Release Settings")]
    [Tooltip("Multiplier applied to horizontal velocity when releasing the hook (1.0 = no boost)")]
    [SerializeField] private float releaseHorizontalBoostMultiplier = 1.3f;
    
    [Tooltip("Additional upward boost when releasing (helps maintain height)")]
    [SerializeField] private float releaseUpwardBoost = 2f;

    [Header("Visual Settings")]
    [Tooltip("Line renderer for the rope visual")]
    [SerializeField] private LineRenderer ropeRenderer;
    
    [Tooltip("Color of the rope")]
    [SerializeField] private Color ropeColor = Color.white;
    
    [Tooltip("Width of the rope")]
    [SerializeField] private float ropeWidth = 0.05f;

    [Header("Status")]
    public GrapplingHookStatus Status = new();

    private Rigidbody2D _rb;
    private PlayerStateMachine _stateMachine;
    private PlayerInputBridge _inputBridge;
    
    private enum HookState { Idle, Shooting, Attached, Retracting }
    private HookState _currentState = HookState.Idle;
    
    private Vector2 _hookPosition;
    private Vector2 _attachPoint;
    private Rigidbody2D _attachedBody;
    private float _currentRopeLength;
    private float _shootDistance;

    public bool IsHooked => _currentState == HookState.Attached;
    public bool IsShooting => _currentState == HookState.Shooting;
    public bool IsActive => _currentState != HookState.Idle;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        SetupRopeRenderer();
    }

    private void Start()
    {
        _stateMachine = PlayerStateMachine.Instance;
        _inputBridge = PlayerInputBridge.Instance;
    }

    private void SetupRopeRenderer()
    {
        if (ropeRenderer == null)
        {
            GameObject ropeObj = new GameObject("RopeRenderer");
            ropeObj.transform.SetParent(transform);
            ropeRenderer = ropeObj.AddComponent<LineRenderer>();
        }
        
        ropeRenderer.positionCount = 2;
        ropeRenderer.startWidth = ropeWidth;
        ropeRenderer.endWidth = ropeWidth;
        ropeRenderer.material = new Material(Shader.Find("Sprites/Default"));
        ropeRenderer.startColor = ropeColor;
        ropeRenderer.endColor = ropeColor;
        ropeRenderer.enabled = false;
    }

    private void Update()
    {
        Status.Regenerate(Time.deltaTime);
        
        switch (_currentState)
        {
            case HookState.Shooting:
                UpdateShooting();
                break;
            case HookState.Attached:
                UpdateAttached();
                break;
            case HookState.Retracting:
                UpdateRetracting();
                break;
        }
        
        UpdateRopeVisual();
    }

    private void FixedUpdate()
    {
        if (_currentState == HookState.Attached)
        {
            ApplySwingPhysics();
        }
    }

    public bool TryShootHook()
    {
        if (_currentState != HookState.Idle) return false;
        if (!Status.HasRopeAvailable()) return false;
        
        _hookPosition = (Vector2)transform.position + hookOriginOffset;
        _shootDistance = 0f;
        _currentState = HookState.Shooting;
        
        // Initialize rope positions BEFORE enabling to prevent flash of stale positions
        Vector2 ropeOrigin = (Vector2)transform.position + hookOriginOffset;
        ropeRenderer.SetPosition(0, ropeOrigin);
        ropeRenderer.SetPosition(1, _hookPosition);
        ropeRenderer.enabled = true;
        
        return true;
    }

    public void ReleaseHook()
    {
        if (_currentState == HookState.Attached)
        {
            ApplyReleaseBoost();
            
            _attachedBody = null;
            _currentState = HookState.Idle;
            ropeRenderer.enabled = false;
            
            if (_stateMachine != null)
            {
                _stateMachine.Conditions.IsHanging = false;
            }
        }
    }
    
    private void ApplyReleaseBoost()
    {
        Vector2 currentVelocity = _rb.linearVelocity;
        
        // Only boost horizontal velocity, keep vertical unchanged (before adding upward boost)
        float boostedHorizontal = currentVelocity.x * releaseHorizontalBoostMultiplier;
        float boostedVertical = currentVelocity.y + releaseUpwardBoost;
        
        _rb.linearVelocity = new Vector2(boostedHorizontal, boostedVertical);
    }

    private void UpdateShooting()
    {
        float maxDistance = Status.CurrentRopeLength;
        float travelDistance = hookSpeed * Time.deltaTime;
        
        Vector2 startPos = _hookPosition;
        Vector2 direction = Vector2.up;
        
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, travelDistance, grappleableLayer);
        
        if (hit.collider != null)
        {
            _attachPoint = hit.point;
            _attachedBody = hit.collider.attachedRigidbody;
            _currentRopeLength = Vector2.Distance(transform.position, _attachPoint);
            _hookPosition = _attachPoint;
            _currentState = HookState.Attached;
            
            if (_stateMachine != null)
            {
                _stateMachine.Conditions.IsHanging = true;
            }
            return;
        }
        
        _hookPosition += direction * travelDistance;
        _shootDistance += travelDistance;
        
        if (_shootDistance >= maxDistance)
        {
            _currentState = HookState.Retracting;
        }
    }

    private void UpdateRetracting()
    {
        float retractSpeed = hookSpeed * 1.5f;
        Vector2 toPlayer = ((Vector2)transform.position - _hookPosition).normalized;
        _hookPosition += toPlayer * retractSpeed * Time.deltaTime;
        
        float distanceToPlayer = Vector2.Distance(_hookPosition, transform.position);
        if (distanceToPlayer < 0.5f)
        {
            _currentState = HookState.Idle;
            ropeRenderer.enabled = false;
        }
    }

    private void UpdateAttached()
    {
        if (_attachedBody != null)
        {
            _attachPoint = _attachedBody.position + (_attachPoint - (Vector2)_attachedBody.transform.position);
        }
    }

    private void ApplySwingPhysics()
    {
        Vector2 playerPos = transform.position;
        Vector2 toAttach = _attachPoint - playerPos;
        float currentDistance = toAttach.magnitude;
        
        if (currentDistance > _currentRopeLength)
        {
            Vector2 correctionDirection = toAttach.normalized;
            float correction = currentDistance - _currentRopeLength;
            _rb.position = playerPos + correctionDirection * correction;
            
            Vector2 ropeDirection = correctionDirection;
            Vector2 velocityAlongRope = Vector2.Dot(_rb.linearVelocity, ropeDirection) * ropeDirection;
            if (Vector2.Dot(_rb.linearVelocity, ropeDirection) < 0)
            {
                _rb.linearVelocity -= velocityAlongRope;
            }
        }
        
        Vector2 moveInput = Vector2.zero;
        if (_inputBridge != null)
        {
            _inputBridge.Consume(PlayerInputType.Move, out moveInput);
        }
        
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            // Tangent perpendicular to rope - positive X input should swing right
            Vector2 tangent = new Vector2(toAttach.normalized.y, -toAttach.normalized.x);
            
            // Get current tangential velocity (how fast we're swinging)
            float tangentialVelocity = Vector2.Dot(_rb.linearVelocity, tangent);
            
            // Check if already at max swing speed in this direction
            if (Mathf.Abs(tangentialVelocity) >= maxSwingSpeed && 
                Mathf.Sign(moveInput.x) == Mathf.Sign(tangentialVelocity))
            {
                // Already at max speed in this direction, don't add more force
            }
            else
            {
                // Calculate how much the input aligns with current swing direction
                float inputAlignment = Mathf.Sign(moveInput.x) * Mathf.Sign(tangentialVelocity);
                
                // Calculate height factor - reduce force when player is high up
                float heightDifference = _attachPoint.y - playerPos.y;
                float normalizedHeight = Mathf.Clamp01(heightDifference / _currentRopeLength);
                float heightFactor = Mathf.Lerp(1f - heightPenaltyStrength, 1f, normalizedHeight);
                
                // Determine swing effectiveness based on momentum alignment
                float swingEffectiveness;
                float scaledSwingForce = swingForce * swingSpeedScale;
                
                if (Mathf.Abs(tangentialVelocity) < stationaryThreshold)
                {
                    // Nearly stationary - good effectiveness to START a swing
                    swingEffectiveness = stationaryEffectiveness * heightFactor;
                    _rb.AddForce(tangent * moveInput.x * scaledSwingForce * swingEffectiveness);
                }
                else if (inputAlignment > 0)
                {
                    swingEffectiveness = withMomentumEffectiveness * heightFactor;
                    _rb.AddForce(tangent * moveInput.x * scaledSwingForce * swingEffectiveness);
                }
                else
                {
                    float brakingForce = againstMomentumBraking * swingSpeedScale * Mathf.Abs(tangentialVelocity);
                    Vector2 brakeDirection = -_rb.linearVelocity.normalized;
                    _rb.AddForce(brakeDirection * brakingForce, ForceMode2D.Impulse);
                }
            }
        }
        
        // Apply damping
        _rb.linearVelocity *= swingDamping;
        
        // Apply pendulum gravity modifiers
        ApplyPendulumGravity();
        
        // Clamp to max swing speed
        Vector2 tangentDir = new Vector2(toAttach.normalized.y, -toAttach.normalized.x);
        float currentTangentSpeed = Vector2.Dot(_rb.linearVelocity, tangentDir);
        if (Mathf.Abs(currentTangentSpeed) > maxSwingSpeed)
        {
            Vector2 tangentVelocity = currentTangentSpeed * tangentDir;
            Vector2 radialVelocity = _rb.linearVelocity - tangentVelocity;
            _rb.linearVelocity = radialVelocity + tangentDir * Mathf.Sign(currentTangentSpeed) * maxSwingSpeed;
        }
    }
    
    private void ApplyPendulumGravity()
    {
        // Check if player is moving up or down
        bool isAscending = _rb.linearVelocity.y > 0.1f;
        
        // Apply extra gravity based on direction
        float gravityMultiplier;
        if (isAscending)
        {
            gravityMultiplier = ascendingGravityMultiplier - 1f;
        }
        else
        {
            gravityMultiplier = descendingGravityMultiplier - 1f;
        }
        
        // Add extra gravity force
        Vector2 extraGravity = Physics2D.gravity * gravityMultiplier * Time.fixedDeltaTime;
        _rb.linearVelocity += extraGravity;
    }

    private void UpdateRopeVisual()
    {
        if (!ropeRenderer.enabled) return;
        
        Vector2 ropeOrigin = (Vector2)transform.position + hookOriginOffset;
        ropeRenderer.SetPosition(0, ropeOrigin);
        ropeRenderer.SetPosition(1, _hookPosition);
    }

    private void OnDrawGizmosSelected()
    {
        if (Status != null)
        {
            Vector2 origin = (Vector2)transform.position + hookOriginOffset;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + Vector2.up * Status.CurrentRopeLength);
        }
        
        if (_currentState == HookState.Attached)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_attachPoint, 0.3f);
        }
    }
}
