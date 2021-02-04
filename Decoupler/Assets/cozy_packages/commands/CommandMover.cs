using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.cozyhome.Actors;
using com.cozyhome.Systems;

public static class CommandHeader
{
    public delegate void ActorAction(ActorHeader.Actor _actor);

    public static void ApplyActorTransform(Transform _t, ActorHeader.Actor _a)
    {
        _a._position = _t.position;
        _a._orientation = _t.rotation;
    }

    public static void ApplyTransformActor(Transform _t, ActorHeader.Actor _a)
    {
        _t.position = _a._position;
        _t.rotation = _a._orientation;
    }

    public static void ActorBounce(ActorHeader.Actor _a)
    {
        // await for ground detection only if we weren't grounded last frame.
        if (_a.Ground.stable && !_a.LastGround.stable)
        {
            _a.SetVelocity(_a.Ground.normal * (UnityEngine.Random.value * 10F + 5F));
            _a.SetSnapEnabled(false);
            return;
        }

        // wait at least two frames until we determine eligiblity to snap again.
        if (!_a.Ground.stable && !_a.LastGround.stable)
        {
            _a._velocity -= Vector3.up * 39.62F * GlobalTime.FDT;
            _a.SetSnapEnabled(true);
            return;
        }
    }
}

// new class don't expect much
public class CommandMover : MonoBehaviour, ActorHeader.IActorReceiver
{
    [System.NonSerialized] private ActorHeader.Actor _actor;

    private List<CommandHeader.ActorAction> _actions = new List<CommandHeader.ActorAction>();

    public void OnGroundHit(in ActorHeader.GroundHit _ground, in ActorHeader.GroundHit _lastground, LayerMask _gfilter)
    {

    }

    public void OnTraceHit(in RaycastHit _trace, in Vector3 _position, in Vector3 _velocity)
    {

    }

    // i want a system that allows me to append actions/listener methods
    // to some pulse that an object will be attached to. (akin to the systems package)

    // this way I can append something like AwaitLanding() => _actor.setvelocity(up * 10); _actor.snapdisabled = true;
    // a simple execution state would be waiting to discover a ground, and then bouncing off of it.

    private void FixedUpdate()
    {
        for (int i = 0; i < _actions.Count; i++)
            _actions[i].Invoke(_actor);
    }

    private void Start()
    {
        _actor = GetComponent<ActorHeader.Actor>();
        _actor.SetMoveType(ActorHeader.MoveType.Slide);
        _actor.SetSnapType(ActorHeader.SlideSnapType.Toggled);
        _actor.SetSnapEnabled(true);

        // update actor state to transform state
        _actions.Add((_actor) => CommandHeader.ApplyActorTransform(transform, _actor));

        // bounce procedure
        _actions.Add((_actor) => CommandHeader.ActorBounce(_actor));

        // move procedure
        _actions.Add((_actor) => { ActorHeader.Move(this, _actor, GlobalTime.FDT); });

        // update transform state to actor state
        _actions.Add((_actor) => CommandHeader.ApplyTransformActor(transform, _actor));
    }
}
