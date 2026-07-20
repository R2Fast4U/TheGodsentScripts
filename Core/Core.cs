using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Core : MonoBehaviour
{
    public Movement Movement { get; private set; }
    public CollisionSenses CollisionSenses { get; private set; }

    private void Awake()
    {
        Movement = GetComponentInChildren<Movement>();
        CollisionSenses = GetComponentInChildren<CollisionSenses>();

        if(!Movement)
        {
            Debug.LogError("Movement component not found in children of Core.");
        }
        if(!CollisionSenses)
        {
            Debug.LogError("CollisionSenses component not found in children of Core.");
        }
    }


}
