using UnityEngine;

public class PlayerStateMachine : PlayerMachine<PlayerStates>
{
    private static PlayerStateMachine _instance;
    public static PlayerStateMachine Instance { get { return _instance; } set { _instance = value; } }

    public PlayerConditions Conditions;

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
    }
}
