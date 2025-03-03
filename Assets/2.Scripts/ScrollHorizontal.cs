using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollHorizontal : MonoBehaviour
{
    [SerializeField] private float speedX = -1;

    void Update()
    {
        transform.Translate(speedX * 1 * Time.deltaTime, 0, 0);
    }
}
