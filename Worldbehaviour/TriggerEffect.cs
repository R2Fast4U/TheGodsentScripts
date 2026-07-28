using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Plays a one-shot sound and a particle system when the player passes through a trigger
/// collider. Put it on a GameObject with a trigger Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TriggerEffect : MonoBehaviour
{
    [Tooltip("Which layers activate this (set to your Player layer).")]
    [SerializeField] private LayerMask targetLayer;

    [Header("Sound")]
    [SerializeField] private AudioClip sound;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [Tooltip("Optional. If set, the one-shot is routed through this mixer group.")]
    [SerializeField] private AudioMixerGroup outputMixerGroup;

    [Header("Particles")]
    [Tooltip("Particle system to play. Leave its 'Play On Awake' off so it only fires here.")]
    [SerializeField] private ParticleSystem particles;

    [Header("Behaviour")]
    [Tooltip("If true, this only ever fires once. If false, it fires every time the player enters.")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    private void Reset()
    {
        // Convenience: make the collider a trigger when the component is first added.
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((targetLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;

        if (particles != null)
            particles.Play();

        if (sound != null)
            PlayClip2D(sound, volume);
    }

    // Like AudioSource.PlayClipAtPoint, but the temp source is 2D (spatialBlend = 0), so it's
    // always audible regardless of how far the AudioListener/camera is on the Z axis.
    private void PlayClip2D(AudioClip clip, float vol)
    {
        var go = new GameObject($"OneShot_{clip.name}");
        go.transform.position = transform.position;

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = vol;
        src.spatialBlend = 0f; // 2D
        src.outputAudioMixerGroup = outputMixerGroup;
        src.Play();

        Destroy(go, clip.length);
    }
}
