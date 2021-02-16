using com.cozyhome.Systems;
using com.cozyhome.Singleton;
using UnityEngine;

public class GCCollectorSystem : SingletonBehaviour<GCCollectorSystem>,
                                SystemsHeader.IUpdateSystem,
                                SystemsHeader.IDiscoverSystem
{
    [Header("GC Settings")]
    [SerializeField] short ExecutionIndex = 10;
    [SerializeField] int FrameCountModulo = 30;
    public void OnDiscover()
    {
        SystemsInjector.RegisterUpdateSystem(ExecutionIndex, this);
    }

    public void OnUpdate()
    {
        if(Time.frameCount % FrameCountModulo == 0)
            System.GC.Collect();
    }
}
