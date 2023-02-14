using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float lerpSpeed = 0.3f;
    [SerializeField] Vector3 offset;

    Vector3 camVel = Vector3.zero;

    void Update() {
        transform.position = Vector3.SmoothDamp(transform.position, target.position + offset, ref camVel, lerpSpeed);
    }
}
