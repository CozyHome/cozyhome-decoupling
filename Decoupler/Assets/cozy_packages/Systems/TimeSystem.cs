using UnityEngine;

namespace com.cozyhome.SystemsHeader
{
    public class TimeSystem : MonoBehaviour,
    SystemsHeader.IDiscoverSystem,
    SystemsHeader.IFixedSystem,
    SystemsHeader.ILateUpdateSystem,
    SystemsHeader.IUpdateSystem
    {
        [SerializeField] private short _executionindex = 1;

        private float[] _times = new float[3] { -1F, -1F, -1F };
        private float[] _deltas = new float[3] { -1F, -1F, -1F };

        public void OnDiscover(in SystemsInjector _injector)
        {
            for (int i = 0; i < 3; i++)
                _times[i] = _deltas[i] = 0;

            _injector.RegisterUpdateSystem(_executionindex , this);
            _injector.RegisterFixedSystem(_executionindex , this);
            _injector.RegisterLateSystem(_executionindex , this);
        }

        public void OnFixedUpdate()
        {
            _times[1] = Time.fixedTime;
            _deltas[1] = Time.fixedDeltaTime;
        }

        public void OnLateUpdate()
        {
            _times[2] = Time.time;
            _deltas[2] = Time.deltaTime;
        }

        public void OnUpdate()
        {
            _times[0] = Time.time;
            _deltas[0] = Time.deltaTime;
        }
    }
}

