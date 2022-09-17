using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Vector3 velocity;
    private Vector3 targetPosition;

    [SerializeField] Transform player;
    [SerializeField] float smoothTime = 0.1f;



    void Start()
    {
        targetPosition = transform.position;
    }

    void FixedUpdate()
    { 
        targetPosition.x = player.transform.position.x;
        Vector3 cameraPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        transform.position = cameraPosition;
    }
}
