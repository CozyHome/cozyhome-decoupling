using UnityEngine;

namespace com.cozyhome.Archetype
{
    public static class ArchetypeHeader
    {
        public static class OverlapFilters
        {
            public static void FilterSelf(
                ref int _overlapsfound,
                Collider _self,
                Collider[] _colliders)
            {
                int nb_found = _overlapsfound;
                for (int i = nb_found - 1; i >= 0; i--)
                {
                    if (_colliders[i] == _self)
                    {
                        nb_found--;

                        if (i < nb_found)
                            _colliders[i] = _colliders[nb_found];
                    }
                    else
                        continue;
                }

                _overlapsfound = nb_found;
            }
        }

        public static class TraceFilters
        {
            public static void FindClosestFilterInvalids(
                ref int _tracesfound,
                out int _closestindex,
                float _bias,
                Collider _self,
                RaycastHit[] _hits)
            {
                int nb_found = _tracesfound;
                float _closestdistance = Mathf.Infinity;
                _closestindex = -1;

                for (int i = nb_found - 1; i >= 0; i--)
                {
                    _hits[i].distance -= _bias;
                    RaycastHit _hit = _hits[i];
                    float _tracelen = _hit.distance;

                    if (_tracelen > 0F &&
                        !_hit.collider.Equals(_self))
                    {
                        if (_tracelen < _closestdistance)
                        {
                            _closestdistance = _tracelen;
                            _closestindex = i;
                        }
                    }
                    else
                    {
                        nb_found--;

                        if (i < nb_found)
                            _hits[i] = _hits[nb_found];
                    }
                }
            }
        }

        /* Archetype Mappings */

        public static readonly int ARCHETYPE_SPHERE = 0;
        public static readonly int ARCHETYPE_CAPSULE = 1;
        public static readonly int ARCHETYPE_BOX = 2;

        private static readonly float[] SKINEPSILON = new float[3]
        {
            0.01F, // sphere
            0.01F, // capsule
            0.01F // box
        };

        private static readonly float[] TRACEBIAS = new float[3]
        {
            0.02F, // sphere
            0.02F, // capsule
            0.02F // box
        };

        private static readonly float[] TRACELOSS = new float[3]
        {
            0.01F, // sphere
            0.01F, // capsule
            0.01F // box
        };

        public static float GET_SKINEPSILON(int _i0) => SKINEPSILON[_i0];
        public static float GET_TRACEBIAS(int _i0) => TRACEBIAS[_i0];

        public static float GET_TRACELOSS(int _i0) => TRACELOSS[_i0];

        [System.Serializable]
        public abstract class Archetype : Object
        {

            public abstract void Overlap(
                Vector3 _pos,
                Quaternion _orient,
                LayerMask _filter,
                float _inflate,
                QueryTriggerInteraction _interacttype,
                Collider[] _colliders,
                out int _overlapcount);
            public abstract void Trace(
                Vector3 _pos,
                Vector3 _direction,
                float _len,
                Quaternion _orient,
                LayerMask _filter,
                float _inflate,
                QueryTriggerInteraction _interacttype,
                RaycastHit[] _hits,
                out int _tracecount);

            public abstract Collider Collider();
            public abstract int PrimitiveType();

            public abstract void Inflate(float _amt);
            public abstract void Deflate(float _amt);
            public virtual bool DetermineGroundStability(in RaycastHit _hit) => true;
        }

        [System.Serializable]
        public class SphereArchetype : Archetype
        {
            [SerializeField] SphereCollider _collider;

            public SphereArchetype(SphereCollider _collider)
            => this._collider = _collider;

            public override void Overlap(Vector3 _pos, Quaternion _orient, LayerMask _filter, float _inflate, QueryTriggerInteraction _interacttype, Collider[] _colliders, out int _overlapcount)
            {
                _overlapcount = 0;
                _pos += _orient * _collider.center;

                _overlapcount = Physics.OverlapSphereNonAlloc(
                    _pos,
                    _collider.radius + _inflate,
                    _colliders,
                    _filter,
                    _interacttype);
                return;
            }

            public override void Trace(Vector3 _pos, Vector3 _direction, float _len, Quaternion _orient, LayerMask _filter, float _inflate, QueryTriggerInteraction _interacttype, RaycastHit[] _hits, out int _tracecount)
            {
                _tracecount = 0;
                _pos += _orient * _collider.center;
                _pos -= _direction * TRACEBIAS[ARCHETYPE_SPHERE];

                _tracecount = Physics.SphereCastNonAlloc(
                    _pos,
                    _collider.radius + _inflate,
                    _direction,
                    _hits,
                    _len + TRACEBIAS[ARCHETYPE_SPHERE],
                    _filter,
                    _interacttype);
                return;
            }

