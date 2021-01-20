using System.Collections;
using System.Collections.Generic;
using com.cozyhome.Systems;
using UnityEngine;

public class Racer : MonoBehaviour, IEntity 
{
    public float _speed = 0F;

    /* UnityEngine */ void Awake()
    {
        GameObject _primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _primitive.transform.SetParent(this.transform);
        _primitive.transform.localPosition = Vector3.zero;
    }

    public void OnInsertion() 
        => _speed = UnityEngine.Random.Range(1F, 3F);
    
    public void OnRemoval()
    {
        Destroy(gameObject);
    }
}
