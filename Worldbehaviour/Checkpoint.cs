using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// An interactable checkpoint. While the player is inside its trigger, pressing Interact records
/// the respawn point into <see cref="PlayerStats"/>, saves to disk, and plays the checkpoint
/// animation. Put it on a GameObject with a trigger Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [Tooltip("Which layers count as the player.")]
    [SerializeField] private LayerMask playerLayer;
    [Tooltip("Optional. Where the player respawns. Defaults to this object's position.")]
    [SerializeField] private Transform respawnPoint;
    [Tooltip("Optional prompt shown while the player is in range (e.g. 'Press Interact').")]
    [SerializeField] private GameObject interactPrompt;
    [Tooltip("Effects (on this or other objects) fired when the checkpoint is activated — e.g. a statue lighting up.")]
    [SerializeField] private TriggerEffect[] effectsOnActivate;

    private Player player;
    private bool playerInRange;

    private void Reset()
    {
        // Convenience: make the collider a trigger when the component is first added.
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        player = other.GetComponentInParent<Player>();
        playerInRange = player != null;
        if (playerInRange && interactPrompt != null) interactPrompt.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        playerInRange = false;
        player = null;
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange || player == null || player.InputHandler == null) return;
        if (!player.InputHandler.InteractInput) return;

        player.InputHandler.UseInteractInput();
        Activate();
    }

    private void Activate()
    {
        if (stats != null)
        {
            Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
            stats.SetCheckpoint(SceneManager.GetActiveScene().name, pos);
        }

        // Persist progress to disk.
        GameManager.Instance?.SaveGame();

        // Play the checkpoint animation on the player.
        player.EnterCheckpoint();

        // Fire any world effects (animations/particles/sound on other objects).
        if (effectsOnActivate != null)
            foreach (var effect in effectsOnActivate)
                if (effect != null) effect.Play();
    }
}
