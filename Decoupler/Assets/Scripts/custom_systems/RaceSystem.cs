using System.Collections;
using System.Collections.Generic;
using com.cozyhome.Systems;
using UnityEngine;

[DefaultExecutionOrder(-600)] public class RaceSystem : 
EntitySystem<Racer>, 
SystemsHeader.IFixedSystem,
SystemsHeader.IDiscoverSystem
{
    [SerializeField] short _executionindex = 2;

    public void OnDiscover()
    {
        List<Racer> _racers = new List<Racer>();

        for(int i = 0;i<10;i++)
            _racers.Add(GenerateRacerAt(1.5F * i, 0, 0, Quaternion.identity));
        
        RegisterEntities(_racers);

        SystemsInjector.RegisterFixedSystem(_executionindex, this);
    }     
    private Racer GenerateRacerAt(float x, float y, float z, Quaternion _worldorient0)
    {
        GameObject _racer = new GameObject("Racer");
        _racer.transform.SetPositionAndRotation(new Vector3(x,y,z), _worldorient0);
        return _racer.AddComponent<Racer>();
    }

    public void OnFixedUpdate()
    {
        float FDT = GlobalTime.FDT;

        ActUponAllEntities(
            (Racer _r) => 
            {   
                _r.transform.SetPositionAndRotation(
                    _r.transform.position + _r.transform.forward * FDT * _r._speed,
                    _r.transform.rotation * Quaternion.AngleAxis(FDT * _r._speed, Vector3.up)
                );
            }
        );
    }
}