            public override Collider Collider()
            => _collider;

            public override int PrimitiveType()
            => ARCHETYPE_SPHERE;

            public override void Inflate(float _amt)
            {
                _collider.radius += _amt;
            }

            public override void Deflate(float _amt)
            {
                _collider.radius -= _amt;
            }
        }

        [System.Serializable]
        public class CapsuleArchetype : Archetype
        {
            [SerializeField] CapsuleCollider _collider;

            public CapsuleArchetype(CapsuleCollider _collider)
            => this._collider = _collider;

            public override void Overlap(Vector3 _pos, Quaternion _orient, LayerMask _filter, float _inflate, QueryTriggerInteraction _interacttype, Collider[] _colliders, out int _overlapcount)
            {
                _overlapcount = 0;
                _pos += _orient * _collider.center;
                Vector3 _u = _orient * new Vector3(0, 1, 0);
                float rh = _inflate + _collider.height * .5F - _collider.radius;
                Vector3 _p0 = _pos - _u * (rh);
                Vector3 _p1 = _pos + _u * (rh);

                _overlapcount = Physics.OverlapCapsuleNonAlloc(_p0, _p1, _collider.radius + _inflate, _colliders, _filter, _interacttype);
                return;
            }
            public override void Trace(Vector3 _pos, Vector3 _direction, float _len, Quaternion _orient, LayerMask _filter, float _inflate, QueryTriggerInteraction _interacttype, RaycastHit[] _hits, out int _tracecount)
            {
                _tracecount = 0;
                _pos += _orient * _collider.center;
                _pos -= _direction * TRACEBIAS[ARCHETYPE_CAPSULE];

                Vector3 _u = _orient * new Vector3(0, 1, 0);
                float rh = _inflate + _collider.height * .5F - _collider.radius;
                Vector3 _p0 = _pos - _u * (rh);
                Vector3 _p1 = _pos + _u * (rh);

                _tracecount = Physics.CapsuleCastNonAlloc(
                    _p0,
                     _p1,
                     _collider.radius + _inflate,
                     _direction,
                     _hits,
                     _len + TRACEBIAS[ARCHETYPE_CAPSULE],
                     _filter,
                     _interacttype);
                return;
            }
            public override Collider Collider()
            => _collider;
            public override int PrimitiveType()
            => ARCHETYPE_CAPSULE;

            public override void Inflate(float _amt)
            {
                _collider.height += _amt;
                _collider.radius += _amt / 2F;
            }

            public override void Deflate(float _amt)
            {
                _collider.height -= _amt;
                _collider.radius -= _amt / 2F;
            }
        }

        [System.Serializable]
        public class BoxArchetype : Archetype
        {
            [SerializeField] BoxCollider _collider;

            public BoxArchetype(BoxCollider _collider)
            => this._collider = _collider;

            public override void Overlap(Vector3 _pos, Quaternion _orient, LayerMask _filter, float _inflate, QueryTriggerInteraction _interacttype, Collider[] _colliders, out int _overlapcount)
            {
                _overlapcount = 0;
                _pos += _orient * _collider.center;
                Vector3 _he = _collider.size * .5F;

                // inflate
                for (int i = 0; i < 3; i++)
                    _he[i] += _inflate;

                _overlapcount = Physics.OverlapBoxNonAlloc(_pos, _he, _colliders, _orient, _filter, _interacttype);
                return;
            }

            public override void Trace(Vector3 _pos, Vector3 _direction, float _len, Quaternion _orient, LayerMask _filter, float _inflate, QueryTriggerInteraction _interacttype, RaycastHit[] _hits, out int _tracecount)
            {
                _tracecount = 0;
                _pos += _orient * _collider.center;
                _pos -= _direction * TRACEBIAS[ARCHETYPE_BOX];

                Vector3 _he = _collider.size * .5F;
                for (int i = 0; i < 3; i++)
                    _he[i] += _inflate;

                _tracecount = Physics.BoxCastNonAlloc(_pos,
                _he,
                _direction,
                _hits,
                _orient,
                _len + TRACEBIAS[ARCHETYPE_BOX],
                _filter,
                _interacttype);
                return;
            }

            public override Collider Collider()
            => _collider;

            public override int PrimitiveType()
            => ARCHETYPE_BOX;

            public override void Inflate(float _amt)
            {
                Vector3 _sz = _collider.size;
                for (int i = 0; i < 3; i++)
                    _sz[i] += _amt;

                _collider.size = _sz;
            }

            public override void Deflate(float _amt)
            {
                Vector3 _sz = _collider.size;
                for (int i = 0; i < 3; i++)
                    _sz[i] -= _amt;

                _collider.size = _sz;
            }
        }
    }
}
