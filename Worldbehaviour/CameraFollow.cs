/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Vector3 baseOffset = new Vector3(0f, 0f, -3f);
    private Vector3 currentOffset;
    private Vector3 targetOffset;
    private float smoothTime = 0.25f;
    private float offsetSmoothTime = 0.2f;
    private Vector3 velocity = Vector3.zero;
    private Vector3 offsetVelocity = Vector3.zero;

    [SerializeField] private Transform target;

    void Awake()
    {
        currentOffset = baseOffset;
        targetOffset = baseOffset;
    }

    // Call this to adjust camera vertical offset (e.g. for look down/up)
    public void SetVerticalOffset(float yOffset)
    {
        targetOffset = baseOffset + new Vector3(0f, yOffset, 0f);
    }

    void FixedUpdate()
    {
        // Smoothly interpolate offset
        currentOffset = Vector3.SmoothDamp(currentOffset, targetOffset, ref offsetVelocity, offsetSmoothTime);

        Vector3 targetPosition = target.position + currentOffset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    // Optional: Reset camera to base offset
    public void ResetOffset()
    {
        targetOffset = baseOffset;
    }

}
*/