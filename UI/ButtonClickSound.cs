using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

/// <summary>
/// Plays a 2D one-shot sound when this Button is clicked. Add it to any Button and assign a clip.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [Tooltip("Optional. Route the click through this mixer group (e.g. your UI/SFX group).")]
    [SerializeField] private AudioMixerGroup outputMixerGroup;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Play);
    }

    private void OnDestroy()
    {
        var button = GetComponent<Button>();
        if (button != null) button.onClick.RemoveListener(Play);
    }

    private void Play()
    {
        if (clip == null) return;

        // Temp 2D source, so it's audible regardless of camera distance and survives this
        // object being destroyed (e.g. a menu closing).
        var go = new GameObject($"UIClick_{clip.name}");
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 0f; // 2D
        src.outputAudioMixerGroup = outputMixerGroup;
        src.Play();

        Destroy(go, clip.length);
    }
}
