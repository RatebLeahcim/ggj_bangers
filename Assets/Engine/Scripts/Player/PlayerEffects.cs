using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerEffects : MonoBehaviour
{
    private static PlayerEffects _instance;
    public static PlayerEffects Instance => _instance;

    public FMODUnity.EventReference DamageSoundEvent;

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


    [Tooltip("Color Change effect for player renderer when damaged")]
    [SerializeField] private Color _damageColor;

    [Tooltip("How long to change player color when damaged")]
    [SerializeField] private float _damageColorChangeTime = 0.5f;
    private Coroutine _damageColorRoutine;

    private float _lastSmokeTime;
    private Transform _smokeTransform;
    private float _lastMoveDirection = 0f;
    private bool _wasGrounded = false;
    private bool _wasMoving = false;
    
    private float _smokeFixedY = 0f;
    private float _smokeXOffset = 0f;

    // sprite squash / stretching
    [SerializeField] float maxStretch = 0.25f;
    [SerializeField] float squashStrength = 0.15f;
    [SerializeField] float velocityForMax = 6f;
    [SerializeField] float returnSpeed = 12f;

    public Transform TapeVisualTransform;

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
        bool justStartedMoving = isMoving && !_wasMoving;

        if (isGrounded)
        {
            bool justLandedWhileMoving = isMoving && !_wasGrounded;
            bool changedDirection = isMoving && _lastMoveDirection != 0f && 
                                    Mathf.Sign(moveX) != Mathf.Sign(_lastMoveDirection);

            if (justStartedMoving || justLandedWhileMoving || changedDirection)
            {
                TriggerSmokeEffect(moveX);
            }
        }

        if(isMoving)
        {
            Vector2 vel = justStartedMoving ? PlayerStateMachine.Instance.Rigidbod.linearVelocity * 2 : PlayerStateMachine.Instance.Rigidbod.linearVelocity;
            ApplyRollScaleWarp(vel);
        }
        else
        {
            if(TapeVisualTransform.localScale != Vector3.one){TapeVisualTransform.localScale = Vector3.one; }
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
        
    }

    void ApplyRollScaleWarp(Vector2 velocity)
    {
        Vector3 targetScale = Vector3.one;

        float speed01 = Mathf.Clamp01(velocity.magnitude / velocityForMax);
        if (speed01 < 0.01f)
        {
            TapeVisualTransform.localScale = Vector3.Lerp(
                TapeVisualTransform.localScale,
                Vector3.one,
                Time.deltaTime * returnSpeed
            );
            return;
        }

        Vector2 dir = velocity.normalized;

        float stretch = 1f + maxStretch * speed01;
        float squash  = 1f - squashStrength * speed01;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            targetScale.x = stretch;
            targetScale.y = squash;
        }
        else
        {
            targetScale.x = squash;
            targetScale.y = stretch;
        }

        TapeVisualTransform.localScale = Vector3.Lerp(
            TapeVisualTransform.localScale,
            targetScale,
            Time.deltaTime * returnSpeed
        );
    }

    public void DamageVFX()
    {
        _damageColorRoutine ??= StartCoroutine(nameof(DamageColorRoutine));
        FMODUnity.RuntimeManager.PlayOneShot(DamageSoundEvent);
    }

    private IEnumerator DamageColorRoutine()
    {
        PlayerStateMachine M = PlayerStateMachine.Instance;
        Color playerColor = M.PlayerRenderer.color;
        M.PlayerRenderer.color = _damageColor;
        yield return new WaitForSeconds(_damageColorChangeTime);
        M.PlayerRenderer.color = playerColor;
        _damageColorRoutine = null;
    }

    public bool IsSmokeEffectConfigured => smokeAnimator != null && smokeEffectObject != null;
}

