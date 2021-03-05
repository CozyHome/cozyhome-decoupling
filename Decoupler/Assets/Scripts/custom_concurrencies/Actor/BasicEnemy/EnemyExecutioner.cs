using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.cozyhome.ConcurrentExecution;

public class EnemyExecutioner : ConcurrentHeader.ExecutionMachine<EnemyArgs>
{
    [SerializeField] private EnemyArgs Middleman;

    void Start() 
    {
        this.Initialize(Middleman);
    }

    private void FixedUpdate() 
    {
        this.Simulate(Middleman);
    }
}