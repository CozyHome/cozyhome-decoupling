using UnityEngine;
using com.cozyhome.ConcurrentExecution;
using System;

public class ActorExecutioner : ConcurrentHeader.ExecutionMachine<ActorArgs>
{
    [SerializeField] private ActorArgs Middleman;

    private void Start()
    {
        this.Initialize(Middleman);
    }

    private void FixedUpdate()
    {
        this.Simulate(Middleman);
    }
}
