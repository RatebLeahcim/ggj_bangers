using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    private static PlayerEffects _instance;
    public static PlayerEffects Instance => _instance;

    [Header("Smoke Effect Settings")]
    [Tooltip("The Animator component for the smoke effect")]
    [SerializeField] private Animator smokeAnimator;
    
    [Tooltip("The SpriteRenderer for the smoke effect (used for flipping)")]
    [SerializeField] private SpriteRenderer smokeSpriteRenderer;
    
    [Tooltip("The GameObject containing the smoke effect")]
    [SerializeField] private GameObject smokeEffectObject;
    
    [Tooltip("Offset from player position for the smoke effect (before direction flip)")]
    [SerializeField] private Vector2 smokeOffset = new Vector2(0.5f, -0.5f);
    
    [Tooltip("Name of the trigger parameter in the Animator")]
    [SerializeField] private string smokeTriggerName = "PlaySmoke";
    
    [Tooltip("Minimum time between smoke triggers (prevents spam)")]
    [SerializeField] private float smokeCooldown = 0.2f;

    [Header("Movement Detection")]
    [Tooltip("Minimum horizontal velocity to consider the player moving")]
    [SerializeField] private float moveThreshold = 0.1f;

    private float _lastSmokeTime;
    private Transform _smokeTransform;
    private float _lastMoveDirection = 0f;
    private bool _wasGrounded = false;
    private bool _wasMoving = false;
    
    private float _smokeFixedY = 0f;
    private float _smokeXOffset = 0f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;

        if (smokeEffectObject != null)
        {
            _smokeTransform = smokeEffectObject.transform;
            
            _smokeTransform.SetParent(null);
            
            _smokeTransform.rotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        
        // Since we unparented the smoke effect, we need to manually destroy it
        if (smokeEffectObject != null)
        {
            Destroy(smokeEffectObject);
        }
    }

    private void Update()
    {
        if (PlayerStateMachine.Instance == null) return;
        
        var conditions = PlayerStateMachine.Instance.Conditions;
        bool isGrounded = conditions.IsGrounded;
        float moveX = conditions.Move.x;
        bool isMoving = Mathf.Abs(moveX) > moveThreshold;

        if (isGrounded)
        {
            bool justStartedMoving = isMoving && !_wasMoving;
            bool justLandedWhileMoving = isMoving && !_wasGrounded;
            bool changedDirection = isMoving && _lastMoveDirection != 0f && 
                                    Mathf.Sign(moveX) != Mathf.Sign(_lastMoveDirection);

            if (justStartedMoving || justLandedWhileMoving || changedDirection)
            {
                TriggerSmokeEffect(moveX);
            }
        }

        // Update tracking state
        _wasGrounded = isGrounded;
        _wasMoving = isMoving;
        if (isMoving)
        {
            _lastMoveDirection = moveX;
        }
    }

    private void LateUpdate()
    {
        if (_smokeTransform != null)
        {
            _smokeTransform.position = new Vector3(
                transform.position.x + _smokeXOffset,
                _smokeFixedY,
                _smokeTransform.position.z
            );
            
            _smokeTransform.rotation = Quaternion.identity;
        }
    }

    public void TriggerSmokeEffect(float movementDirection)
    {
        if (smokeAnimator == null || smokeEffectObject == null)
        {
            Debug.LogWarning("PlayerEffects: Smoke animator or effect object not assigned!");
            return;
        }

        if (Time.time - _lastSmokeTime < smokeCooldown)
        {
            return;
        }
        _lastSmokeTime = Time.time;

        float directionSign = Mathf.Sign(movementDirection);
        _smokeXOffset = -directionSign * smokeOffset.x; // Store offset for LateUpdate
        _smokeFixedY = transform.position.y + smokeOffset.y; // Store fixed Y position
        
        _smokeTransform.position = new Vector3(
            transform.position.x + _smokeXOffset,
            _smokeFixedY,
            0f
        );

        _smokeTransform.rotation = Quaternion.identity;

        if (smokeSpriteRenderer != null)
        {
            smokeSpriteRenderer.flipX = directionSign < 0;
        }

        smokeAnimator.SetTrigger(smokeTriggerName);
        
        Debug.Log($"Smoke triggered! Direction: {directionSign}, Offset: ({_smokeXOffset}, {_smokeFixedY})");
    }

    public bool IsSmokeEffectConfigured => smokeAnimator != null && smokeEffectObject != null;
}

