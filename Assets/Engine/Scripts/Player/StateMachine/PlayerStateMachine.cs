using System;
using Unity.Mathematics;
using UnityEngine;

public class PlayerStateMachine : PlayerMachine<PlayerStates>
{
    private static PlayerStateMachine _instance;
    public static PlayerStateMachine Instance { get { return _instance; } set { _instance = value; } }

    public PlayerConditions Conditions;
    public Rigidbody2D Rigidbod;
    public PlayerHealth Health;
    public SpriteRenderer PlayerRenderer;
    private CameraShake _cameraShake;

    private void Awake()
    {
        if(_instance != null){ Destroy(this); } else { _instance = this; }
        Conditions = new();
        RootStates = new()
        {
            { PlayerStates.Grounded, new PlayerGroundedRoot(PlayerStates.Grounded) },
            { PlayerStates.Ungrounded, new PlayerUngroundedRoot(PlayerStates.Ungrounded) },
            { PlayerStates.Hang, new PlayerHangRoot(PlayerStates.Hang) },
            { PlayerStates.Interacting, new PlayerInteractingRoot(PlayerStates.Interacting) },
        };
    }

    private void Start()
    {
        CurrentState = RootStates[PlayerStates.Grounded];
        CurrentState.EnterState(CurrentState.StateKey);
        _cameraShake = Camera.main.GetComponent<CameraShake>();
    }

    private void Update()
    {
        if(Health.HealthCooloffTimer > 0)
        {
            Health.HealthCooloffTimer -= Time.deltaTime;
        }
    }

    public void ShakeCamera()
    {
        _cameraShake.Shake(.3f,0.05f);
    }
}

[Serializable]
public class PlayerHealth
{
    public PlayerHealth()
    {
        Health = MaxHealth;
    }
    public bool IsDead;
    public float Health, MaxHealth = 100f;
    public float HealthCooloffTimer = 0;
    private float _healthCooloffReset = 0.5f;
    public void TakeDamage(float damage)
    {
        if(HealthCooloffTimer > 0){ return; }
        PlayerEffects.Instance.DamageVFX();
        PlayerStateMachine.Instance.ShakeCamera();
        HealthCooloffTimer = _healthCooloffReset;
        Health -= damage;
        if(Health <= 0){ IsDead = true; }
    }

    public void AddHealth(float increasePercentage)
    {
        Health = math.lerp(Health,MaxHealth,increasePercentage);
    }
}
