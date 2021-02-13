using System;
using System.Collections.Generic;
using UnityEngine;
using com.cozyhome.Actors;
using com.cozyhome.Console;
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
    private ActorHolds ActorHolds;
    private ActorDash ActorDash;
    public UnityEngine.Transform ActorView;

    [Header("Input Vars")]
    public Vector2 RawMouseDelta;
    public Vector2 RawWishDir;
    public int ActionFlags;

    [Header("Projection Vars")]
    public Vector3 ViewWishDir;

    [Header("Misc. Vars")]
    public float Gravity = 9.81F;
    public float GravitationalMultiplier = 4F;

    // Executional Communications Center

    public void AssignHoldsObject(ActorHolds ActorHolds)
        => this.ActorHolds = ActorHolds;
    public void AssignHold(Func<ActorArgs, bool> _execution)
        => ActorHolds.AssignHolds(_execution);
    public void AssignDashObject(ActorDash ActorDash)
        => this.ActorDash = ActorDash;
    public void AssignDashCallback(Action<ActorArgs, ActorDash.DashState> OnDashEvent)
        => this.ActorDash.AssignDashCallback(OnDashEvent);
    public void RemoveDashCallback(Action<ActorArgs, ActorDash.DashState> OnDashEvent)
        => this.ActorDash.RemoveDashCallback(OnDashEvent);
}
[System.Serializable]
public class ActorHolds : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution
//      This execution generates a shit load of garbage every time you create a new method (104 Bytes-ish) so I suggest
//      avoiding it for the time being
{
    private List<Func<ActorArgs, bool>> _heldexecutions;

    protected override void OnExecutionDiscovery(ActorArgs _args)
    {
        _heldexecutions = new List<Func<ActorArgs, bool>>();
        RegisterExecution();
        BeginExecution();
    }

    public override void Simulate(ActorArgs _args)
    {
        for (int i = 0; i < _heldexecutions.Count; i++)
            if (_heldexecutions[i](_args))
            {
                _heldexecutions[i] = null;
                _heldexecutions.RemoveAt(i);
            }
    }

    public void AssignHolds(Func<ActorArgs, bool> _execution)
    => _heldexecutions.Add(_execution);
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

    protected override void OnExecutionDiscovery(ActorArgs _args)
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
    protected override void OnExecutionDiscovery(ActorArgs _args)
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

//      Actor Wish is responsible for determining what direction the actor 
//      should be moving in PURELY based on their inputs.
//      Dashing 
//      Jumping 
//      Running 
{
    [Header("Wish Movement Parameters")]
    [SerializeField] private bool OrientVelocityToGroundPlane = true;
    [SerializeField] private float GroundAcceleration = 10.0F;
    [SerializeField] private float AirAcceleration = 20.0F;
    [SerializeField] private float MaximumGroundMoveSpeed = 10.0F;
    [SerializeField] private float MaximumAirMoveSpeed = 30.0F;

    protected override void OnExecutionDiscovery(ActorArgs _args)
    {
        RegisterExecution();
        BeginExecution();

        _args.AssignDashCallback((_args, state) =>
       {
           if (state == ActorDash.DashState.Enter)
               EndExecution();
           else
           {
               if (_args.Actor.Ground.stable)
                   BeginExecution();
               else
                   _args.AssignHold((_args) =>
                   {
                       if (_args.Actor.Ground.stable)
                       {
                           BeginExecution();
                           return true;
                       }

                       return false;
                   });
           }
       });
    }

    public override void Simulate(ActorArgs _args)
    {
        ActorHeader.Actor Actor = _args.Actor;
        ActorHeader.GroundHit Ground = Actor.Ground;
        ActorHeader.GroundHit LastGround = Actor.LastGround;
        Vector3 Velocity = Actor._velocity;
        Vector3 Wish = _args.ViewWishDir;

        bool Grounded = Actor.SnapEnabled && Ground.stable;

        if (Grounded && !LastGround.stable) // Landing
            VectorHeader.ClipVector(ref Velocity, Ground.normal);

        // Orient Wish Velocity to grounding plane
        if (Grounded && OrientVelocityToGroundPlane)
            VectorHeader.CrossProjection(ref Wish, new Vector3(0, 1, 0), Ground.normal);
        else
        {
            // Clip Wish Velocity along upward plane if we're not orienting/stable as we may be able to fight gravity if not done
            VectorHeader.ClipVector(ref Wish, new Vector3(0, 1, 0));
            Wish.Normalize();
        }

        if (Grounded) // Subtract max speed based on stability 
            DetermineWishVelocity(ref Velocity, Wish, MaximumGroundMoveSpeed, GroundAcceleration * GlobalTime.FDT);
        else
            DetermineWishVelocity(ref Velocity, Wish, MaximumAirMoveSpeed, AirAcceleration * GlobalTime.FDT);

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
            _velocity -= (_wish) * (_newspeed - _maxspeed);
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

    protected override void OnExecutionDiscovery(ActorArgs _args)
    {
        RegisterExecution();
        BeginExecution();

        _args.AssignDashCallback((_args, _state) =>
        {
            if (_state == ActorDash.DashState.Enter)
                EndExecution();
            else
                BeginExecution();
        });
    }

    public override void Simulate(ActorArgs _args)
    {
        ActorHeader.Actor Actor = _args.Actor;
        ActorHeader.GroundHit Ground = Actor.Ground; // Ground is currently last frame as we have not moved until ActorMove() of this simulation
        ActorHeader.GroundHit LastGround = Actor.LastGround; // LastGround is the frame prior to the last
        Vector3 _v = Actor._velocity;
        bool _applyfriction = _v.sqrMagnitude > 0F;

        if (!_applyfriction)
            return;

        LastGroundSnapshot = (Ground.snapped && !LastGround.snapped)
            ? GlobalTime.T : LastGroundSnapshot;
        float _vm = _v.magnitude;

        // Determine whether to apply ground friction or air friction
        bool _applygroundfriction = (Actor.SnapEnabled && Ground.stable) && (GlobalTime.T - LastGroundSnapshot) > GroundFrictionLeeway;

        if (_applygroundfriction)
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

    private void ApplyAirFriction(ref Vector3 _v, float _speed, float _friction, float FDT)
    {
        //float _newspeed = _speed - (_friction * FDT);
        //if (_newspeed <= 0)
        //    _v = Vector3.zero;
        //else
        //    _v *= _newspeed / _speed;

        _v -= VectorHeader.ClipVector(_v / _speed, new Vector3(0, 1, 0)) * (_friction * FDT);

        return;
    }
}
[System.Serializable]

public class ActorGravity : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution
{
    protected override void OnExecutionDiscovery(ActorArgs _args)
    {
        BeginExecution();
        RegisterExecution();

        _args.AssignDashCallback((_args, _state) =>
       {
           if (_state == ActorDash.DashState.Enter)
               EndExecution();
           else
               BeginExecution();
       });
    }

    public override void Simulate(ActorArgs _args)
    {
        ActorHeader.Actor Actor = _args.Actor;
        ActorHeader.GroundHit Ground = Actor.Ground;
        Vector3 Velocity = Actor._velocity;

        bool _validgravity = Actor.Ground.stable;

        Velocity -= new Vector3(0, 1, 0) * (_args.Gravity * _args.GravitationalMultiplier * GlobalTime.FDT);

        Actor.SetVelocity(Velocity);

        return;
    }
}

[System.Serializable]
public class ActorJump : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution
{
    [Header("Jump Parameters")]
    [SerializeField] private float JumpHeight = 2F;
    [SerializeField] private float TimeJumpSnapshot = 0.0F;

    protected override void OnExecutionDiscovery(ActorArgs _args)
    {
        RegisterExecution();
        BeginExecution();

    }

    public override void Simulate(ActorArgs _args)
    {
        ActorHeader.Actor Actor = _args.Actor;
        ActorHeader.GroundHit Ground = Actor.Ground;
        Vector3 Velocity = Actor._velocity;

        bool JumpRequest = (_args.ActionFlags & (1 << 2)) != 0;

        if (Actor.SnapEnabled && Ground.stable && JumpRequest)
        {
            // Eliminate Y-component of our velocity and instead set it to whatever we'd like:
            Velocity[1] = Mathf.Sqrt(2 * (_args.Gravity * _args.GravitationalMultiplier) * JumpHeight);

            // We're only doing this since gravitational movement / vertical movement is always
            // 100% in the 2nd component of our velocity. We'll never be implementing spherical gravity
            // or anything like that as of this moment. Changing it won't be hard either if we 
            // ever want this functionality.

            Actor.SetSnapEnabled(false); // disabling snapping until we've found the ground again

            Cursor.lockState = CursorLockMode.Locked;
            TimeJumpSnapshot = GlobalTime.T;

            _args.AssignHold(WaitToSnapUntil); // wait a few milliseconds to begin snapping again
        }
        Actor.SetVelocity(Velocity);
    }

    private bool WaitToEnableUntil(ActorArgs _args)
    {
        bool _e = _args.Actor.Ground.stable;
        if (_e)
            BeginExecution();

        return _e;
    }

    private bool WaitToSnapUntil(ActorArgs _args)
    {
        float D = GlobalTime.T - TimeJumpSnapshot;

        if (D > 0.025F)
        {
            _args.Actor.SetSnapEnabled(true);
            return true;
        }

        return false;
    }

}

[System.Serializable]
public class ActorMove : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution,
            ActorHeader.IActorReceiver
//      Actor Move is responsible for the actual displacement of the Actor, and should usually be the last function of execution in the 
//      timeline.
{
    protected override void OnExecutionDiscovery(ActorArgs _args)
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
    {

    }

    public void OnTraceHit(in RaycastHit _trace, in Vector3 _position, in Vector3 _velocity)
    {

    }
}

[System.Serializable]
public class ActorDash : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution
{
    public enum DashState { Enter, Exit }

    [Header("Dash Parameters")]
    [SerializeField] private float DashDuration = 0.1F;
    [SerializeField] private float DashGrowthRate = 25F;
    [SerializeField] private float DashVelocityDelta = 5F;
    [SerializeField] private float DashCost = 25F;
    [SerializeField] private float DashCap = 50F;
    [SerializeField] private float DashStamina = 50F;

    [System.NonSerialized] private float DashRequestSnapshot = 0.0F;

    private Action<ActorArgs, DashState> DashEvents;

    protected override void OnExecutionDiscovery(ActorArgs _args)
    {
        RegisterExecution();
        BeginExecution();

        _args.AssignDashCallback((_args, state) =>
       {
           if (state == ActorDash.DashState.Enter)
               EndExecution();
           else
           {
               BeginExecution();
           }
       });
    }
    public override void Simulate(ActorArgs _args)
    {
        ActorHeader.Actor Actor = _args.Actor;
        ActorHeader.GroundHit Ground = Actor.Ground;

        Vector3 Forward = _args.ActorView.forward;
        Vector3 Wish = _args.ViewWishDir;

        bool Grounded = Actor.SnapEnabled && Ground.stable;
        bool DashRequest = (GlobalTime.T - DashRequestSnapshot) > 0.1F &&
                         DashStamina >= DashCost &&
                         (_args.ActionFlags & (1 << 3)) != 0;

        if (DashRequest) // valid dash
        {
            bool UseViewDir = Wish.sqrMagnitude == 0;

            // Completely eradicate all velocity
            Actor.SetVelocity(Vector3.zero);
            DashStamina -= DashCost;

            Vector3 DashDirection = UseViewDir ? Forward : Wish;
            VectorHeader.ClipVector(ref DashDirection, new Vector3(0, 1, 0));
            DashDirection.Normalize();

            DashRequestSnapshot = GlobalTime.T;

            _args.AssignHold(ApplyDashDuration);

            Actor.SetVelocity(DashDirection * DashVelocityDelta);

            DashEvents?.Invoke(_args, DashState.Enter);
        }
        else if (Grounded) // If we aren't actually dashing, let's rejuvenate our stamina if we're grounded.
        {
            DashStamina += DashGrowthRate * GlobalTime.FDT;
            DashStamina = Mathf.Min(DashStamina, DashCap);
        }
    }

    private bool ApplyDashDuration(ActorArgs _args) // Run X frames
    {
        bool _e = (GlobalTime.T - DashRequestSnapshot) > DashDuration;
        // This is the section we'll want to repeat several times...
        if (_e)
        {
            DashEvents?.Invoke(_args, DashState.Exit);
            return true;
        }

        return false;
    }

    public void AssignDashCallback(Action<ActorArgs, DashState> OnDashEvent)
        => DashEvents += OnDashEvent;

    public void RemoveDashCallback(Action<ActorArgs, DashState> OnDashEvent)
        => DashEvents -= OnDashEvent;
}

[System.Serializable]
public class ActorTilt : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution
{
    [Header("Tilt Parameters")]
    [SerializeField] private Transform CameraTransform;
    [SerializeField] private Transform HandsTransform;
    [SerializeField] private Vector3 LocalEulers;
    [SerializeField] private Vector3 LocalPosition;
    [SerializeField] private float MaximumZTilt = -2F;

    [Header("Hands Parameters")]
    [SerializeField] private Vector3 VelocitySwayMagnitudes = new Vector3(0.05F, 0.05F, 0.05F);
    [SerializeField] private float HandsOffset = 0F;
    [SerializeField] private float SwaySpeed = 3.0F;
    [SerializeField] private float SwayDistance = 0.25F;
    [SerializeField] private AnimationCurve OffsetCurve;

    [System.NonSerialized] private float WishElapsed = 0F;

    protected override void OnExecutionDiscovery(ActorArgs Middleman)
    {
        RegisterExecution();
        BeginExecution();

        LocalEulers = CameraTransform.eulerAngles;
    }
    public override void Simulate(ActorArgs _args)
    {
        ActorHeader.Actor Actor = _args.Actor;
        UnityEngine.Transform ViewTransform = _args.ActorView;
        bool Grounded = Actor.SnapEnabled && Actor.Ground.stable;
        Vector2 RawWish = _args.RawWishDir;

        float _visanglez = 0F;
        float _moveoffset = HandsOffset;

        if (Grounded)
            _visanglez = MaximumZTilt * RawWish[0];
        else
            _visanglez = 0;

        if (RawWish[1] != 0 && Grounded)
            WishElapsed += GlobalTime.FDT;
        else
            WishElapsed = 0F;

        LocalEulers[2] = LocalEulers[2] + (_visanglez - LocalEulers[2]) * (GlobalTime.FDT * 20F);

        _moveoffset += OffsetCurve.Evaluate(WishElapsed * SwaySpeed) * SwayDistance;

        LocalPosition[2] = LocalPosition[2] + (_moveoffset - LocalPosition[2]) * (GlobalTime.FDT * 20F);

        HandsTransform.localPosition = LocalPosition;
        HandsTransform.position += Vector3.Scale(Actor._velocity, VelocitySwayMagnitudes);
        CameraTransform.localRotation = Quaternion.Euler(LocalEulers);
    }
}
public class ActorBehaviour : ConcurrentHeader.ExecutionMachine<ActorArgs>.MonoExecution
{
    // List your Executions here
    [SerializeField] private ActorHolds HoldsExecution;
    [SerializeField] private ActorInput InputExecution;
    [SerializeField] private ActorView ViewExecution;
    [SerializeField] private ActorWish WishExecution;
    [SerializeField] private ActorFriction FrictionExecution;
    [SerializeField] private ActorGravity GravityExecution;
    [SerializeField] private ActorDash DashExecution;
    [SerializeField] private ActorJump JumpExecution;
    [SerializeField] private ActorMove MoveExecution;
    [SerializeField] private ActorTilt TiltExecution;

    public override void OnBehaviourDiscovered(
        Action<ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution>[] ExecutionCommands,
        ActorArgs _args)
    {

        // Assignment Stage
        _args.AssignHoldsObject(HoldsExecution);
        _args.AssignDashObject(DashExecution);

        // Initialize all executions you want here
        HoldsExecution.OnBaseDiscovery(ExecutionCommands, _args);

        InputExecution.OnBaseDiscovery(ExecutionCommands, _args);

        ViewExecution.OnBaseDiscovery(ExecutionCommands, _args);

        WishExecution.OnBaseDiscovery(ExecutionCommands, _args);

        FrictionExecution.OnBaseDiscovery(ExecutionCommands, _args);

        GravityExecution.OnBaseDiscovery(ExecutionCommands, _args);

        DashExecution.OnBaseDiscovery(ExecutionCommands, _args);

        JumpExecution.OnBaseDiscovery(ExecutionCommands, _args);

        MoveExecution.OnBaseDiscovery(ExecutionCommands, _args);

        TiltExecution.OnBaseDiscovery(ExecutionCommands, _args);
    }
}
