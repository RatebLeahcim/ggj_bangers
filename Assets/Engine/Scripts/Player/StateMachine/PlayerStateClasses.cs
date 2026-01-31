using UnityEngine;

#region Root States

public class PlayerInteractingRoot : RootState<PlayerStates>
{
    public PlayerInteractingRoot(PlayerStates key) : base(key)
    {
        SubStates = new()
        {
            { PlayerStates.None, new PlayerEmptyState(PlayerStates.None) },
        };
    }

    public override PlayerStates InitializeSubState(PlayerStates previousState)
    {
        return PlayerStates.None;
    }

    public override void EnterState(PlayerStates previousState)
    {
        CurrentSubState = SubStates[InitializeSubState(previousState)];
    }

    public override void UpdateState()
    {
        UpdateSubState();
    }

    public override void ExitState(PlayerStates nextState)
    {
    }

    public override void FixedUpdateState(){}

    public override void LateUpdateState(){}

    public override PlayerStates CheckSwitchStates()
    {
        if(PlayerStateMachine.Instance.Conditions.IsInteracting){ return PlayerStates.Interacting; }
        if (PlayerStateMachine.Instance.Conditions.IsHanging)
        {
            return PlayerStates.Hang;
        }
        else if (PlayerStateMachine.Instance.Conditions.IsGrounded)
        {
            return PlayerStates.Grounded;
        }
        else
        {
            return PlayerStates.Ungrounded;
        }
    }
}

public class PlayerGroundedRoot : RootState<PlayerStates>
{
    public PlayerGroundedRoot(PlayerStates key) : base(key)
    {
        SubStates = new()
        {
            { PlayerStates.Idle, new PlayerGroundedIdle(PlayerStates.Idle) },
            { PlayerStates.GroundMove, new PlayerGroundedMove(PlayerStates.GroundMove) },
            { PlayerStates.GroundAttack, new PlayerGroundedAttack(PlayerStates.GroundAttack) }
        };
    }

    public override PlayerStates InitializeSubState(PlayerStates previousState)
    {
        if(!PlayerInputBridge.Instance.Consume(PlayerInputType.Move, out Vector2 value))
        {
            return PlayerStates.Idle;
        }
        else
        {
            return PlayerStates.GroundMove;
        }
    }

    public override void EnterState(PlayerStates previousState)
    {
        CurrentSubState = SubStates[InitializeSubState(previousState)];
    }

    public override void UpdateState()
    {
        UpdateSubState();
    }

    public override void ExitState(PlayerStates nextState)
    {
    }

    public override void FixedUpdateState(){}

    public override void LateUpdateState(){}

    public override PlayerStates CheckSwitchStates()
    {
        if (PlayerStateMachine.Instance.Conditions.IsInteracting)
        {
            return PlayerStates.Interacting;
        }
        else if (PlayerStateMachine.Instance.Conditions.IsHanging)
        {
            return PlayerStates.Hang;
        }
        else if (PlayerStateMachine.Instance.Conditions.IsGrounded)
        {
            return PlayerStates.Ungrounded;
        }
        return PlayerStates.Grounded;
    }
}

public class PlayerUngroundedRoot : RootState<PlayerStates>
{
    public PlayerUngroundedRoot(PlayerStates key) : base(key)
    {
        SubStates = new()
        {
            { PlayerStates.Fall, new PlayerFall(PlayerStates.Fall) },
            { PlayerStates.Jump, new PlayerJump(PlayerStates.Jump) },
        };
    }

    public override PlayerStates InitializeSubState(PlayerStates previousState)
    {
        return PlayerStates.Fall;
    }

    public override void EnterState(PlayerStates previousState)
    {
        CurrentSubState = SubStates[InitializeSubState(previousState)];
    }

    public override void UpdateState()
    {
        UpdateSubState();
    }

    public override void ExitState(PlayerStates nextState)
    {
    }

    public override void FixedUpdateState(){}

    public override void LateUpdateState(){}

    public override PlayerStates CheckSwitchStates()
    {
        if (PlayerStateMachine.Instance.Conditions.IsInteracting)
        {
            return PlayerStates.Interacting;
        }
        else if (PlayerStateMachine.Instance.Conditions.IsHanging)
        {
            return PlayerStates.Hang;
        }
        else if (PlayerStateMachine.Instance.Conditions.IsGrounded)
        {
            return PlayerStates.Grounded;
        }
        return PlayerStates.Ungrounded;
    }
}

public class PlayerHangRoot : RootState<PlayerStates>
{
    public PlayerHangRoot(PlayerStates key) : base(key)
    {
        SubStates = new()
        {
            { PlayerStates.Swing, new PlayerSwing(PlayerStates.Swing) },
            { PlayerStates.HangIdle, new PlayerHangIdle(PlayerStates.HangIdle) },
        };
    }

    public override PlayerStates InitializeSubState(PlayerStates previousState)
    {
        return PlayerStates.HangIdle;
    }

    public override void EnterState(PlayerStates previousState)
    {
        CurrentSubState = SubStates[InitializeSubState(previousState)];
    }

    public override void UpdateState()
    {
        UpdateSubState();
    }

    public override void ExitState(PlayerStates nextState)
    {
    }

    public override void FixedUpdateState(){}

    public override void LateUpdateState(){}

    public override PlayerStates CheckSwitchStates()
    {
        if (PlayerStateMachine.Instance.Conditions.IsInteracting)
        {
            return PlayerStates.Interacting;
        }
        else if (!PlayerStateMachine.Instance.Conditions.IsHanging)
        {
            if (PlayerStateMachine.Instance.Conditions.IsGrounded)
            {
                return PlayerStates.Grounded;
            }
            return PlayerStates.Ungrounded;
        }
        return PlayerStates.Hang;
    }
}

