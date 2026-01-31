using System;
using System.Collections.Generic;

public abstract class BaseState<EState> where EState : Enum
    {

#region Variables

        public BaseState(EState key){ StateKey = key; }
        public EState StateKey { get; private set; }

#endregion

#region Functions

        public abstract void EnterState(EState previousState);
        public abstract void ExitState(EState nextState);
        public abstract void UpdateState();
        public abstract void FixedUpdateState();
        public abstract void LateUpdateState();
        public abstract EState CheckSwitchStates();

#endregion

    }

    public abstract class RootState<EState> where EState : Enum
    {

#region Variables

        public RootState(EState key){ StateKey = key; }
        public EState StateKey { get; private set; }
        protected bool InTransition { get; private set; }
        protected Dictionary<EState, BaseState<EState>> SubStates = new();
        public BaseState<EState> CurrentSubState;

#endregion

#region Functions

        public abstract void EnterState(EState previousState);
        public abstract void ExitState(EState nextState);
        public abstract PlayerStates InitializeSubState(EState previousState);
        public void UpdateSubState()
        { 
            if(InTransition){ return; }
            EState nextSubState = CurrentSubState.CheckSwitchStates();
            if(nextSubState.Equals(CurrentSubState.StateKey)){ CurrentSubState.UpdateState(); }
            else{ SwitchSubState(nextSubState); }
        }
        public abstract void UpdateState();
        public abstract void FixedUpdateState();
        public abstract void LateUpdateState();
        public abstract EState CheckSwitchStates();

        public void SwitchSubState(EState stateKey)
        {
            InTransition = true;
            CurrentSubState.ExitState(stateKey);
            EState previousState = CurrentSubState.StateKey;
            CurrentSubState = SubStates[stateKey];
            CurrentSubState.EnterState(previousState);
            InTransition = false;
        } 

#endregion

    }

    public enum PlayerStates
    {
        // Default
        None,

        // Root States
        Grounded, Ungrounded, Hang, Interacting,

        // Sub States Grounded
        Idle, GroundMove, GroundAttack,

        // Sub States Air
        Fall, Jump, 

        // Sub States Hang
        HangIdle, Swing

    }