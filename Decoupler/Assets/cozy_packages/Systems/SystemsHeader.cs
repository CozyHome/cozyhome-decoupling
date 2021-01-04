using System.Collections.Generic;
using UnityEngine;

namespace com.cozyhome.SystemsHeader
{
    public static class SystemsHeader
    {
        public interface IDiscoverSystem { void OnDiscover(in SystemsInjector _injector); }
        public interface IFixedSystem { void OnFixedUpdate(); }
        public interface IUpdateSystem { void OnUpdate(); }
        public interface ILateUpdateSystem { void OnLateUpdate(); }
    }

    [DefaultExecutionOrder(-500)]
    public partial class SystemsInjector : MonoBehaviour
    {
        [SerializeField] private bool SimulateFixed;
        [SerializeField] private bool SimulateUpdate;
        [SerializeField] private bool SimulateLate;

        private SortedList<short, SystemsHeader.IFixedSystem> _fixedsystems = null;
        private SortedList<short, SystemsHeader.IUpdateSystem> _updatesystems = null;
        private SortedList<short, SystemsHeader.ILateUpdateSystem> _latesystems = null;

        public void Awake()
        {
            _fixedsystems = new SortedList<short, SystemsHeader.IFixedSystem>();
            _updatesystems = new SortedList<short, SystemsHeader.IUpdateSystem>();
            _latesystems = new SortedList<short, SystemsHeader.ILateUpdateSystem>();

            // Get System Components
            SystemsHeader.IDiscoverSystem[] _discoveredsystems =
                this.GetComponents<SystemsHeader.IDiscoverSystem>();

            for (int i = _discoveredsystems.Length - 1; i >= 0; i--)
                _discoveredsystems[i].OnDiscover(this);

            _discoveredsystems = null;
        }

        public void Update()
        {
            if (!SimulateUpdate)
                return;
            else
            {
                for (int i=0; i < _updatesystems.Count; i++)
                    _updatesystems.Values[i].OnUpdate();
                return;
            }
        }

        public void LateUpdate()
        {
            if (!SimulateLate)
                return;
            else
            {
                for (int i = 0; i < _latesystems.Count; i++)
                    _latesystems.Values[i].OnLateUpdate();
                return;
            }
        }

        public void FixedUpdate()
        {
            if (!SimulateFixed)
                return;
            else
            {
                for (int i = 0; i < _fixedsystems.Count; i++)
                    _fixedsystems.Values[i].OnFixedUpdate();
                return;
            }
        }


        public void RegisterUpdateSystem(short _executionindex, SystemsHeader.IUpdateSystem _sys) => _updatesystems?.Add(_executionindex, _sys);
        public void RemoveUpdateSystem(short _executionindex) => _updatesystems?.RemoveAt(_executionindex);

        public void RegisterFixedSystem(short _executionindex, SystemsHeader.IFixedSystem _sys) => _fixedsystems?.Add(_executionindex, _sys);
        public void RemoveFixedSystem(short _executionindex) => _fixedsystems?.RemoveAt(_executionindex);

        public void RegisterLateSystem(short _executionindex, SystemsHeader.ILateUpdateSystem _sys) => _latesystems?.Add(_executionindex, _sys);
        public void RemoveLateSystem(short _executionindex, SystemsHeader.ILateUpdateSystem _sys) => _latesystems?.RemoveAt(_executionindex);
    }
}
