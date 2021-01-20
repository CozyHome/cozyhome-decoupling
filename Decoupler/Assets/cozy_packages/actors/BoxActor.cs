using com.cozyhome.Archetype;
using UnityEngine;

namespace com.cozyhome.Actors
{
    [RequireComponent(typeof(BoxCollider))] public class BoxActor : ActorHeader.Actor, ActorHeader.IActor
    {
        [System.NonSerialized] private ArchetypeHeader.BoxArchetype BoxArchetype;
        /* UnityEngine */
        void Start() 
        {
            BoxArchetype = new ArchetypeHeader.BoxArchetype(
                GetComponent<BoxCollider>()
            );
        }        
        public void OnActorBump(Vector3 _pos, Vector3 _velocity, in RaycastHit _hit) { }
        public void OnActorOverlap(Vector3 _normal, Collider _collider) { }

        public override ArchetypeHeader.Archetype GetArchetype()
        => BoxArchetype;
    }
}