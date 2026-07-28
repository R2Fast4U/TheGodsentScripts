using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
    [Tooltip("How long the fade-to-black takes on game over.")]
    [SerializeField] private float gameOverFadeDuration = 2f;
    [SerializeField] private GameObject gameOverScreen;
    [Tooltip("How long the game-over screen itself fades in after the black.")]
    [SerializeField] private float gameOverScreenFadeDuration = 0.5f;

    [Tooltip("Scene loaded when there is no saved checkpoint (e.g. your first level).")]
    [SerializeField] private string defaultRespawnScene = "";

    [Header("HUD")]
    [Tooltip("The HUD root (e.g. HUDCanvas). Toggled on/off per scene.")]
    [SerializeField] private GameObject hud;
    [Tooltip("Scenes where the HUD is hidden (menus, tutorials, cutscenes). All other scenes show it.")]
    [SerializeField] private List<string> hudHiddenScenes = new List<string>();

    [Header("Settings")]
    [Tooltip("Settings canvas (child of this manager) shown in any scene via OpenSettings().")]
    [SerializeField] private GameObject settingsCanvas;

    private int currentSlot;

    public PlayerStats Stats => stats;
    public SceneLoader SceneLoader => sceneLoader;

    /// <summary>The active save slot. Set it (e.g. from the main menu) before New Game / Continue.</summary>
    public int CurrentSlot => currentSlot;
    public void SetSlot(int slot) => currentSlot = Mathf.Max(0, slot);

    public bool HasSave => SaveSystem.HasSave(currentSlot);
    public bool SlotHasSave(int slot) => SaveSystem.HasSave(slot);

    /// <summary>Creation date of a slot's save, or null if the slot is empty / dateless.</summary>
    public DateTime? GetSlotCreatedDate(int slot)
    {
        var data = SaveSystem.Load(slot);
        if (data == null || data.createdTicks == 0) return null;
        return new DateTime(data.createdTicks);
    }

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

        // Hidden by default until the first scene load decides — avoids a HUD flash in a menu scene.
        if (hud != null) hud.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.SetActive(false);
    }

    private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    #region Save / Load / Flow
    public void SaveGame()
    {
        if (stats == null) return;
        SaveSystem.Save(stats.ToSaveData(), currentSlot);
    }

    public void LoadGame()
    {
        if (stats == null) return;
        stats.LoadFromSaveData(SaveSystem.Load(currentSlot));
    }

    /// <summary>Wipe the current slot's progress and start fresh from the given scene.</summary>
    public void NewGame(string firstScene)
    {
        SaveSystem.Delete(currentSlot);
        if (stats != null) stats.ResetToNewGame();
        SaveGame(); // occupy the slot immediately so it reads as "Continue" next time
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
            yield return sceneLoader.FadeToBlack(gameOverFadeDuration);

        if (gameOverScreen != null)
        {
            yield return FadeInScreen(gameOverScreen, gameOverScreenFadeDuration);
            SelectFirst(gameOverScreen); // so a controller can navigate Retry/Quit
        }
    }

    // Activates a screen and fades its CanvasGroup from 0 to 1.
    private static IEnumerator FadeInScreen(GameObject screen, float duration)
    {
        screen.SetActive(true);
        var cg = screen.GetComponent<CanvasGroup>();
        if (cg == null) cg = screen.AddComponent<CanvasGroup>();

        if (duration <= 0f)
        {
            cg.alpha = 1f;
            yield break;
        }

        cg.alpha = 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // unscaled so it runs even if timeScale is 0
            cg.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        cg.alpha = 1f;
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

    /// <summary>Quits the application. Hook to the game-over screen's (or any) Quit button.</summary>
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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

        UpdateHudForScene(scene.name);
    }
    #endregion

    #region HUD
    /// <summary>Shows the HUD unless the scene is in the hidden list.</summary>
    private void UpdateHudForScene(string sceneName)
    {
        if (hud == null) return;
        bool hide = hudHiddenScenes != null && hudHiddenScenes.Contains(sceneName);
        hud.SetActive(!hide);
    }

    /// <summary>Manually show/hide the HUD (e.g. an in-gameplay cutscene). A scene load re-applies
    /// the per-scene rule, so this is for temporary toggles within a scene.</summary>
    public void SetHudVisible(bool visible)
    {
        if (hud != null) hud.SetActive(visible);
    }
    #endregion

    #region Settings
    public void OpenSettings() => SetSettingsVisible(true);
    public void CloseSettings() => SetSettingsVisible(false);
    public void ToggleSettings()
    {
        if (settingsCanvas != null) SetSettingsVisible(!settingsCanvas.activeSelf);
    }

    public void SetSettingsVisible(bool visible)
    {
        if (settingsCanvas == null) return;
        settingsCanvas.SetActive(visible);
        if (visible) SelectFirst(settingsCanvas);
    }
    #endregion

    // Selects a panel's first interactable so gamepad/keyboard navigation works.
    private static void SelectFirst(GameObject root)
    {
        if (root == null || EventSystem.current == null) return;
        var selectable = root.GetComponentInChildren<Selectable>();
        if (selectable == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}
