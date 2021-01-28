using UnityEngine;
using com.cozyhome.Archetype;
using com.cozyhome.Vectors;
using System;

namespace com.cozyhome.Actors
{
    public static class ActorHeader
    {
        public enum SlideSnapType
        {
            Never = 0,
            Toggled = 1,
            Always = 2
        };

        public enum MoveType
        {
            Fly = 0, // PM_FlyMove()
            Slide = 1, // PM_SlideMove()
            Noclip = 2 // PM_NoclipMove()
        };

        public abstract class Actor : MonoBehaviour
        {
            [Header("Move Type Properties")]
            [SerializeField] private MoveType _moveType = MoveType.Fly;
            [SerializeField] private SlideSnapType _snapType = SlideSnapType.Always;

            [Header("Ground Stability Properties")]
            [SerializeField] private float MaximumStableSlideAngle = 65F;

            [Header("Actor Filter Properties")]
            [SerializeField] private LayerMask _filter;

            [System.NonSerialized] private readonly RaycastHit[] _internalhits = new RaycastHit[ActorHeader.MAX_HITS];

            [System.NonSerialized] private readonly Collider[] _internalcolliders = new Collider[ActorHeader.MAX_OVERLAPS];

            [System.NonSerialized] private readonly Vector3[] _internalnormals = new Vector3[ActorHeader.MAX_OVERLAPS];

            [System.NonSerialized] public Vector3 _position;
            [System.NonSerialized] public Vector3 _velocity;
            [System.NonSerialized] public Quaternion _orientation;

            public RaycastHit[] Hits => _internalhits;
            public Collider[] Colliders => _internalcolliders;
            public Vector3[] Normals => _internalnormals;

            public MoveType MoveType => _moveType;

            public SlideSnapType SnapType => _snapType;

            public void Fly(float fdt) => PM_FlyMove(this, ref _position, ref _velocity, _orientation, _filter, fdt);

            public void SetVelocity(Vector3 _velocity) => this._velocity = _velocity;
            public void SetPosition(Vector3 _position) => this._position = _position;
            public void SetOrientation(Quaternion _orientation) => this._orientation = _orientation;

            public abstract ArchetypeHeader.Archetype GetArchetype();
        }

        public static void PM_FlyMove(
            Actor _actor,
            ref Vector3 _pos,
            ref Vector3 _vel,
            Quaternion _orient,
            LayerMask _filter,
            float _fdt)
        {
            // STEPS:
            // RUN :
            // (OVERLAP -> PUSHBACK -> CONVEX HULL NORMAL (NEARBY PLANE DETECTION) -> GENERATE TOPOLOGY BITMASK -> TRACING -> REPEAT

            ArchetypeHeader.Archetype _arc = _actor.GetArchetype();

            Collider[] _colliders = _actor.Colliders;
            Collider _self = _arc.Collider();

            Vector3[] _normals = _actor.Normals;
            RaycastHit[] _traces = _actor.Hits;

            Vector3 _tracepos = _pos;

            float _tf = 1F; // time factor
            float _skin = ArchetypeHeader.GET_SKINEPSILON(_arc.PrimitiveType());
            float _loss = ArchetypeHeader.GET_TRACELOSS(_arc.PrimitiveType());

            int _bumpcount = 0;
            int _pushbackcount = 0;

            // Inflate our collider to successfully compute a penetration vector.

            Vector3 _l = Vector3.up;
            int _tflags = 0;

            // Attempt an Overlap Pushback at this current position:
            while (_pushbackcount++ < ActorHeader.MAX_PUSHBACKS)
            {
                _arc.Overlap(
                    _tracepos,
                    _orient,
                    _filter,
                    0F,
                    QueryTriggerInteraction.Ignore,
                    _colliders,
                    out int _overlapsfound);

                ArchetypeHeader.OverlapFilters.FilterSelf(
                    ref _overlapsfound,
                    _self,
                    _colliders);

                if (_overlapsfound == 0) // nothing !
                    break;
                else
                {
                    // Actually resolve our position whilst also keeping note of normals discovered:
                    for (int _colliderindex = 0; _colliderindex < _overlapsfound; _colliderindex++)
                    {
                        Collider _other = _colliders[_colliderindex];
                        Transform _otherT = _other.GetComponent<Transform>();

                        if (Physics.ComputePenetration(_self, _tracepos, _orient, _other, _otherT.position, _otherT.rotation, out Vector3 _normal, out float _distance))
                        {
                            // im assuming Unity computes the GJK with a penetration vector. Nevertheless, we're only resolving pushback here, I personally don't really trust the
                            // normals detected here, my inflated overlap works but at the expense of allowing our trace to detect any rigidbodies... So i've removed that in favor
                            // of interaction with the physics system
                            _tracepos += _normal * (_distance + _skin);

                            if (VectorHeader.Dot(_vel, _normal) < 0F) // In this overlap, we  want the immediate normals
                                PM_DetermineNearbyTopology(ref _vel, ref _l, _normal, ref _tflags);
                            break;
                        }
                    }
                }
            }

            while (_bumpcount++ <= ActorHeader.MAX_BUMPS
                  && _tf > 0)
            {
                // Begin Trace
                Vector3 _trace = _vel * _fdt;
                float _tracelen = _trace.magnitude;

                // IF unable to trace any further, break and end
                if (_tracelen <= MIN_DISPLACEMENT)
                {
                    _tf = 0;
                    break;
                }
                else
                {
                    _arc.Trace(
                    _tracepos,
                    _trace / _tracelen,
                    _tracelen + _skin,
                    _orient,
                    _filter,
                    0F,
                    QueryTriggerInteraction.Ignore,
                    _traces,
                    out int _tracecount);

                    ArchetypeHeader.TraceFilters.FindClosestFilterInvalids(
                        ref _tracecount,
                        out int _i0,
                        ArchetypeHeader.GET_TRACEBIAS(_arc.PrimitiveType()),
                        _self,
                        _traces);

                    if (_i0 <= -1) // nothing discovered:::
                    {
                        _tf = 0; // end move
                        _tracepos += _trace;
                        break;
                    }
                    else // discovered an obstruction:::
                    {
                        RaycastHit _closest = _traces[_i0];
                        float _rto = _closest.distance / _tracelen;
                        _tf -= _rto;

                        float _dis = Mathf.Max(_closest.distance - _skin, 0F);
                        _tracepos += (_trace / _tracelen) * _dis; // move back along the trace line!

                        PM_DetermineNearbyTopology(ref _vel, ref _l, _closest.normal, ref _tflags);

                        continue;
                    }
                }
            }

            _pos = _tracepos;
        }

