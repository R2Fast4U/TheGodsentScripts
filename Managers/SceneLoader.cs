using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene transition service: fades a full-screen overlay to black, loads the target scene
/// asynchronously, then fades back in. Lives on the persistent GameManager object so the
/// fade can span the actual scene load. Named SceneLoader (not SceneManager) to avoid
/// colliding with UnityEngine.SceneManagement.SceneManager.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Tooltip("CanvasGroup of a full-screen black Image covering everything.")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeDuration = 1f;

    /// <summary>True when the overlay is fully opaque (already black).</summary>
    public bool IsBlack => fadeGroup != null && fadeGroup.alpha >= 0.999f;

    private void Awake()
    {
        if (fadeGroup != null)
            fadeGroup.blocksRaycasts = fadeGroup.alpha > 0f;
    }

    /// <summary>Fade out (if not already black), load the scene, then fade back in.</summary>
    public Coroutine LoadScene(string sceneName) => StartCoroutine(LoadRoutine(sceneName));

    private IEnumerator LoadRoutine(string sceneName)
    {
        if (!IsBlack)
            yield return FadeToBlack();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone)
            yield return null;

        yield return FadeFromBlack();
    }

    public IEnumerator FadeToBlack()
    {
        if (fadeGroup == null) yield break;
        fadeGroup.blocksRaycasts = true;
        yield return Fade(fadeGroup.alpha, 1f, fadeDuration);
    }

    public IEnumerator FadeFromBlack()
    {
        if (fadeGroup == null) yield break;
        yield return Fade(fadeGroup.alpha, 0f, fadeDuration);
        fadeGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Quick fade to black and back — a "blink". Runs <paramref name="onBlack"/> at the darkest
    /// point so an action (e.g. teleporting the player) happens unseen.
    /// </summary>
    public Coroutine Blink(Action onBlack, float toBlack = 0.08f, float hold = 0.05f, float fromBlack = 0.15f)
        => StartCoroutine(BlinkRoutine(onBlack, toBlack, hold, fromBlack));

    private IEnumerator BlinkRoutine(Action onBlack, float toBlack, float hold, float fromBlack)
    {
        if (fadeGroup == null)
        {
            onBlack?.Invoke();
            yield break;
        }

        fadeGroup.blocksRaycasts = true;
        yield return Fade(fadeGroup.alpha, 1f, toBlack);

        onBlack?.Invoke();
        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

        yield return Fade(1f, 0f, fromBlack);
        fadeGroup.blocksRaycasts = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            fadeGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        // Unscaled so transitions still run if the game is paused (timeScale = 0).
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        fadeGroup.alpha = to;
    }
}
