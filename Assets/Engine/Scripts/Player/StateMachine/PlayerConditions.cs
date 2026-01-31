using System;
using UnityEngine;

[Serializable]
public class PlayerConditions 
{
    public bool IsInteracting;
    public bool IsGrounded;
    public bool IsMoving;
    public bool IsAttacking;
    public bool IsJumping;
    public bool IsHanging;

    public Vector2 Move;
    public Vector2 MousePosition;
}
