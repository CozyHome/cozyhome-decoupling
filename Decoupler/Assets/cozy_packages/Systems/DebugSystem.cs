namespace com.cozyhome.SystemsHeader
{
    public class DebugSystem : UnityEngine.MonoBehaviour,
        SystemsHeader.IDiscoverSystem,
        SystemsHeader.IFixedSystem,
        SystemsHeader.ILateUpdateSystem,
        SystemsHeader.IUpdateSystem
    {
        [UnityEngine.SerializeField] short _executionindex = 0;
        public void OnDiscover(in SystemsInjector _injector)
        {
            _injector.RegisterUpdateSystem(_executionindex,this);
            _injector.RegisterFixedSystem(_executionindex, this);
            _injector.RegisterLateSystem(_executionindex, this);
        }

        public void OnFixedUpdate()
        { }
        public void OnLateUpdate()
        { }
        public void OnUpdate()
        { }
    }
}