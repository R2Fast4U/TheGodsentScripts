using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent game coordinator: owns the flow (new game / continue / save / respawn) and the
/// death → game-over sequence. It is DontDestroyOnLoad, but deliberately holds NO persistent
/// data itself — all state lives in the <see cref="PlayerStats"/> asset and on disk via
/// <see cref="SaveSystem"/>, so a New Game is just "reset the asset + delete the save",
/// independent of this object's lifetime.
///
/// Self-bootstraps from a "GameManager" prefab in a Resources folder, so it exists no matter
/// which scene you press Play in (the main reason DontDestroyOnLoad is usually painful).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private PlayerStats stats;
    [SerializeField] private SceneLoader sceneLoader;

    [Header("Death / Game Over")]
    [Tooltip("Pause after death before the fade begins.")]
    [SerializeField] private float delayBeforeFade = 0.6f;
    [SerializeField] private GameObject gameOverScreen;

    [Tooltip("Scene loaded when there is no saved checkpoint (e.g. your first level).")]
    [SerializeField] private string defaultRespawnScene = "";

    public PlayerStats Stats => stats;
    public SceneLoader SceneLoader => sceneLoader;
    public bool HasSave => SaveSystem.HasSave();

    /// <summary>Raised the instant the player dies, before the fade/screen.</summary>
    public event Action OnPlayerDeath;

    private bool isGameOver;
    private bool pendingRespawn;

    /// <summary>True (once) if the next scene load is a respawn/continue, so the player should
    /// move to its checkpoint. Consumed by the Player on spawn.</summary>
    public bool ConsumePendingRespawn()
    {
        if (!pendingRespawn) return false;
        pendingRespawn = false;
        return true;
    }

    // Ensures the manager exists regardless of which scene Play starts in.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<GameManager>("GameManager");
        if (prefab != null)
            Instantiate(prefab);
        else
            Debug.LogWarning("[GameManager] No 'GameManager' prefab found in a Resources folder. " +
                             "Create Assets/Resources/GameManager.prefab.");
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sceneLoader == null) sceneLoader = GetComponentInChildren<SceneLoader>(includeInactive: true);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (stats != null) stats.EnsureInitialized();
    }

    private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    #region Save / Load / Flow
    public void SaveGame()
    {
        if (stats == null) return;
        SaveSystem.Save(stats.ToSaveData());
    }

    public void LoadGame()
    {
        if (stats == null) return;
        stats.LoadFromSaveData(SaveSystem.Load());
    }

    /// <summary>Wipe progress and start fresh from the given scene.</summary>
    public void NewGame(string firstScene)
    {
        SaveSystem.Delete();
        if (stats != null) stats.ResetToNewGame();
        isGameOver = false;
        LoadSceneInternal(firstScene);
    }

    /// <summary>Load the save file and jump to the saved checkpoint scene.</summary>
    public void Continue()
    {
        LoadGame();
        string scene = ResolveRespawnScene();
        if (!string.IsNullOrEmpty(scene))
        {
            pendingRespawn = true;
            LoadSceneInternal(scene);
        }
    }
    #endregion

    #region Death / Respawn
    /// <summary>Called by the Player on death (Combat.OnDeath).</summary>
    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        OnPlayerDeath?.Invoke();
        AudioManager.PlaySound(SoundType.DEATH);
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(delayBeforeFade);

        if (sceneLoader != null)
            yield return sceneLoader.FadeToBlack();

        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);
    }

    /// <summary>Hook to the game-over screen's "Retry" button.</summary>
    public void RespawnAtCheckpoint()
    {
        if (stats != null) stats.RefillHealth(); // else the reloaded player arrives dead

        isGameOver = false;
        pendingRespawn = true;
        Time.timeScale = 1f;
        LoadSceneInternal(ResolveRespawnScene());
    }

    private string ResolveRespawnScene()
    {
        if (stats != null && stats.HasCheckpoint && !string.IsNullOrEmpty(stats.LastCheckpointScene))
            return stats.LastCheckpointScene;
        if (!string.IsNullOrEmpty(defaultRespawnScene))
            return defaultRespawnScene;
        return SceneManager.GetActiveScene().name;
    }

    private void LoadSceneInternal(string scene)
    {
        if (string.IsNullOrEmpty(scene)) return;
        if (sceneLoader != null) sceneLoader.LoadScene(scene);
        else SceneManager.LoadScene(scene);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isGameOver = false;
        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);
    }
    #endregion
}
