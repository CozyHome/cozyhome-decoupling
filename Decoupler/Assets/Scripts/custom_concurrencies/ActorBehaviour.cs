using System;
using UnityEngine;
using com.cozyhome.Actors;
using com.cozyhome.ConcurrentExecution;
using com.cozyhome.Systems;
using com.cozyhome.Vectors;

// leave values in here to be mostly primitive with exceptions to essentials like the Animator
// etc..
[System.Serializable]
public class ActorArgs
{
    [Header("Reference Vars")]
    public ActorHeader.Actor Actor;
    public UnityEngine.Transform ActorTransform;
    public UnityEngine.Transform ActorView;

    [Header("Input Vars")]
    public Vector2 RawMouseDelta;
    public Vector2 RawWishDir;
    public int ActionFlags;


    [Header("Projection Vars")]
    public Vector3 ViewWishDir;
}

[System.Serializable]
public class ActorInput : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution
{
    [Header("Movement Keycodes")]
    [SerializeField] private KeyCode Forward = KeyCode.W;
    [SerializeField] private KeyCode Backward = KeyCode.S;
    [SerializeField] private KeyCode Leftward = KeyCode.A;
    [SerializeField] private KeyCode Rightward = KeyCode.D;
    [Header("Action")]
    [SerializeField] private KeyCode PrimaryFire = KeyCode.Mouse0;
    [SerializeField] private KeyCode SecondaryFire = KeyCode.Mouse1;
    [SerializeField] private KeyCode Jump = KeyCode.Space;
    [SerializeField] private KeyCode Dash = KeyCode.LeftShift;

    protected override void OnExecutionDiscovery()
    {
        RegisterExecution();
        BeginExecution();
    }

    public override void Simulate(ActorArgs _args)
    {
        _args.RawWishDir = new Vector2(
            Evaluate(Input.GetKey(Rightward)) - Evaluate(Input.GetKey(Leftward)),
            Evaluate(Input.GetKey(Forward)) - Evaluate(Input.GetKey(Backward))
        );

        _args.RawMouseDelta = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        );

        int _actflags = 0;

        _actflags |= Input.GetKey(PrimaryFire) ? (1 << 0) : 0;
        _actflags |= Input.GetKey(SecondaryFire) ? (1 << 1) : 0;
        _actflags |= Input.GetKey(Jump) ? (1 << 2) : 0;
        _actflags |= Input.GetKey(Dash) ? (1 << 3) : 0;

        _args.ActionFlags = _actflags;
    }

    private float Evaluate(bool B) { if (B) return 1; else return 0; }
}

[System.Serializable]
public class ActorView : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution
{
    [Header("Viewing Properties")]
    [SerializeField] private float ViewSensitivity = 240F;
    [SerializeField] private float MaxVerticalViewingAngle = 89.9F;
    protected override void OnExecutionDiscovery()
    {
        RegisterExecution();
        BeginExecution();
    }
    public override void Simulate(ActorArgs _args)
    {
        // cache members at top of function execution
        Transform _v = _args.ActorView;
        Vector2 _m = _args.RawWishDir;
        Vector2 _d = _args.RawMouseDelta;

        // apply sensitivity and deltatime to our rotational deltas
        for (int i = 0; i < 2; i++)
            _d[i] = _d[i] * ViewSensitivity * GlobalTime.FDT;

        // rotate our orientation based on our mouse delta and clamping angle
        _v.rotation = LookRotate(_v.rotation, _d, MaxVerticalViewingAngle);

        // clamp raw wish direction to a magnitude of 1.0 to prevent "square running"
        _m = Vector2.ClampMagnitude(_m, 1.0F);
        _args.ViewWishDir = (_v.rotation) * new Vector3(
            _m[0],
            0,
            _m[1]
        );

        return;
    }

