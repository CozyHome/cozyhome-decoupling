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
            SphereArchetype = new ArchetypeHeader.SphereArchetype(
                GetComponent<SphereCollider>()
            );
        }
        public void OnActorBump(Vector3 _pos, Vector3 _velocity, in RaycastHit _hit) { }
        public void OnActorOverlap(Vector3 _normal, Collider _collider) { }

        public override ArchetypeHeader.Archetype GetArchetype()
        => SphereArchetype;
    }
}