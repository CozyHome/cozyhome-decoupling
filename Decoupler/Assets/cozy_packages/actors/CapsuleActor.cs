using com.cozyhome.Archetype;
using UnityEngine;

namespace com.cozyhome.Actors
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class CapsuleActor : ActorHeader.Actor
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

        public override ArchetypeHeader.Archetype GetArchetype()
        => CapsuleArchetype;

        public override bool DeterminePlaneStability(Vector3 _normal, Collider _other) 
        {
            return base.DeterminePlaneStability(_normal, _other);
        }

        public override bool DetermineGroundStability(in RaycastHit _hit)
        {
            return base.DetermineGroundStability(_hit);
        }
    }
}