        private static void PM_DetermineNearbyTopology(ref Vector3 _v, ref Vector3 _l, Vector3 _n, ref int _tflags)
        {
            switch (_tflags)
            {
                case 0:
                    PM_FlyClipVelocity(ref _v, _n);
                    _tflags |= (1 << 0);
                    break;
                case (1 << 0):
                    if (Mathf.Abs(VectorHeader.Dot(_l, _n)) >= FLY_CREASE_EPSILON)
                    {
                        Vector3 _c = Vector3.Cross(_l, _n);
                        _c.Normalize();
                        VectorHeader.ProjectVector(ref _v, _c);
                        _tflags |= (1 << 1);
                    }
                    else
                        PM_FlyClipVelocity(ref _v, _n);
                    break;
                case (1 << 0) | (1 << 1):
                    _v = Vector3.zero;
                    _tflags |= (1 << 2);
                    break;
            }

            _l = _n;
        }

        public static void PM_FlyClipVelocity(ref Vector3 _velocity, Vector3 _plane)
        {
            float _m = _velocity.magnitude;
            if (_m <= MIN_DISPLACEMENT)
                return;
            else
                if (VectorHeader.Dot(_velocity / _m, _plane) < ActorHeader.FLY_CLIP_EPSILON) // only clip if we're piercing into the infinite plane 
                    VectorHeader.ClipVector(ref _velocity, _plane);
        }

        public interface IActor
        {
            void OnActorOverlap(Vector3 _normal, Collider _collider);
            void OnActorBump(
                Vector3 _pos, // character's position
                Vector3 _velocity, // character's unclipped velocity
                RaycastHit _hit // physics's hit structure
            );
        }

        public const int MAX_GROUNDBUMPS = 2; // # of ground snaps/iterations in a SlideMove() 
        public const int MAX_PUSHBACKS = 3; // # of iterations in our Pushback() funcs
        public const int MAX_BUMPS = 8; // # of iterations in our Move() funcs
        public const int MAX_HITS = 8; // # of RaycastHit[] structs allocated to
                                       // a hit buffer.
        public const int MAX_OVERLAPS = 8; // # of Collider classes allocated to a
                                           // overlap buffer.
        public const float MIN_DISPLACEMENT = 0.001F; // min squared length of a displacement vector required for a Move() to proceed.
        public const float FLY_CLIP_EPSILON = 0F; // minimum correlation respondance between the velocity vector and normal plane to allow for a clip during a FlyMove()
        public const float FLY_CREASE_EPSILON = 0.0001F; // minimum distance angle during a crease check to disregard any normals being queried.
    }
}