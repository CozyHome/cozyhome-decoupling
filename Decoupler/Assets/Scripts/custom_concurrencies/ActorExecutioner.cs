using UnityEngine;
using com.cozyhome.ConcurrentExecution;

public class ActorExecutioner : ConcurrentHeader.ExecutionMachine<ActorArgs>
{
    [SerializeField] private ActorArgs Middleman;

    private void Start()
    {
        this.Initialize();
    }

    private void FixedUpdate()
    {
        this.Simulate(Middleman);
    }
}
