using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.cozyhome.Actors;
using com.cozyhome.ConcurrentExecution;
using com.cozyhome.Systems;
using System;


[System.Serializable]
public class EnemyArgs
{
    [Header("Enemy References")]
    public UnityEngine.Transform Self;
    public UnityEngine.Transform Target;
    public ActorHeader.Actor Actor;
}

[System.Serializable]
public class EnemyChase : ConcurrentHeader.ExecutionMachine<EnemyArgs>.ConcurrentExecution
{
    protected override void OnExecutionDiscovery(EnemyArgs Middleman)
    {
        this.RegisterExecution();
        this.BeginExecution();
    }
    public override void Simulate(EnemyArgs _args)
    {
        if (_args.Target == null)
            return;

        ActorHeader.Actor _actor = _args.Actor;
        Vector3 _targetv3 = _args.Target.position;
        Vector3 _selfv3 = _args.Self.position;
        Vector3 _lookv3 = _targetv3 - _selfv3;
        _lookv3.Normalize();
        _lookv3 *= 16F;
        //_lookv3 = Vector3.ClampMagnitude(_lookv3, 1.0F);

        _actor.SetVelocity(_lookv3);
    }
}

[System.Serializable]
public class EnemyMove : ConcurrentHeader.ExecutionMachine<EnemyArgs>.ConcurrentExecution,
                         ActorHeader.IActorReceiver
{
    protected override void OnExecutionDiscovery(EnemyArgs Middleman)
    {
        this.RegisterExecution();
        this.BeginExecution();
    }

    public override void Simulate(EnemyArgs _args)
    {
        ActorHeader.Actor _actor = _args.Actor;
        UnityEngine.Transform _self = _args.Self;

        _actor.SetPosition(_self.position);
        _actor.SetOrientation(_self.rotation);

        ActorHeader.Move(this, _actor, GlobalTime.FDT);

        _self.SetPositionAndRotation(_actor._position, _actor._orientation);
    }

    public void OnGroundHit(ActorHeader.GroundHit _ground, ActorHeader.GroundHit _lastground, LayerMask _gfilter)
    { }

    public void OnTraceHit(RaycastHit _trace, Vector3 _position, Vector3 _velocity)
    { }
}

public class EnemyBehaviour : ConcurrentHeader.ExecutionMachine<EnemyArgs>.MonoExecution
{
    [SerializeField] private EnemyChase ChaseExecution;
    [SerializeField] private EnemyMove MoveExecution;

    public override void OnBehaviourDiscovered(Action<ConcurrentHeader.ExecutionMachine<EnemyArgs>.ConcurrentExecution>[] ExecutionCommands, EnemyArgs Middleman)
    {
        ChaseExecution.OnBaseDiscovery(ExecutionCommands, Middleman);
        MoveExecution.OnBaseDiscovery(ExecutionCommands, Middleman);
    }
}