    private Quaternion LookRotate(
            Quaternion _previous,
            Vector2 _lookdelta,
            float _maxvertical)
    {
        Quaternion R = _previous;

        // measure the angular difference and adjust to clamped angle if need be:
        float _px = 90F - Vector3.Angle(
            _previous * new Vector3(0, 0, 1),
            new Vector3(0, 1, 0)
        );

        Vector3 fwd = R * new Vector3(0, 0, 1);

        float _nextx = -_lookdelta[1];

        // if (cur angle + delta angle) > clamp angle
        // subtract difference from delta and apply
        if (_px - _nextx > _maxvertical)
            _nextx = -(_maxvertical - _px);

        // do the same for the opposite axis
        else if (_px - _nextx < -_maxvertical)
            _nextx = -(-_maxvertical - _px);

        fwd = Quaternion.AngleAxis(
                _nextx,
                R * new Vector3(1, 0, 0)
            ) * fwd;

        fwd = Quaternion.AngleAxis(
            _lookdelta[0],
            new Vector3(0, 1, 0)
        ) * fwd;

        R = Quaternion.LookRotation(fwd);

        return R;
    }
}

[System.Serializable]
public class ActorWish : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution

//      Actor Wish is responsible for determing what direction the actor 
//      should be moving in PURELY based on their inputs.
//      Dashing 
//      Jumping 
//      Running 
{
    [Header("Wish Movement Parameters")]
    [SerializeField] private bool OrientVelocityToGroundPlane = true;
    [SerializeField] private float ConstantAcceleration = 10.0F;
    [SerializeField] private float MaximumMoveSpeed = 10.0F;

    protected override void OnExecutionDiscovery()
    {
        RegisterExecution();
        BeginExecution();
    }

    public override void Simulate(ActorArgs _args)
    {
        ActorHeader.Actor Actor = _args.Actor;
        ActorHeader.GroundHit Ground = Actor.Ground;
        Vector3 Velocity = Actor._velocity;
        Vector3 Wish = _args.ViewWishDir;

        // Orient Wish Velocity to grounding plane
        if (Ground.snapped && OrientVelocityToGroundPlane)
            VectorHeader.CrossProjection(ref Wish, new Vector3(0, 1, 0), Ground.normal);
        else // Clip Wish Velocity along upward plane if we're not orienting/stable as we may be able to fight gravity if not done
            VectorHeader.ClipVector(ref Wish, new Vector3(0, 1, 0));

        float _vm = Velocity.magnitude;

        DetermineWishVelocity(ref Velocity, Wish, MaximumMoveSpeed, ConstantAcceleration * GlobalTime.FDT);

        // clamp max speed

        Actor.SetVelocity(Velocity);

        return;
    }

    private void DetermineWishVelocity(ref Vector3 _velocity, Vector3 _wish, float _maxspeed, float _accelspeed)
    {
        _velocity += (_wish * _accelspeed);

        float _vm = _velocity.magnitude;
        if (_vm <= 0F)
            return;

        float _newspeed = VectorHeader.Dot(_velocity, _wish);

        if (_newspeed > _maxspeed)
        {   // Trim (circle strafe)
            _velocity -= (_velocity / _vm) * (_newspeed - _maxspeed);
        }
    }
}

[System.Serializable]
public class ActorFriction : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution
{
    [Header("Friction Parameters")]
    [SerializeField] private float GroundFriction = 30.0F;
    [SerializeField] private float AirFriction = 10.0F;
    [SerializeField] private float LastGroundSnapshot = 0.0F;
    [SerializeField] private float GroundFrictionLeeway = 0.1F;

