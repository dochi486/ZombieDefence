using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 2f;
    public Vector2 offset;
    public float cameraDepth = -10;

    public float lerp = 0.01f;
    void Update()
    {
        var newPos = transform.position;
        newPos.x = Mathf.Lerp(newPos.x, target.transform.position.x, lerp);
        transform.position = newPos;
    }

    //void LateUpdate()
    //{
    //    if(target == null)
    //        return;
    //    Vector2 desPosition = Vector2.zero;
    //    desPosition.x = (target.position.x + offset.x);
    //    desPosition.y = (target.position.y + offset.y);
    //    transform.position = Vector2.Lerp(transform.position, desPosition, smoothSpeed * Time.deltaTime);
    //}
}
