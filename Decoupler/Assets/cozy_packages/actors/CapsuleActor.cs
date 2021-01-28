using com.cozyhome.Archetype;
using UnityEngine;

namespace com.cozyhome.Actors
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class CapsuleActor : ActorHeader.Actor, ActorHeader.IActor
    {
        [System.NonSerialized] private ArchetypeHeader.CapsuleArchetype CapsuleArchetype;
        /* UnityEngine */
        void Start()
        {
            SetPosition(transform.position);
            SetOrientation(transform.rotation);
            SetVelocity(Vector3.zero);

            CapsuleArchetype = new ArchetypeHeader.CapsuleArchetype(
                GetComponent<CapsuleCollider>()
            );
        }

        public void OnActorBump(Vector3 _pos, Vector3 _velocity, RaycastHit _hit) { }
        public void OnActorOverlap(Vector3 _normal, Collider _collider) { }

        public override ArchetypeHeader.Archetype GetArchetype()
        => CapsuleArchetype;
    }
}