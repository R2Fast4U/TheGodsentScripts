using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Plays a one-shot sound, a particle system, and/or an animation when the player passes through
/// a trigger collider. Put it on a GameObject with a trigger Collider2D.
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

    [Header("Animation")]
    [Tooltip("Optional Animator to drive when triggered.")]
    [SerializeField] private Animator animator;
    [Tooltip("Bool parameter set true on trigger, then false after the duration (leave empty to skip).")]
    [SerializeField] private string animationBool = "";
    [Tooltip("Seconds the bool stays true before going false. 0 = auto-detect the current clip's length.")]
    [SerializeField] private float animationDuration = 0f;
    [SerializeField] private int animatorLayer = 0;

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
        Play();
    }

    /// <summary>Fires the effect (particles + animation + sound) immediately. Callable externally
    /// (e.g. by a Checkpoint on interact), bypassing the layer/once gating.</summary>
    public void Play()
    {
        if (particles != null)
            particles.Play();

        if (animator != null && !string.IsNullOrEmpty(animationBool))
        {
            animator.SetBool(animationBool, true);
            StartCoroutine(ClearAnimationBoolRoutine());
        }

        if (sound != null)
            PlayClip2D(sound, volume);
    }

    // Holds the bool true for the duration, then sets it false so the Animator transitions back.
    private IEnumerator ClearAnimationBoolRoutine()
    {
        // Let the animator enter the target state so auto-detect reads the right clip length.
        yield return null;
        yield return null;

        float wait = animationDuration > 0f
            ? animationDuration
            : animator.GetCurrentAnimatorStateInfo(animatorLayer).length;

        yield return new WaitForSeconds(wait);

        animator.SetBool(animationBool, false);
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
