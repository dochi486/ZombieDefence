using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public float speed = 2f;
    public Vector2[] pathPoints;
    private int targetIndex = 0;

    void Update()
    {
        if(pathPoints.Length == 0)
            return;
        transform.position = Vector2.MoveTowards(transform.position, pathPoints[targetIndex], speed * Time.deltaTime);

        if(Vector2.Distance(transform.position, pathPoints[targetIndex]) < 0.1f)
        {
            targetIndex = (targetIndex + 1) % pathPoints.Length;
        }
    }
}
