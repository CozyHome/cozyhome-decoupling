using UnityEngine;

namespace com.cozyhome.ConcurrentExecution
{
    public class Middleman
    {
        // Put data and funcs in here
    }

    public class DebugExecutionMachine : ConcurrentHeader.ExecutionMachine<Middleman>
    {
        void Start()
        {
            this.Initialize();
        }
    }
}
