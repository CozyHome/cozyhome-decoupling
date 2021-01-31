using com.cozyhome.Archetype;
using UnityEngine;

namespace com.cozyhome.Actors
{
    [RequireComponent(typeof(SphereCollider))] public class SphereActor : ActorHeader.Actor
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

        public override ArchetypeHeader.Archetype GetArchetype()
        => SphereArchetype;
    }
}