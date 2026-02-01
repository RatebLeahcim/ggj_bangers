using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputBridge : MonoBehaviour
{
    private static PlayerInputBridge _instance;
    public static PlayerInputBridge Instance { get { return _instance; } set { _instance = value; } }

    private PlayerActions _inputAsset;
    private PlayerActions.RuntimeActions _actionMap; 
    private List<IPlayerInputControl<bool>> _cutoffButtons;
    private List<IPlayerInputControl<Vector2>> _cutoffVectors;
    private Dictionary<PlayerInputType,IPlayerInputControl<bool>> _buttonControls;
    private Dictionary<PlayerInputType,IPlayerInputControl<Vector2>> _vectorControls;

    public void Awake()
    {
        if(_instance != null){ Destroy(this); } else { _instance = this; }
        _cutoffButtons = new();
        _cutoffVectors = new();
        _buttonControls = new();
        _vectorControls = new();
        EnableInput();
        AssignInputs();
    }

    private void EnableInput()
    {
        _inputAsset = new();
        _actionMap = _inputAsset.Runtime;
        _actionMap.Enable();
    }

    // private void OnDisable()
    // {
    //     _actionMap.Disable();
    // }

    private void Update()
    {
        AssignCutoffTimers();
    }

    private void LateUpdate()
    {
        ProcessCutoffTimers();
    }

    public bool Consume(PlayerInputType input, out bool value)
    {
        value = _buttonControls[input].Consume();
        return value;
    }

    public bool Consume(PlayerInputType input,float cutoffTimer, out bool value)
    {
        value = _buttonControls[input].Consume(cutoffTimer);
        return value;
    }

    public bool Consume(PlayerInputType input,out Vector2 value)
    {
        value = _vectorControls[input].Consume();
        return value.magnitude > 0;
    }

    public bool Consume(PlayerInputType input,float cutoffTimer, out Vector2 value)
    {
        value = _vectorControls[input].Consume(cutoffTimer);
        return value.magnitude > 0;
    }

    private void AssignCutoffTimers()
    {
        foreach(var control in _buttonControls)
        {
            if(control.Value.CheckCutoff() && !_cutoffButtons.Contains(control.Value))
            {
                _cutoffButtons.Add(control.Value);
            }
        }

        foreach(var control in _vectorControls)
        {
            if(control.Value.CheckCutoff() && !_cutoffVectors.Contains(control.Value))
            {
                _cutoffVectors.Add(control.Value);
            }
        }
    }

    private void ProcessCutoffTimers()
    {
        List<IPlayerInputControl<bool>> tempButtons = new();
        List<IPlayerInputControl<Vector2>> tempVectors = new();

        foreach(var timer in _cutoffButtons)
        {
            if(timer.CutoffTimer())
            {
                tempButtons.Add(timer);
            }
        }

        foreach(var timer in _cutoffVectors)
        {
            if(timer.CutoffTimer())
            {
                tempVectors.Add(timer);
            }
        }

        foreach(var finishedButton in tempButtons)
        {
            _cutoffButtons.Remove(finishedButton);
        }

        foreach(var finishedVector in tempVectors)
        {
            _cutoffVectors.Remove(finishedVector);
        }
    }

    private void AssignInputs()
    {
        AddVector(PlayerInputType.Move,_actionMap.Move);
        AddVector(PlayerInputType.MousePosition,_actionMap.MousePosition);

        AddButton(PlayerInputType.Space,_actionMap.Space);
        AddButton(PlayerInputType.Enter,_actionMap.Enter);
        AddButton(PlayerInputType.LeftClick,_actionMap.LeftClick);
        AddButton(PlayerInputType.RightClick,_actionMap.RightClick);
    }

    private void AddButton(PlayerInputType inputType, InputAction action)
    {
        _buttonControls.Add(inputType,new ButtonInput());
        action.started += _buttonControls[inputType].Set;
        action.canceled += _buttonControls[inputType].Set;
        action.performed += _buttonControls[inputType].Set;
    }

    private void AddVector(PlayerInputType inputType, InputAction action)
    {
        _vectorControls.Add(inputType,new VectorInput());
        action.started += _vectorControls[inputType].Set;
        action.canceled += _vectorControls[inputType].Set;
        action.performed += _vectorControls[inputType].Set;
    }
}

[Serializable]
public class ButtonInput : IPlayerInputControl<bool>
{
    public bool Value;
    public bool Hold;
    public bool RequireNewInput;
    public float CutoffTimer = 0;

    public bool Consume()
    {
        if(CutoffTimer > 0){ RequireNewInput = true; }
        if (RequireNewInput)
        {
            return false;
        }
        RequireNewInput = true;
        return Value;
    }

    public bool Consume(float cutoffTimer)
    {
        if(CutoffTimer > 0){ RequireNewInput = true; }
        if (RequireNewInput)
        {
            return false;
        }
        if(Value){ 
            RequireNewInput = true;
            CutoffTimer = cutoffTimer;
        }

        return Value;
    }

    public void Set(InputAction.CallbackContext context)
    {
        bool tempValue = context.ReadValueAsButton();
        if(Value && tempValue){ Hold = true; }
        else{ Hold = false; }
        Value = tempValue;
        RequireNewInput = false;
    }

    bool IPlayerInputControl<bool>.CutoffTimer()
    {
        if(CutoffTimer > 0)
        {
            CutoffTimer -= Time.deltaTime;
            return true;
        }
        return false;
    }

    bool IPlayerInputControl<bool>.CheckCutoff()
    {
        return CutoffTimer > 0;
    }
}

[Serializable]
public class VectorInput : IPlayerInputControl<Vector2>
{
    public Vector2 Value;
    public bool IsPressed;
    public float CutoffTimer = 0;

    public void Set(InputAction.CallbackContext context)
    {
        if(CutoffTimer > 0){ Value = Vector2.zero; IsPressed = false; return;}
        Value = context.ReadValue<Vector2>();
        IsPressed = Value.magnitude > 0;
    }

    public Vector2 Consume()
    {
        if(CutoffTimer > 0){ return Vector2.zero; }
        return Value;
    }

    public Vector2 Consume(float cutoffTimer)
    {
        if(CutoffTimer > 0)
        { 
            return Vector2.zero; 
        }
        else
        {
            CutoffTimer = cutoffTimer;
        }
        return Value;
    }

    bool IPlayerInputControl<Vector2>.CutoffTimer()
    {
        if(CutoffTimer > 0)
        {
            CutoffTimer -= Time.deltaTime;
            return true;
        }
        return false;
    }

    bool IPlayerInputControl<Vector2>.CheckCutoff()
    {
        return CutoffTimer > 0;
    }
}

public interface IPlayerInputControl<T>
{
    public void Set(InputAction.CallbackContext context);
    public T Consume();
    public T Consume(float cutoffTimer);
    public bool CutoffTimer();
    public bool CheckCutoff();
}

[Serializable]
public enum PlayerInputType
{
    Move, MousePosition, LeftClick, RightClick, Space, Enter
}

