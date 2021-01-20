using UnityEngine;
using com.cozyhome.Archetype;
using com.cozyhome.Vectors;
using System;

namespace com.cozyhome.Actors
{
    public static class ActorHeader
    {
        public abstract class Actor : MonoBehaviour
        {
            [SerializeField] private float MaximumStableAngle = 65F;
            public Vector3 _position;
            public Vector3 _velocity;
            public Quaternion _orientation;
            [SerializeField] private LayerMask _Filter;


            [System.NonSerialized]
            public readonly RaycastHit[] _internalhits
                = new RaycastHit[ActorHeader.MAX_HITS];

            [System.NonSerialized]
            public readonly Collider[] _internalcolliders
                = new Collider[ActorHeader.MAX_OVERLAPS];

            [System.NonSerialized]
            public readonly Vector3[] _internalnormals
                = new Vector3[ActorHeader.MAX_PUSHBACKS];

            public RaycastHit[] Hits => _internalhits;
            public Collider[] Colliders => _internalcolliders;
            public Vector3[] Normals => _internalnormals;
            public abstract ArchetypeHeader.Archetype GetArchetype();

            public virtual bool IsStableOnPlane(
                Collider _collider,
                Vector3 _point,
                Vector3 _plane) => Vector3.Angle(
                    _plane, Vector3.up) <= MaximumStableAngle;

            public void Fly(float fdt)
            =>
                PM_FlyMove(ref _velocity, ref _position, fdt, _orientation, _Filter, this);

            public void SetVelocity(Vector3 _velocity)
            => this._velocity = _velocity;

            public void SetPosition(Vector3 _position)
            => this._position = _position;

            public void SetOrientation(Quaternion _orientation)
            => this._orientation = _orientation;
        }

        public static void PM_FlyMove(
            ref Vector3 _vel,
            ref Vector3 _pos,
            float _fdt,
            Quaternion _orient,
            LayerMask _filter,
            Actor _actor)
        {
            // STEPS:
            // 1 : Declare local fields
            // 2 : Initial discrete resolution and storage
            // 3 : Clip initial discrete normals
            // 4 : Begin traceback loop
            // 1 : Trace archetype
            // 2 : Move to time of arrival
            // 3 : Clip velocity
            // 4 : Determine creases/corners

            if (_vel.sqrMagnitude < ActorHeader.MIN_DISPLACEMENT)
                return;

            ArchetypeHeader.Archetype _arc = _actor.GetArchetype();
            Collider _self = _arc.Collider();

            Collider[] _colliders = _actor.Colliders;
            RaycastHit[] _hits = _actor.Hits;
            Vector3[] _planes = _actor.Normals;

            Vector3 _lastclippedplane = new Vector3(0, 1F, 0);

            Vector3 _tracepos = _pos;
            Vector3 _tracevel = _vel;

            float _timeleft = 1F;

            float _bias = ArchetypeHeader.GET_TRACEBIAS(_arc.PrimitiveType());
            float _loss = ArchetypeHeader.GET_TRACELOSS(_arc.PrimitiveType());
            float _skin = ArchetypeHeader.GET_SKINEPSILON(_arc.PrimitiveType());

            int _tracephase = TRACE_DEFAULT;

            // discrete resolution
            PM_FlyPushback(ref _tracepos,
             out int _pushplanes,
            _orient,
            _filter,
            _colliders,
            _planes,
            _self,
            _skin,
            _arc);

            // clip velocity based on current discrete resolution normals
            for (int j = _pushplanes - 1; j >= 0; j--)
            {
                Vector3 _plane = _planes[j];
                if (VectorHeader.Dot(_tracevel, _planes[j]) <= 0F)
                    PM_ResolveFlyVelocity(
                        ref _tracephase,
                        ref _tracevel,
                        ref _lastclippedplane,
                        _plane);
            }

            // trace clip
            for (int i = MAX_BUMPS; i >= 0; i--)
            {
                if (_timeleft <= 0F || _tracevel.sqrMagnitude <= 0F)
                    break;
                else
                {
                    Vector3 _trace = _tracevel * _fdt;
                    float _tracelen = _trace.magnitude;

                    if(_tracelen <= 0F)
                        break;

                    // trace
                    _arc.Trace(
                        _tracepos,
                        _trace / _tracelen,
                        _tracelen + 2 * _skin,
                        _orient,
                        _filter,
                        -_skin,
                        QueryTriggerInteraction.Ignore,
                        _hits,
                        out int _tracesfound);

                    // filter then check
                    ArchetypeHeader.TraceFilters.FindClosestFilterInvalids(
                        ref _tracesfound,
                        out int _i0,
                        _bias,
                        _self,
                        _hits);

                    if (_i0 >= 0)
                    {
                        RaycastHit _closest = _hits[_i0];
                        Vector3 _plane = _closest.normal;

                        float _rto = (_closest.distance) / _tracelen;

                        _timeleft -= (_rto);

                        // trace to time of impact                        
                        _tracepos +=
                                (_trace) * (_rto - _loss);
                        //_tracepos +=
                            //(_plane * _loss);
                        
                        // clip velocity
                        PM_ResolveFlyVelocity(
                            ref _tracephase,
                            ref _tracevel,
                            ref _lastclippedplane,
                            _closest.normal);

                    }
                    else
                    {
                        _tracepos += _trace;
                        _timeleft = 0F;
                    }
                }
            }

            // Reject traceback position if
            // overlap exists at final position, as well as safety being enabled.

            // REFACTOR MAYBE RESEARCH MORE INTO:::

            /*

            // final end fast overlap check:
            _arc.Overlap(_tracepos,
            _orient,
            _filter,
            -_skin,
            QueryTriggerInteraction.Ignore,
            _colliders,
            out int _safetycount);

            ArchetypeHeader.OverlapFilters.FilterSelf(ref _safetycount, _self, _colliders);

            // Set velocity and position to end movement resolution
            //if(_safetycount == 0)
            
            */

            _pos = _tracepos;
            _vel = _tracevel;

            return;
        }

