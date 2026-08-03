using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TMPro;

/// <summary>
/// Main menu controller. Phases fade in/out (CanvasGroup):
///  1. Studio splash / credits (still image) for a fixed duration.
///  2. "Press any button to start" prompt.
///  3. Main buttons panel (Play / Settings / Quit). Play swaps to the save-slots panel
///     (four slot buttons + Back); Back returns.
/// Slot buttons show each save's creation date so slots are distinguishable.
/// Save/scene/settings work is delegated to the persistent GameManager.
/// </summary>
public class MainMenu : MonoBehaviour
{
    private const int SlotCount = 4;

    [Header("Fade")]
    [Tooltip("Fade time for each phase in/out (seconds).")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Phase 1 — Studio Splash")]
    [SerializeField] private GameObject studioSplashScreen;
    [SerializeField] private float splashDuration = 3f;

    [Header("Phase 2 — Press Any Button")]
    [SerializeField] private GameObject pressAnyKeyScreen;

    [Header("Phase 3 — Main Buttons")]
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Phase 3 — Save Slots")]
    [SerializeField] private GameObject saveSlotsPanel;
    [Tooltip("The four save-slot buttons, in slot order (1..4).")]
    [SerializeField] private Button[] saveSlotButtons = new Button[SlotCount];
    [Tooltip("Labels on the slot buttons (same order), showing the save date or 'Empty'.")]
    [SerializeField] private TMP_Text[] saveSlotLabels;
    [SerializeField] private Button backButton;
    [Tooltip("Scene loaded when starting a NEW game on an empty slot.")]
    [SerializeField] private string firstLevelScene = "Level1";
    [Tooltip("How the save date is formatted on slot labels.")]
    [SerializeField] private string dateFormat = "yyyy-MM-dd HH:mm";

    private bool awaitingAnyButton;
    private GameObject currentPhase;
    private Coroutine transition;

    private void Awake()
    {
        // Start everything hidden; IntroFlow fades the splash in.
        foreach (var go in new[] { studioSplashScreen, pressAnyKeyScreen, mainButtonsPanel, saveSlotsPanel })
            if (go != null) go.SetActive(false);
        currentPhase = null;
    }

    private void OnEnable()
    {
        if (playButton != null) playButton.onClick.AddListener(OnPlay);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        if (backButton != null) backButton.onClick.AddListener(OnBack);

        for (int i = 0; i < saveSlotButtons.Length; i++)
        {
            if (saveSlotButtons[i] == null) continue;
            int slot = i; // capture per-iteration for the closure
            saveSlotButtons[i].onClick.AddListener(() => PlaySlot(slot));
        }
    }

    private void OnDisable()
    {
        if (playButton != null) playButton.onClick.RemoveListener(OnPlay);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        if (backButton != null) backButton.onClick.RemoveListener(OnBack);

        foreach (var btn in saveSlotButtons)
            if (btn != null) btn.onClick.RemoveAllListeners(); // slot buttons are wired in code only
    }

    private void Start()
    {
        StartCoroutine(IntroFlow());
    }

    private IEnumerator IntroFlow()
    {
        // Phase 1: studio splash / credits (fades in).
        yield return TransitionRoutine(studioSplashScreen);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, splashDuration));

        // Phase 2: crossfade to the prompt, then wait for any button.
        yield return TransitionRoutine(pressAnyKeyScreen);
        awaitingAnyButton = true;
    }

    private void Update()
    {
        if (awaitingAnyButton && AnyButtonPressedThisFrame())
        {
            awaitingAnyButton = false;
            TransitionTo(mainButtonsPanel);
        }
    }

    #region Panel switching (fades)
    private void OnPlay()
    {
        RefreshSlots();
        TransitionTo(saveSlotsPanel);
    }

    private void OnBack() => TransitionTo(mainButtonsPanel);

    private void TransitionTo(GameObject target)
    {
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(TransitionRoutine(target));
    }

    // Fades the current phase out, then the target in.
    private IEnumerator TransitionRoutine(GameObject target)
    {
        if (currentPhase == target) yield break;

        if (currentPhase != null)
        {
            yield return Fade(currentPhase, GetGroup(currentPhase).alpha, 0f);
            currentPhase.SetActive(false);
        }

        currentPhase = target;
        if (target != null)
        {
            target.SetActive(true);
            GetGroup(target).alpha = 0f;
            yield return Fade(target, 0f, 1f);
            SelectFirst(target); // so a controller/keyboard has something to navigate from
        }
    }

    // Selects the panel's first interactable so gamepad/keyboard navigation works.
    private static void SelectFirst(GameObject panel)
    {
        if (panel == null || EventSystem.current == null) return;
        var selectable = panel.GetComponentInChildren<Selectable>();
        if (selectable == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }

    private IEnumerator Fade(GameObject go, float from, float to)
    {
        var cg = GetGroup(go);
        cg.blocksRaycasts = to > 0.5f; // don't intercept clicks while invisible/fading out
        cg.interactable = to > 0.5f;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        cg.alpha = to;
    }

    private static CanvasGroup GetGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }
    #endregion

    #region Slots / actions
    /// <summary>Updates each slot button's label to show its save date, or "Empty".</summary>
    private void RefreshSlots()
    {
        if (saveSlotLabels == null) return;
        for (int i = 0; i < SlotCount && i < saveSlotLabels.Length; i++)
        {
            if (saveSlotLabels[i] == null) continue;

            if (GameManager.Instance != null && GameManager.Instance.SlotHasSave(i))
            {
                var date = GameManager.Instance.GetSlotCreatedDate(i);
                string when = date.HasValue ? date.Value.ToString(dateFormat) : "Continue";
                saveSlotLabels[i].text = $"Slot {i + 1}\n{when}";
            }
            else
            {
                saveSlotLabels[i].text = $"Slot {i + 1}\nEmpty";
            }
        }
    }

    private void PlaySlot(int slot)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MainMenu: no GameManager in scene (is the prefab in Resources?).");
            return;
        }

        GameManager.Instance.SetSlot(slot);
        if (GameManager.Instance.SlotHasSave(slot))
            GameManager.Instance.Continue();      // load the save and jump to its checkpoint scene
        else
            GameManager.Instance.NewGame(firstLevelScene);
    }

    private void OnSettings() => GameManager.Instance?.OpenSettings();

    private void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion

    // Any key, mouse button, or gamepad button pressed this frame (new Input System).
    private static bool AnyButtonPressedThisFrame()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.anyKey.wasPressedThisFrame) return true;

        var mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame ||
                              mouse.rightButton.wasPressedThisFrame ||
                              mouse.middleButton.wasPressedThisFrame)) return true;

        var pad = Gamepad.current;
        if (pad != null)
        {
            foreach (var control in pad.allControls)
                if (control is ButtonControl button && !button.synthetic && button.wasPressedThisFrame)
                    return true;
        }
        return false;
    }
}
