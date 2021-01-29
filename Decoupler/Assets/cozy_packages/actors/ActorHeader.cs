using UnityEngine;
using com.cozyhome.Archetype;
using com.cozyhome.Vectors;
using System;

namespace com.cozyhome.Actors
{
    public static class ActorHeader
    {
        public enum SlideSnapType { Never = 0, Toggled = 1, Always = 2 };
        public enum MoveType { Fly = 0, /* PM_FlyMove() */ Slide = 1, /* PM_SlideMove() */  Noclip = 2 /* PM_NoclipMove() */  };

        public struct GroundHit 
        {
            public Vector3 point; // our trace point
            public Vector3 normal; // our trace normal
            public float distance; // our trace distance

            
        }

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

        public static void PM_SlideMove(
            Actor _actor,
            ref Vector3 _pos,
            ref Vector3 _vel,
            Quaternion _orient,
            LayerMask _filter,
            float _fdt
        )
        {
            SlideSnapType _snaptype = _actor.SnapType;

            /* STEPS:
                RUN:
                GROUND TRACE & GROUND SNAP -> OVERLAP -> PUSHBACK -> CONVEX HULL NORMAL (NEARBY PLANE DETECTION) -> GENERATE GEOMETRY BITMASK -> TRACING -> REPEAT
            */

            ArchetypeHeader.Archetype _arc = _actor.GetArchetype();
            Collider[] _colliders = _actor.Colliders;
            Collider _self = _arc.Collider();

            Vector3[] _normals = _actor.Normals;
            RaycastHit[] _traces = _actor.Hits;

            Vector3 _tracepos = _pos;
            Vector3 _groundtracepos = _pos;

            Vector3 _lastplane = Vector3.zero;
            Vector3 _groundtracedir = _orient * new Vector3(0, -1, 0);

            float _tf = 1F;
            float _skin = ArchetypeHeader.GET_SKINEPSILON(_arc.PrimitiveType());

            int _bumpcount = 0;
            int _groundbumpcount = 0;
            int _pushbackcount = 0;
            int _gflags = 0;

            float _groundtracelen = .5F;

            while (_groundbumpcount++ < MAX_GROUNDBUMPS &&
            _groundtracelen > 0F)
            {
                // trace along dir
                // if detected 
                // if stable : 
                // end trace and determine whether a snap is to occur
                // else :
                // clip along floor
                // continue
                // else : 
                // break out of loop as no floor was detected
                _arc.Trace(
                    _groundtracepos,
                    _groundtracedir,
                    _groundtracelen + _skin,
                    _orient,
                    _filter,
                    0F,
                    QueryTriggerInteraction.Ignore,
                    _traces,
                    out int _groundtraces);

                ArchetypeHeader.TraceFilters.FindClosestFilterInvalids(
                    ref _groundtraces,
                    out int _i0,
                    ArchetypeHeader.GET_TRACEBIAS(_arc.PrimitiveType()),
                    _self,
                    _traces);

                if (_i0 >= 0) // an intersection has occured, but we aren't sure its ground yet
                {
                    RaycastHit _closest = _traces[_i0];

                    _groundtracepos += _groundtracedir * _closest.distance;
                    // warp regardless of stablility. We'll only be setting our trace position
                    // to our ground trace position if a stable floor has been determined, and snapping is enabled. 

                    if ( _arc.DetermineGroundStability(in _closest) )
                    {
                        _groundtracepos += (_orient * new Vector3(0, 1, 0)) * _skin;
                        _tracepos = _groundtracepos;
                    }
                    else
                    {
                        // clip, normalize, and continue:
                        VectorHeader.ClipVector(ref _groundtracedir, _closest.normal);
                        _groundtracedir.Normalize();
                        _groundtracelen -= _closest.distance;
                    }
                }
                else // nothing discovered, end out of our ground loop.
                {
                    _groundtracelen = 0F;
                }
            }

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
            // OVERLAP -> PUSHBACK -> CONVEX HULL NORMAL (NEARBY PLANE DETECTION) -> GENERATE GEOMETRY BITMASK -> TRACING -> REPEAT

            /*
                I've thought long and hard about providing documentation and comments to explain the fuckery that occurs
                in these lines of code, and I think it'd be best I do so.
            */

            /* 
                Initializing our local variables at the very top of our function. I'd like to maintain this codebase to be as
                functional as possible, starting with the ActorHeader class. 
            */

            /*
            
                Here i'll be summarizing the usage of each field found below before our discrete resolution loop:

                Archetype - The Archetype class allows us to access each primitive's Archetype class which is required to be bundled
                with the Monobehaviour Actor variants. Each Archetype contains its own primitive implementation provided by the Unity Physics API,
                and allows us to interface with each primitive without explicitly hardcoding each type's movement.

                Colliders[] - The Colliders[] array is a crucial part to our discrete resolution loop, as it stores all potential colliders discovered in
                our archetype's Overlap() query. By storing these Colliders, it allows us to act upon them and attempt to compute a penetration vector and distance.
                This penetration vector and distance allows us to push our actor outside of any potentially overlapping colliders.

                Vector3[] Normals - The Normals[] array isn't necessarily important as of this moment. However, I'm thinking I reuse this in some callback for the
                IActor interface.

                RaycastHit[] - The RaycastHit[] traces array is crucial for the Continuous Collision Detection (CCD) portion of our resolution loop. 
                Unity's Physics API uses the RaycastHit structure as the main output component for interfacing with its various casting methods. 
                In our case, we don't want to generate garbage during our trace, so we use the NonAlloc() variant for our linear cast.

                We then operate on this array and filter it to find the closest valid trace intersection for our interception resolution.

                Vector3 TracePos - We create a copy of our actor's position to operate upon during our movement. This is implemented just in case any last minute
                'safety' detections need to run and determine if the newly calculated position is safe to warp to.

                Vector3 LastPlane - We create a local Vector3 to store the previous plane we've encountered during our resolution loop.
                I'd initially settled with Q1's "clip and pray" approach to dealing with creases, followed by a for loop looking for a crease. 
                However, Q1's initial crease/corner detection loop was is kinda clunky and ugly and I'm not even sure it works 100% of the time.

                I must confess, this method of detection found in PM_DetermineNearbyTopology() isn't pretty either, but it gets the job done perfectly.

                float TimeLeft - I use a fraction instead of the actual distance required, as its generally much cleaner to express what is occuring then to iteratively
                subtract the traced distance. 1 = trace has just begun, 0 = trace is finished. Any value in between is a percentage of how much further we need to travel. 

                float Skin - the skin variable is important as it prevents tunneling when the Actor gets asymptotically close to the surface of any infinite plane
                detected in either the discrete pushback phase or continuous tracing phase.

                int BumpCount - stores the amount of bump iterations that have occured during our resolution loop.

                int PushbackCount - stores the amount of discrete pushbacks that have occured during our resolution loop.

                int GeometryFlags - stores the current state of our Actor's immediate surroundings. 0 = Discovered Plane, 1 = Discovered Crease, 3 = Discovered Corner
            */

            ArchetypeHeader.Archetype _arc = _actor.GetArchetype();
            Collider[] _colliders = _actor.Colliders;
            Collider _self = _arc.Collider();

            Vector3[] _normals = _actor.Normals;
            RaycastHit[] _traces = _actor.Hits;

            Vector3 _tracepos = _pos;
            Vector3 _lastplane = Vector3.zero;

            float _tf = 1F;
            float _skin = ArchetypeHeader.GET_SKINEPSILON(_arc.PrimitiveType());

            int _bumpcount = 0;
            int _pushbackcount = 0;
            int _gflags = 0;

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
                    for (int _colliderindex = 0; _colliderindex < _overlapsfound; _colliderindex++)
                    {
                        Collider _other = _colliders[_colliderindex];
                        Transform _otherT = _other.GetComponent<Transform>();

                        if (Physics.ComputePenetration(_self, _tracepos, _orient, _other, _otherT.position, _otherT.rotation, out Vector3 _normal, out float _distance))
                        {
                            _tracepos += _normal * (_distance + _skin);

                            if (VectorHeader.Dot(_vel, _normal) < 0F) // In this overlap, we  want the immediate normals
                                PM_DetermineImmediateGeometry(ref _vel, ref _lastplane, _normal, ref _gflags);
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

                        PM_DetermineImmediateGeometry(ref _vel, ref _lastplane, _closest.normal, ref _gflags); // determine our topology state
                        continue;
                    }
                }
            }