        private static void PM_ResolveFlyVelocity(
            ref int _tracephase,
            ref Vector3 _clipvelocity,
            ref Vector3 _lastclippedplane,
            Vector3 _clippedplane)
        {
            // clip into, remove any velocity traveling into plane

            // switch:
            switch (_tracephase)
            {
                case ActorHeader.TRACE_DEFAULT:
                    VectorHeader.ClipVector(ref _clipvelocity, _clippedplane);
                    _tracephase = ActorHeader.TRACE_PLANE;
                    break;
                case ActorHeader.TRACE_PLANE:

                    Vector3 _prevflatplane = _lastclippedplane;
                    VectorHeader.ClipVector(ref _prevflatplane, Vector3.up);
                    _prevflatplane.Normalize();

                    Vector3 _flatplane = _clippedplane;
                    VectorHeader.ClipVector(ref _flatplane, Vector3.up);
                    _flatplane.Normalize();
                    
                    const float CREASE_EPSILON = -0.01F;
                    float CREASEDOT = VectorHeader.Dot(_prevflatplane, _flatplane);

                    if (CREASEDOT >= CREASE_EPSILON)
                    {
                        Vector3 _crease = Vector3.Cross(
                            _clippedplane,
                            _lastclippedplane);
                        _crease.Normalize();

                        VectorHeader.ProjectVector(ref _clipvelocity, _clippedplane);
                        _tracephase = ActorHeader.TRACE_CREASE;
                    }
                    else
                        VectorHeader.ClipVector(ref _clipvelocity, _clippedplane);
                        
                    break;
                case ActorHeader.TRACE_CREASE:
                    _clipvelocity = Vector3.zero;
                    break;
            }

            _lastclippedplane = _clippedplane;
        }
        private static void PM_FlyPushback(
            ref Vector3 _position,
            out int _pushplanesfound,
            Quaternion _orientation,
            LayerMask _filter,
            Collider[] _colliders,
            Vector3[] _planes,
            Collider _self,
            float _inflate,
            ArchetypeHeader.Archetype _archetype)
        {
            _pushplanesfound = 0;
            bool _overlapsolved = false;

            for (int i = ActorHeader.MAX_PUSHBACKS; i >= 0; i--)
            {
                if (_overlapsolved)
                    break;
                else
                {
                    _archetype.Overlap(_position, 
                    _orientation, 
                    _filter, 
                    0F,
                    QueryTriggerInteraction.Ignore, 
                    _colliders, 
                    out int _overlapsfound);

                    ArchetypeHeader.OverlapFilters.FilterSelf(
                        ref _overlapsfound,
                        _self,
                        _colliders);

                    // FILTER COLLIDERS :::

                    if (_overlapsfound != 0)
                    {
                        for (int j = 0; j < _overlapsfound; j++)
                        {
                            Collider _overlappedcollider = _colliders[j];
                            Transform _overlappedtransform = _overlappedcollider.GetComponent<Transform>();

                            if (Physics.ComputePenetration(
                                _self,
                                _position,
                                _orientation,
                                _overlappedcollider,
                                _overlappedtransform.position,
                                _overlappedtransform.rotation,
                                out Vector3 _resolution,
                                out float _distance))
                            {
                                _position += _resolution * (_distance + _inflate);

                                if (_pushplanesfound < MAX_OVERLAPS)
                                    _planes[_pushplanesfound++] = _resolution;
                            }
                        }
                    }
                    else
                    {
                        _overlapsolved=true;
                        break;
                    }
                }
            }
        }

        public interface IActor
        {
            void OnActorOverlap(Vector3 _normal, Collider _collider);
            void OnActorBump(
                Vector3 _pos, // character's position
                Vector3 _velocity, // character's unclipped velocity
                in RaycastHit _hit // physics's hit structure
            );
        }

        public static readonly int MAX_PUSHBACKS = 8; // # of iterations in our Pushback() funcs
        public static readonly int MAX_BUMPS = 8; // # of iterations in our Move() funcs
        public static readonly int MAX_HITS = 8; // # of RaycastHit[] structs allocated to
                                                 // a hit buffer.
        public static readonly int MAX_OVERLAPS = 8; // # of Collider classes allocated to a
                                                     // overlap buffer.
        public static readonly float MIN_DISPLACEMENT = 0.001F;
        // min squared length of a displacement vector required for a Move() to proceed.

        public const int TRACE_DEFAULT = 0;
        public const int TRACE_PLANE = (1 << 0);
        public const int TRACE_CREASE = (1 << 1);
    }
}