#endregion

#region Empty Sub State

    public class PlayerEmptyState : BaseState<PlayerStates>
{
    public PlayerEmptyState(PlayerStates key) : base(key){}

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override PlayerStates CheckSwitchStates()
    {
        return PlayerStates.None;
    }
    public override void FixedUpdateState(){}

    public override void EnterState(PlayerStates previousState){}

    public override void ExitState(PlayerStates nextState){}

    public override void LateUpdateState(){}
}

#endregion



#region Grounded Sub States

public class PlayerGroundedIdle : BaseState<PlayerStates>
{
    public PlayerGroundedIdle(PlayerStates key) : base(key){}

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override PlayerStates CheckSwitchStates()
    {
        if(PlayerInputBridge.Instance.Consume(PlayerInputType.Enter, out bool attackValue))
        {
            PlayerStateMachine.Instance.Conditions.IsAttacking = true;
            return PlayerStates.GroundAttack;
        }
        else if(PlayerInputBridge.Instance.Consume(PlayerInputType.Move, out Vector2 moveValue))
        {
            PlayerStateMachine.Instance.Conditions.Move = moveValue;
            return PlayerStates.GroundMove;
        }
        else
        {
            PlayerStateMachine.Instance.Conditions.Move = moveValue;
            return PlayerStates.Idle;
        }
    }
    public override void FixedUpdateState(){}

    public override void EnterState(PlayerStates previousState){}

    public override void ExitState(PlayerStates nextState){}

    public override void LateUpdateState(){}
}

public class PlayerGroundedMove : BaseState<PlayerStates>
{
    public PlayerGroundedMove(PlayerStates key) : base(key){}

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override PlayerStates CheckSwitchStates()
    {
        if(PlayerInputBridge.Instance.Consume(PlayerInputType.Enter, out bool attackValue))
        {
            return PlayerStates.GroundAttack;
        }
        else if(PlayerInputBridge.Instance.Consume(PlayerInputType.Move, out Vector2 moveValue))
        {
            PlayerStateMachine.Instance.Conditions.Move = moveValue;
            return PlayerStates.GroundMove;
        }
        PlayerStateMachine.Instance.Conditions.Move = Vector2.zero;
        return PlayerStates.Idle;
    }
    public override void FixedUpdateState(){}

    public override void EnterState(PlayerStates previousState){}

    public override void ExitState(PlayerStates nextState){}

    public override void LateUpdateState(){}
}

public class PlayerGroundedAttack : BaseState<PlayerStates>
{
    public PlayerGroundedAttack(PlayerStates key) : base(key){}

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override PlayerStates CheckSwitchStates()
    {
        if (!PlayerStateMachine.Instance.Conditions.IsAttacking)
        {
            if(PlayerInputBridge.Instance.Consume(PlayerInputType.Move, out Vector2 moveValue))
            {
                PlayerStateMachine.Instance.Conditions.Move = moveValue;
                return PlayerStates.GroundMove;
            }
            return PlayerStates.Idle;
        }
        return PlayerStates.GroundAttack;
    }
    public override void FixedUpdateState(){}

    public override void EnterState(PlayerStates previousState){}

    public override void ExitState(PlayerStates nextState){}

    public override void LateUpdateState(){}
}

#endregion

#region UNGROUNDED STATES

public class PlayerFall : BaseState<PlayerStates>
{
    public PlayerFall(PlayerStates key) : base(key){}

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override PlayerStates CheckSwitchStates()
    {
        return PlayerStates.Fall;
    }
    public override void FixedUpdateState(){}

    public override void EnterState(PlayerStates previousState){}

    public override void ExitState(PlayerStates nextState){}

    public override void LateUpdateState(){}
}

public class PlayerJump : BaseState<PlayerStates>
{
    public PlayerJump(PlayerStates key) : base(key){}

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override PlayerStates CheckSwitchStates()
    {
        if (!PlayerStateMachine.Instance.Conditions.IsJumping)
        {
            return PlayerStates.Fall;
        }
        return PlayerStates.Jump;
    }
    public override void FixedUpdateState(){}

    public override void EnterState(PlayerStates previousState){}

    public override void ExitState(PlayerStates nextState){}

    public override void LateUpdateState(){}
}

#endregion

#region HANGING STATES

public class PlayerHangIdle : BaseState<PlayerStates>
{
    public PlayerHangIdle(PlayerStates key) : base(key){}

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override PlayerStates CheckSwitchStates()
    {
        if(PlayerInputBridge.Instance.Consume(PlayerInputType.Move, out Vector2 value))
        {
            PlayerStateMachine.Instance.Conditions.Move = value;
            return PlayerStates.Swing;
        }
        else
        {
            PlayerStateMachine.Instance.Conditions.Move = value;
            return PlayerStates.HangIdle;
        }
    }
    public override void FixedUpdateState(){}

    public override void EnterState(PlayerStates previousState){}

    public override void ExitState(PlayerStates nextState){}

    public override void LateUpdateState(){}
}

public class PlayerSwing : BaseState<PlayerStates>
{
    public PlayerSwing(PlayerStates key) : base(key){}

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override PlayerStates CheckSwitchStates()
    {
        if(PlayerInputBridge.Instance.Consume(PlayerInputType.Move, out Vector2 value))
        {
            PlayerStateMachine.Instance.Conditions.Move = value;
            return PlayerStates.Swing;
        }
        else
        {
            PlayerStateMachine.Instance.Conditions.Move = value;
            return PlayerStates.HangIdle;
        }
    }
    public override void FixedUpdateState(){}

    public override void EnterState(PlayerStates previousState){}

    public override void ExitState(PlayerStates nextState){}

    public override void LateUpdateState(){}
}

#endregion