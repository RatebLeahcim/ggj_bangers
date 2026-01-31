using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerMachine<EState> : MonoBehaviour where EState : Enum
    {

#region Variables

        protected Dictionary<EState, RootState<EState>> RootStates = new();
        protected RootState<EState> CurrentState;
        protected bool InTransition = false;

        #endregion

        #region Monobehaviour Functions

        // private void Awake()
        // {
        //     foreach(var root in RootStates)
        //     {
                
        //     }   
        // }

        private void Start()
        {
            CurrentState.EnterState(CurrentState.StateKey);
        }

        private void Update()
        {
            if(InTransition){ return; }
            EState nextState = CurrentState.CheckSwitchStates();
            if(nextState.Equals(CurrentState.StateKey)){ CurrentState.UpdateState(); }
            else{ SwitchState(nextState); }
        }

#endregion

#region Helper Functions

        private void SwitchState(EState stateKey)
        {
            InTransition = true;
            CurrentState.ExitState(stateKey);
            EState previousState = CurrentState.StateKey;
            CurrentState = RootStates[stateKey];
            CurrentState.EnterState(previousState);
            InTransition = false;
        } 

#endregion

    } 

    
