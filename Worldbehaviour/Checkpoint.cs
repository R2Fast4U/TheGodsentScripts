using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A trigger volume that records the player's respawn point into <see cref="PlayerStats"/>
/// when the player walks through it. Put it on a GameObject with a trigger Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [Tooltip("Which layers count as the player.")]
    [SerializeField] private LayerMask playerLayer;
    [Tooltip("Optional. Where the player respawns. Defaults to this object's position.")]
    [SerializeField] private Transform respawnPoint;

    private void Reset()
    {
        // Convenience: make the collider a trigger when the component is first added.
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (stats == null) return;

        Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
        stats.SetCheckpoint(SceneManager.GetActiveScene().name, pos);

        // Persist progress to disk at each checkpoint.
        GameManager.Instance?.SaveGame();
    }
}
