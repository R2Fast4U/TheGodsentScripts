using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Interactable that loads another scene (with the SceneLoader's fade-out → load → fade-in) when
/// the player presses Interact while in range. Choose the target scene in the Inspector.
/// Put it on a GameObject with a trigger Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneDoor : MonoBehaviour
{
    [Tooltip("Scene to load. Must be added to File > Build Settings.")]
    [SerializeField] private string targetScene;
    [Tooltip("Id of the PlayerSpawnPoint to appear at in the target scene. Empty = scene's default position.")]
    [SerializeField] private string targetSpawnId;
    [Tooltip("Which layers count as the player.")]
    [SerializeField] private LayerMask playerLayer;
    [Tooltip("Optional prompt shown while the player is in range (e.g. 'Press Interact').")]
    [SerializeField] private GameObject interactPrompt;

    private Player player;
    private bool playerInRange;
    private bool loading;

    private void Reset()
    {
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
        if (loading || !playerInRange || player == null || player.InputHandler == null) return;
        if (!player.InputHandler.InteractInput) return;

        player.InputHandler.UseInteractInput();
        LoadTargetScene();
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning($"SceneDoor '{name}': no target scene set.");
            return;
        }

        loading = true;
        if (interactPrompt != null) interactPrompt.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.LoadSceneToSpawn(targetScene, targetSpawnId); // fade + spawn placement
        else
            SceneManager.LoadScene(targetScene); // fallback if no GameManager (no fade/placement)
    }
}
