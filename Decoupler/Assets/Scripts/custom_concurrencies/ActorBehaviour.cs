using System;
using UnityEngine;
using com.cozyhome.Actors;
using com.cozyhome.ConcurrentExecution;
using com.cozyhome.Systems;

// leave values in here to be mostly primitive with exceptions to essentials like the Animator
// etc..
[System.Serializable]
public class ActorArgs
{
    public ActorHeader.Actor Actor;
    public UnityEngine.Transform ActorTransform;
    public UnityEngine.Transform ActorView;
}

[System.Serializable]
public class ActorMove : ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution,
                         ActorHeader.IActorReceiver
{
    protected override void OnExecutionDiscovery()
    {
        RegisterExecution();
        BeginExecution();
    }

    public override void Simulate(ActorArgs _args)
    {
        ActorHeader.Actor _a = _args.Actor;
        Transform _t = _args.ActorTransform;
        Transform _v = _args.ActorView;

        // write to actors
        _a._position = _t.position;
        _a._orientation = _t.rotation;

        // generate vel
        _v.rotation *= Quaternion.AngleAxis(45F * GlobalTime.FDT, new Vector3(0, 1, 0));
        _a._velocity = _v.forward;

        // move
        ActorHeader.Move(this, _a, GlobalTime.FDT);

        // upd
        _t.position = _a._position;
        _t.rotation = _a._orientation;
    }

    public void OnGroundHit(in ActorHeader.GroundHit _ground, in ActorHeader.GroundHit _lastground, LayerMask _gfilter) { }
    public void OnTraceHit(in RaycastHit _trace, in Vector3 _position, in Vector3 _velocity) { }
}

public class ActorBehaviour : ConcurrentHeader.ExecutionMachine<ActorArgs>.MonoExecution
{
    // List your Executions here
    [SerializeField] private ActorMove MoveExecution;

    public override void OnBehaviourDiscovered(
        Action<ConcurrentHeader.ExecutionMachine<ActorArgs>.ConcurrentExecution>[] ExecutionCommands)
    {
        // Initialize all executions you want here
        MoveExecution.OnBaseDiscovery(ExecutionCommands);
    }
}
