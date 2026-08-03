using System.Linq;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using System;
using Unity.VisualScripting;
using UnityEngine.Audio;
public enum SoundType
{
    ATTACK,
    JUMP,
    HURT,
    LEDGECLIMB,
    WARP,
    DEATH,
    LAND,
    WALK,
    WALLSLIDE,
    WALLJUMP,
    WALLLAND,
    LOOKUP,
    LOOKDOWN,
    CHECKPOINT,
    CHECKPOINTEXIT,
}
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [SerializeField] private SoundList[] soundList;
    [SerializeField] private int poolSize = 16;
    [SerializeField] AudioMixer audioMixer;
    
    private static AudioManager instance;
    private Queue<AudioSource> audioSourcePool;
    private List<AudioSource> activeSources;

    private void Awake()
    {
        // Single persistent instance: SFX are fired globally via the static PlaySound API, so we
        // keep one pool alive across scenes and drop any duplicates.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // DontDestroyOnLoad only works on root objects — detach if parented (e.g. under GameManager).
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        InitializePool();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        // Pool is initialized in Awake
    }

    private void InitializePool()
    {
        audioSourcePool = new Queue<AudioSource>(poolSize);
        activeSources = new List<AudioSource>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            // Route audio source through the mixer if one is assigned
            if (audioMixer != null)
            {
                source.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Master").Length > 0 
                    ? audioMixer.FindMatchingGroups("Master")[0] 
                    : null;
            }
            audioSourcePool.Enqueue(source);
        }
    }

    private AudioSource GetAudioSource()
    {
        if (audioSourcePool.Count > 0)
        {
            AudioSource source = audioSourcePool.Dequeue();
            source.enabled = true;
            // Ensure mixer group is set even when reusing pooled sources
            if (audioMixer != null)
            {
                source.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Master").Length > 0 
                    ? audioMixer.FindMatchingGroups("Master")[0] 
                    : null;
            }
            return source;
        }

        // If pool exhausted, create a new one
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        if (audioMixer != null)
        {
            newSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Master").Length > 0 
                ? audioMixer.FindMatchingGroups("Master")[0] 
                : null;
        }
        return newSource;
    }

    private void ReturnAudioSource(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.enabled = false;
        audioSourcePool.Enqueue(source);
    }

    public static void PlaySound(SoundType sound, float volume = 1)
    {
        if (instance == null) return;

        int soundIndex = (int)sound;
        if (instance.soundList == null || soundIndex < 0 || soundIndex >= instance.soundList.Length)
        {
            Debug.LogWarning($"AudioManager: SoundType {sound} is out of bounds of soundList (length {instance.soundList?.Length ?? 0}). Make sure it is set up in the Inspector.");
            return;
        }

        AudioClip[] clips = instance.soundList[soundIndex].Sounds;
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"AudioManager: No clips configured for SoundType {sound}. Please assign at least one AudioClip in the Inspector.");
            return;
        }

        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (randomClip == null)
        {
            Debug.LogWarning($"AudioManager: AudioClip is null for SoundType {sound} in soundList.");
            return;
        }

        float configuredVolume = instance.soundList[soundIndex].Volume;
        float finalVolume = volume * configuredVolume;

        AudioSource source = instance.GetAudioSource();
        source.clip = randomClip;
        source.volume = finalVolume;
        source.PlayOneShot(randomClip, finalVolume);

        instance.activeSources.Add(source);
        instance.StartCoroutine(instance.ReturnSourceAfterClip(source, randomClip.length));
    }

    private IEnumerator ReturnSourceAfterClip(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        activeSources.Remove(source);
        ReturnAudioSource(source);
    }

    // --- Added for Wall Slide Sound ---
    private static AudioSource wallSlideSource;

    public static void PlayWallSlide(float volume = 1f)
    {
        if (instance == null) return;

        // If already playing, do not start it again
        if (wallSlideSource != null && wallSlideSource.isPlaying) return;

        int slideIndex = (int)SoundType.WALLSLIDE;
        if (instance.soundList == null || slideIndex < 0 || slideIndex >= instance.soundList.Length)
        {
            Debug.LogWarning($"AudioManager: SoundType WALLSLIDE is out of bounds of soundList (length {instance.soundList?.Length ?? 0}). Make sure it is set up in the Inspector.");
            return;
        }

        AudioClip[] clips = instance.soundList[slideIndex].Sounds;
        if (clips == null || clips.Length == 0) return;
        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];

        float configuredVolume = instance.soundList[slideIndex].Volume;
        float finalVolume = volume * configuredVolume;

        wallSlideSource = instance.GetAudioSource();
        wallSlideSource.clip = randomClip;
        wallSlideSource.volume = finalVolume;
        wallSlideSource.pitch = 1f; // Reset default pitch
        wallSlideSource.loop = true; // Loop the slide audio while sliding
        wallSlideSource.Play();
    }

    public static void StopWallSlide()
    {
        if (instance == null) return;

        if (wallSlideSource != null)
        {
            wallSlideSource.loop = false; // Reset loop before returning to pool
            wallSlideSource.pitch = 1f; // Reset pitch before returning to pool
            instance.ReturnAudioSource(wallSlideSource);
            wallSlideSource = null;
        }
    }

    public static void SetWallSlidePitch(float pitch)
    {
        if (wallSlideSource != null)
        {
            wallSlideSource.pitch = pitch;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        string[] names = Enum.GetNames(typeof(SoundType));
        if (soundList == null || soundList.Length != names.Length)
        {
            Array.Resize(ref soundList, names.Length);
        }
        for (int i = 0; i < soundList.Length; i++)
        {
            if (soundList[i].name != names[i])
            {
                soundList[i].name = names[i];
            }
        }
    }
#endif

}
 
 [Serializable]
 public struct SoundList
{
    public AudioClip[] Sounds {get => sounds;}
    public float Volume { get => volume == 0f ? 1f : volume; }
    [HideInInspector] public string name;
    [SerializeField] private AudioClip[] sounds;
    [SerializeField, Range(0f, 1f)] private float volume;
}