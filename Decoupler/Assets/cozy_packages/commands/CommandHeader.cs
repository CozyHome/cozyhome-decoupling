using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.cozyhome.Actors;

namespace com.cozyhome.Commands
{
    public static class CommandHeader
    {
        // RET TRUE to keep in loop, RET FALSE to remove from list
        public delegate bool ActorAction(ActorHeader.Actor _actor);

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
    }
}
