/*
using UnityEngine;


public class CameraAspectRatio : MonoBehaviour
{
    public float targetAspect = 16.0f / 9.0f; // Set your desired aspect ratio here

    void Start()
    {
        // Determine the game window's current aspect ratio
        float windowAspect = (float)Screen.width / (float)Screen.height;
        // Current viewport height should be scaled by this amount
        float scaleHeight = windowAspect / targetAspect;

        // If scaled height is less than current height, add letterbox
        if (scaleHeight < 1.0f)
        {
            Rect rect = Camera.main.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            Camera.main.rect = rect;
        }
        else // Add pillarbox
        {
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = Camera.main.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            Camera.main.rect = rect;
        }
    }
}
*/