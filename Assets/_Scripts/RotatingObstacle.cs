using System;
using PurrNet;
using UnityEngine;

public class RotatingObstacle : NetworkBehaviour
{
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 90f, 0); 

    void FixedUpdate()
    {

        if (isServer)
        {
            Quaternion deltaRotation = Quaternion.Euler(rotationSpeed * Time.fixedDeltaTime);
            GetComponent<Rigidbody>().MoveRotation(GetComponent<Rigidbody>().rotation * deltaRotation);
        }
    }
}
