using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Vector3 cameraPosition;
    private Vector3 targetPosition;

    [SerializeField] Transform player;
    [SerializeField] float smoothTime = 0.1f;



    void Start()
    {
        targetPosition = transform.position;
        cameraPosition = targetPosition;
    }

    void LateUpdate()
    { 
        targetPosition.x = player.transform.position.x;
        //transform.position = Vector3.Lerp(transform.position, targetPosition, smoothTime);
        transform.position = targetPosition;
    }
}