            _pos = _tracepos;
        }

        private static void PM_DetermineImmediateGeometry(ref Vector3 _v, ref Vector3 _l, Vector3 _n, ref int _gflags)
        {
            switch (_gflags)
            {
                case 0: // plane detected
                    PM_FlyClipVelocity(ref _v, _n);
                    _gflags |= (1 << 0);
                    break;
                case (1 << 0): // potential crease detected
                    if (Mathf.Abs(VectorHeader.Dot(_l, _n)) >= FLY_CREASE_EPSILON)
                    {
                        Vector3 _c = Vector3.Cross(_l, _n);
                        _c.Normalize();
                        VectorHeader.ProjectVector(ref _v, _c);
                        _gflags |= (1 << 1);
                    }
                    else
                        PM_FlyClipVelocity(ref _v, _n);
                    break;
                case (1 << 0) | (1 << 1): // corner detected
                    _v = Vector3.zero;
                    _gflags |= (1 << 2);
                    break;
            }
            _l = _n;
        }

        public static void PM_FlyClipVelocity(ref Vector3 _velocity, Vector3 _plane)
        {
            float _m = _velocity.magnitude;
            if (_m <= 0F) // preventing NaN generation
                return;
            else
                if (VectorHeader.Dot(_velocity / _m, _plane) < 0F) // only clip if we're piercing into the infinite plane 
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
        public const float FLY_CREASE_EPSILON = 0.0001F; // minimum distance angle during a crease check to disregard any normals being queried.
    }
}