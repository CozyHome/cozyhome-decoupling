using System.Collections.Generic;
using UnityEngine;

using com.cozyhome.Actors;
using com.cozyhome.Systems;

namespace com.cozyhome.Commands
{
    // new class don't expect much
    public class CommandMover : MonoBehaviour, ActorHeader.IActorReceiver
    {
        [SerializeField] int _bouncecount = 0;
        [System.NonSerialized] private ActorHeader.Actor _actor;

        private List<CommandHeader.ActorAction> _actions = new List<CommandHeader.ActorAction>();

        public void OnGroundHit(ActorHeader.GroundHit _ground, ActorHeader.GroundHit _lastground, LayerMask _gfilter) { }

        public void OnTraceHit(RaycastHit _trace, Vector3 _position, Vector3 _velocity) { }

        // i want a system that allows me to append actions/listener methods
        // to some pulse that an object will be attached to. (akin to the systems package)

        // this way I can append something like AwaitLanding() => _actor.setvelocity(up * 10); _actor.snapdisabled = true;
        // a simple execution state would be waiting to discover a ground, and then bouncing off of it.

        private void FixedUpdate()
        {
            for (int i = 0; i < _actions.Count; i++)
                if (!_actions[i].Invoke(_actor))
                    _actions.RemoveAt(i);
        }

        private void Start()
        {
            _actor = GetComponent<ActorHeader.Actor>();
            _actor.SetMoveType(ActorHeader.MoveType.Slide);
            _actor.SetSnapType(ActorHeader.SlideSnapType.Toggled);
            _actor.SetSnapEnabled(true);

            // update actor state to transform state
            _actions.Add((_actor) =>
            {
                CommandHeader.ApplyActorTransform(transform, _actor);
                return true;
            });

            // gravity
            _actions.Add((_actor) =>
            {
                if (!_actor.Ground.stable)
                    _actor._velocity -= Vector3.up * 39.62F * GlobalTime.FDT;
                return true;
            });

            // bounce determinism
            _actions.Add((_actor) =>
            {
                if (_bouncecount <= 0)
                {
                    _actor.SetVelocity(Vector3.zero);
                    _actor.SetSnapEnabled(false);
                    return false;
                }
                else
                {
                    if (_actor.Ground.stable)
                    {
                        _bouncecount--;
                        _actor.SetSnapEnabled(false);
                        _actor.SetVelocity(_actor.Ground.normal * (_bouncecount));
                    }

                    return true;
                }
            });

            // snap detection procedure
            _actions.Add((_actor) =>
            {
                if (_bouncecount <= 0)
                    return false;
                else
                {
                    // wait at least two frames until we determine eligiblity to snap again.
                    if (!_actor.Ground.stable && !_actor.LastGround.stable)
                        _actor.SetSnapEnabled(true);
                    return true;
                }
            });

            // move procedure
            _actions.Add((_actor) =>
            {
                if (_bouncecount <= 0)
                    return false;
                else
                {
                    ActorHeader.Move(this, _actor, GlobalTime.FDT);
                    return true;
                }
            });

            // update transform state to actor state
            _actions.Add((_actor) =>
            {
                CommandHeader.ApplyTransformActor(transform, _actor);
                return true;
            });
        }
    }

}

