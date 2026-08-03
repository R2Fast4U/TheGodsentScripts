using UnityEngine;

/// <summary>
/// A named spawn location. Place these in a scene and give each an Id; a SceneDoor targeting
/// that Id will drop the player here on arrival.
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [Tooltip("Unique id within this scene, referenced by a SceneDoor's Target Spawn Id.")]
    [SerializeField] private string id;
    public string Id => id;

    /// <summary>Finds the spawn point with the given id in the active scene, or null.</summary>
    public static PlayerSpawnPoint Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var sp in FindObjectsOfType<PlayerSpawnPoint>())
            if (sp.id == id) return sp;
        return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
