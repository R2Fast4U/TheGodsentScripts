using UnityEngine;
using Cinemachine;

/// <summary>
/// A trigger volume that teleports whatever enters it (the player) to a destination transform,
/// hidden behind a quick camera "fade blink". Use it to fence off an area so crossing the
/// boundary snaps the player back — an "unrestricted area" feel.
///
/// Setup: put on a GameObject with a trigger Collider2D shaped like the boundary, assign a
/// Destination transform and the Player layer.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TeleportTrigger : MonoBehaviour
{
    [Header("Teleport")]
    [Tooltip("Where the entering object is sent.")]
    [SerializeField] private Transform destination;
    [Tooltip("Which layers get teleported (set to your Player layer).")]
    [SerializeField] private LayerMask targetLayer;
    [Tooltip("Keep momentum after teleporting instead of stopping the object.")]
    [SerializeField] private bool preserveVelocity = false;

    [Header("Fade Blink (seconds)")]
    [SerializeField] private float fadeToBlack = 0.08f;
    [SerializeField] private float holdBlack = 0.05f;
    [SerializeField] private float fadeBack = 0.15f;

    private void Reset()
    {
        // Convenience: make the collider a trigger when the component is first added.
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((targetLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (destination == null)
        {
            Debug.LogWarning($"TeleportTrigger '{name}': no destination assigned.");
            return;
        }

        Rigidbody2D body = other.attachedRigidbody;
        Transform toMove = body != null ? body.transform : other.transform;

        SceneLoader loader = GameManager.Instance != null ? GameManager.Instance.SceneLoader : null;
        if (loader != null)
            loader.Blink(() => Teleport(toMove, body), fadeToBlack, holdBlack, fadeBack);
        else
            Teleport(toMove, body); // no fade available — teleport instantly
    }

    private void Teleport(Transform toMove, Rigidbody2D body)
    {
        // Keep the player's current Z so they stay on the gameplay plane. Copying the
        // destination's Z can pull them off-plane, which reads as a camera zoom and breaks
        // the Z-based parallax/blur.
        Vector3 target = new Vector3(destination.position.x, destination.position.y, toMove.position.z);
        Vector3 delta = target - toMove.position;

        toMove.position = target;
        if (body != null && !preserveVelocity)
            body.velocity = Vector2.zero;

        // Tell Cinemachine the follow target warped, so the camera snaps by the same delta
        // instead of slowly damping across the whole teleport distance.
        var cam = CinemachineOffsetController.Instance != null
            ? CinemachineOffsetController.Instance.virtualCamera
            : null;
        if (cam != null)
        {
            Transform warpTarget = cam.Follow != null ? cam.Follow : toMove;
            cam.OnTargetObjectWarped(warpTarget, delta);
        }
    }
}
