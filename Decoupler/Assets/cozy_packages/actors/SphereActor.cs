using com.cozyhome.Archetype;
using UnityEngine;

namespace com.cozyhome.Actors
{
    [RequireComponent(typeof(SphereCollider))] public class SphereActor : ActorHeader.Actor, ActorHeader.IActor
    {
        [System.NonSerialized] private ArchetypeHeader.SphereArchetype SphereArchetype;
        /* UnityEngine */
        void Start() 
        {
            SetPosition(transform.position);
            SetOrientation(transform.rotation);
            SetVelocity(Vector3.zero);
        
            SphereArchetype = new ArchetypeHeader.SphereArchetype(
                GetComponent<SphereCollider>()
            );
        }
        public void OnActorBump(Vector3 _pos, Vector3 _velocity, RaycastHit _hit) { }
        public void OnActorOverlap(Vector3 _normal, Collider _collider) { }

        public override ArchetypeHeader.Archetype GetArchetype()
        => SphereArchetype;
    }
}