    protected override void OnExecutionDiscovery()
    {
        RegisterExecution();
        BeginExecution();
    }
    public override void Simulate(ActorArgs _args)
    {
        ActorHeader.Actor Actor = _args.Actor;
        ActorHeader.GroundHit Ground = Actor.Ground; // Ground is currently last frame as we have not moved until ActorMove() of this simulation
        ActorHeader.GroundHit LastGround = Actor.LastGround; // LastGround is the frame prior to the last
        Vector3 _v = Actor._velocity;

        LastGroundSnapshot = (Ground.snapped && !LastGround.snapped) ? GlobalTime.T : LastGroundSnapshot;

        bool _applyfriction = _v.sqrMagnitude > 0F;

        if (!_applyfriction)
            return;

        float _vm = _v.magnitude;

        // Determine whether to apply ground friction
        bool _validgroundfriction = (Ground.snapped) && (GlobalTime.T - LastGroundSnapshot) > GroundFrictionLeeway;

        if (_validgroundfriction)
            ApplyFriction(ref _v, _vm, GroundFriction, GlobalTime.FDT);
        else
            ApplyFriction(ref _v, _vm, AirFriction, GlobalTime.FDT);

        Actor.SetVelocity(_v);
    }

    private void ApplyFriction(ref Vector3 _v, float _speed, float _friction, float FDT)
    {
        float _newspeed = _speed - (_friction * FDT);
        if (_newspeed <= 0)
            _v = Vector3.zero;
        else
            _v *= _newspeed / _speed;

        return;
    }
}
[System.Serializable]
public class ActorGravity : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution
{
    [System.NonSerialized] private static float GRAVITY = 9.81F;
    [Header("Gravity Parameters")]
    [SerializeField] private float GravitationalMultiplier = 4F;

    protected override void OnExecutionDiscovery()
    {
        BeginExecution();
        RegisterExecution();
    }

    public override void Simulate(ActorArgs _args)
    {
        ActorHeader.Actor Actor = _args.Actor;
        ActorHeader.GroundHit Ground = Actor.Ground;
        Vector3 Velocity = Actor._velocity;

        bool _validgravity = Actor.Ground.stable;

        Velocity -= new Vector3(0, 1, 0) * (GRAVITY * GravitationalMultiplier * GlobalTime.FDT);

        Actor.SetVelocity(Velocity);

        return;
    }
}

[System.Serializable]
public class ActorMove : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution,
            ActorHeader.IActorReceiver
//      Actor Move is responsible for the actual displacement of the Actor, and should usually be the last function of execution in the 
//      timeline.
{
    protected override void OnExecutionDiscovery()
    {
        RegisterExecution();
        BeginExecution();
    }

    public override void Simulate(ActorArgs _args)
    {
        ActorHeader.Actor Actor = _args.Actor;
        UnityEngine.Transform Transform = _args.ActorTransform;

        Actor.SetPosition(Transform.position);
        Actor.SetOrientation(Transform.rotation);

        ActorHeader.Move(this, Actor, GlobalTime.FDT);

        Transform.SetPositionAndRotation(Actor._position, Actor._orientation);

        return;
    }

    public void OnGroundHit(in ActorHeader.GroundHit _ground, in ActorHeader.GroundHit _lastground, LayerMask _gfilter)
    { }

    public void OnTraceHit(in RaycastHit _trace, in Vector3 _position, in Vector3 _velocity)
    { }
}

public class ActorBehaviour : ConcurrentHeader.ExecutionMachine<ActorArgs>.MonoExecution
{
    // List your Executions here
    [SerializeField] private ActorInput InputExecution;
    [SerializeField] private ActorView ViewExecution;
    [SerializeField] private ActorWish WishExecution;
    [SerializeField] private ActorFriction FrictionExecution;
    [SerializeField] private ActorGravity GravityExecution;
    [SerializeField] private ActorMove MoveExecution;

    public override void OnBehaviourDiscovered(
        Action<ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution>[] ExecutionCommands)
    {
        // Initialize all executions you want here
        InputExecution.OnBaseDiscovery(ExecutionCommands);
        ViewExecution.OnBaseDiscovery(ExecutionCommands);
        WishExecution.OnBaseDiscovery(ExecutionCommands);
        FrictionExecution.OnBaseDiscovery(ExecutionCommands);
        GravityExecution.OnBaseDiscovery(ExecutionCommands);
        MoveExecution.OnBaseDiscovery(ExecutionCommands);
    }
}
