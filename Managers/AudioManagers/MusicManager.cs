using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Mixer & Channels")]
    [SerializeField] AudioMixer _audioMixer;

    [Header("Playlists")]
    [SerializeField] List<AudioClip> _peacefulMusic;
    [SerializeField] List<AudioClip> _combatMusic;
    [SerializeField] List<AudioClip> _tempList;

    [Header("Ambience")]
    [SerializeField] List<AudioClip> _ambienceMusic;

    [Header("Settings")]
    [SerializeField] bool _playOnStart = true;
    [SerializeField] bool _playAmbienceOnStart = true;
    [SerializeField] bool _shufflePlaylist = false;
    [SerializeField, Range(0f, 1f)] private float _baseVolume = 1.0f;
    [SerializeField, Range(0f, 1f)] private float _combatVolume = 1.0f;
    [SerializeField, Range(0f, 1f)] private float _ambienceVolume = 1.0f;
    [SerializeField, Range(0f, 1f)] private float _baseVolumeInCombat = 0.15f;

    AudioMixerGroup _peacefulAudioMixerGroup, _combatAudioMixerGroup, _ambienceAudioMixerGroup;
    AudioSource _peacefulAudioSource, _combatAudioSource, _ambienceAudioSource, _audioSource;
    AudioMixerSnapshot _peacefulSnapshot, _combatSnapshot;
    Queue<AudioClip> _peacefultrackQueue, _combattrackQueue, _ambiencetrackQueue;
    List<AudioClip> _currentPlayList;

    private bool _isInCombat = false;
    private bool _isAutoPlaylistEnabled = true;
    private bool _isPlaylistActive = false;
    private bool _isAmbiencePlaylistActive = false;

    private Coroutine _playlistCoroutine;
    private Coroutine _baseFadeCoroutine;
    private Coroutine _combatFadeCoroutine;
    private Coroutine _baseCombatFadeCoroutine;
    private Coroutine _ambiencePlaylistCoroutine;
    private Coroutine _ambienceFadeCoroutine;

    private Dictionary<string, AudioSource> _activeOverlays = new Dictionary<string, AudioSource>();
    private Dictionary<string, Coroutine> _overlayFadeCoroutines = new Dictionary<string, Coroutine>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Prioritize the instance that actually has configuration/clips assigned
            bool existingHasClips = Instance._peacefulMusic != null && Instance._peacefulMusic.Count > 0;
            bool thisHasClips = _peacefulMusic != null && _peacefulMusic.Count > 0;

            if (!existingHasClips && thisHasClips)
            {
                Instance.DestroyDuplicate();
                Instance = this;
            }
            else
            {
                DestroyDuplicate();
                return;
            }
        }
        else
        {
            Instance = this;
        }

        // To make DontDestroyOnLoad safe for components on child GameObjects
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            transform.parent = null;
            DontDestroyOnLoad(gameObject);
        }

        ConfigureAudioSources();
    }

    private void DestroyDuplicate()
    {
        // If this script is attached to a shared GameObject (like a background quad or player),
        // only destroy this component instance to prevent losing visual elements/colliders.
        if (GetComponent<Renderer>() != null || GetComponent<Collider>() != null || transform.childCount > 0 || transform.parent != null)
        {
            Destroy(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (_playOnStart && _peacefulMusic != null && _peacefulMusic.Count > 0)
        {
            StartPlaylist();
        }
        if (_playAmbienceOnStart && _ambienceMusic != null && _ambienceMusic.Count > 0)
        {
            StartAmbiencePlaylist();
        }
    }

    void ConfigureAudioSources()
    {
        if (_audioMixer != null)
        {
            var peacefulGroups = _audioMixer.FindMatchingGroups("Peaceful");
            if (peacefulGroups.Length > 0) _peacefulAudioMixerGroup = peacefulGroups[0];

            var combatGroups = _audioMixer.FindMatchingGroups("Combat");
            if (combatGroups.Length > 0) _combatAudioMixerGroup = combatGroups[0];

            var ambienceGroups = _audioMixer.FindMatchingGroups("Ambience");
            if (ambienceGroups.Length > 0) _ambienceAudioMixerGroup = ambienceGroups[0];
            else
            {
                var ambientGroups = _audioMixer.FindMatchingGroups("Ambient");
                if (ambientGroups.Length > 0) _ambienceAudioMixerGroup = ambientGroups[0];
            }

            _peacefulSnapshot = _audioMixer.FindSnapshot("Peaceful");
            _combatSnapshot = _audioMixer.FindSnapshot("Combat");
        }

        _peacefulAudioSource = gameObject.AddComponent<AudioSource>();
        if (_peacefulAudioMixerGroup != null) _peacefulAudioSource.outputAudioMixerGroup = _peacefulAudioMixerGroup;
        _peacefulAudioSource.loop = false;
        _peacefulAudioSource.playOnAwake = false;

        _combatAudioSource = gameObject.AddComponent<AudioSource>();
        if (_combatAudioMixerGroup != null) _combatAudioSource.outputAudioMixerGroup = _combatAudioMixerGroup;
        _combatAudioSource.loop = false;
        _combatAudioSource.playOnAwake = false;

        _ambienceAudioSource = gameObject.AddComponent<AudioSource>();
        if (_ambienceAudioMixerGroup != null)
        {
            _ambienceAudioSource.outputAudioMixerGroup = _ambienceAudioMixerGroup;
        }
        else if (_peacefulAudioMixerGroup != null)
        {
            _ambienceAudioSource.outputAudioMixerGroup = _peacefulAudioMixerGroup;
        }
        _ambienceAudioSource.loop = false;
        _ambienceAudioSource.playOnAwake = false;
        _ambienceAudioSource.volume = _ambienceVolume;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        _peacefultrackQueue = new Queue<AudioClip>(_peacefulMusic);
        _combattrackQueue = new Queue<AudioClip>(_combatMusic);
        _ambiencetrackQueue = new Queue<AudioClip>(_ambienceMusic);
    }

    public void StartPlaylist()
    {
        if (_isPlaylistActive) return;
        _isPlaylistActive = true;
        _isAutoPlaylistEnabled = true;
        _playlistCoroutine = StartCoroutine(PlaylistLoop());
    }

    public void StopPlaylist(float fadeDuration = 1.0f)
    {
        _isPlaylistActive = false;
        _isAutoPlaylistEnabled = false;
        if (_playlistCoroutine != null)
        {
            StopCoroutine(_playlistCoroutine);
            _playlistCoroutine = null;
        }
        StartCoroutine(FadeSourceVolume(_peacefulAudioSource, 0f, fadeDuration, true));
    }

    private IEnumerator PlaylistLoop()
    {
        while (_isPlaylistActive && _isAutoPlaylistEnabled)
        {
            if (_peacefulMusic == null || _peacefulMusic.Count == 0)
            {
                yield return new WaitForSeconds(1.0f);
                continue;
            }

            if (_peacefultrackQueue == null || _peacefultrackQueue.Count == 0)
            {
                List<AudioClip> tempList = new List<AudioClip>(_peacefulMusic);
                if (_shufflePlaylist)
                {
                    for (int i = 0; i < tempList.Count; i++)
                    {
                        AudioClip temp = tempList[i];
                        int randomIndex = Random.Range(i, tempList.Count);
                        tempList[i] = tempList[randomIndex];
                        tempList[randomIndex] = temp;
                    }
                }
                _peacefultrackQueue = new Queue<AudioClip>(tempList);
            }

            if (_peacefultrackQueue.Count > 0)
            {
                AudioClip nextClip = _peacefultrackQueue.Dequeue();
                if (nextClip != null)
                {
                    yield return StartCoroutine(TransitionBaseTrackCoroutine(nextClip, 1.5f, false));

                    while (_peacefulAudioSource.isPlaying && _peacefulAudioSource.clip == nextClip)
                    {
                        yield return new WaitForSeconds(1.0f);
                    }
                }
                else
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }
        }
    }

    public void PlayBaseTrack(AudioClip clip, float fadeDuration = 1.0f, bool loop = true)
    {
        _isAutoPlaylistEnabled = false;
        if (_playlistCoroutine != null)
        {
            StopCoroutine(_playlistCoroutine);
            _playlistCoroutine = null;
        }

        if (_baseFadeCoroutine != null) StopCoroutine(_baseFadeCoroutine);
        _baseFadeCoroutine = StartCoroutine(TransitionBaseTrackCoroutine(clip, fadeDuration, loop));
    }

    private IEnumerator TransitionBaseTrackCoroutine(AudioClip clip, float fadeDuration, bool loop)
    {
        if (_peacefulAudioSource.isPlaying && _peacefulAudioSource.volume > 0f)
        {
            float startVol = _peacefulAudioSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _peacefulAudioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                yield return null;
            }
            _peacefulAudioSource.volume = 0f;
            _peacefulAudioSource.Stop();
        }

        _peacefulAudioSource.clip = clip;
        _peacefulAudioSource.loop = loop;
        _peacefulAudioSource.volume = 0f;
        _peacefulAudioSource.Play();

        float elapsedIn = 0f;
        float targetVol = _isInCombat ? _baseVolumeInCombat : _baseVolume;
        while (elapsedIn < fadeDuration)
        {
            elapsedIn += Time.deltaTime;
            _peacefulAudioSource.volume = Mathf.Lerp(0f, targetVol, elapsedIn / fadeDuration);
            yield return null;
        }
        _peacefulAudioSource.volume = targetVol;
    }

    public void RestartCurrentTrack(float fadeDuration = 1.0f)
    {
        if (_peacefulAudioSource.clip == null) return;
        if (_baseFadeCoroutine != null) StopCoroutine(_baseFadeCoroutine);
        _baseFadeCoroutine = StartCoroutine(RestartTrackCoroutine(fadeDuration));
    }

    private IEnumerator RestartTrackCoroutine(float fadeDuration)
    {
        if (_peacefulAudioSource.isPlaying)
        {
            float startVol = _peacefulAudioSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _peacefulAudioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                yield return null;
            }
            _peacefulAudioSource.volume = 0f;
            _peacefulAudioSource.Stop();
        }

        _peacefulAudioSource.time = 0f;
        _peacefulAudioSource.Play();

        float elapsedIn = 0f;
        float targetVol = _isInCombat ? _baseVolumeInCombat : _baseVolume;
        while (elapsedIn < fadeDuration)
        {
            elapsedIn += Time.deltaTime;
            _peacefulAudioSource.volume = Mathf.Lerp(0f, targetVol, elapsedIn / fadeDuration);
            yield return null;
        }
        _peacefulAudioSource.volume = targetVol;
    }

    public void StartCombat(float fadeDuration = 1.0f, float combatVolume = -1.0f)
    {
        if (_combatMusic == null || _combatMusic.Count == 0)
        {
            Debug.LogWarning("MusicManager: StartCombat called but _combatMusic list is empty in the Inspector!");
            return;
        }
        StartCombat(_combatMusic[0], fadeDuration, combatVolume);
    }

    public void StartCombat(AudioClip combatClip, float fadeDuration = 1.0f, float combatVolume = -1.0f)
    {
        if (_isInCombat && _combatAudioSource.clip == combatClip && _combatAudioSource.isPlaying) return;
        _isInCombat = true;

        if (_combatSnapshot != null)
        {
            _combatSnapshot.TransitionTo(fadeDuration);
        }

        if (_baseCombatFadeCoroutine != null) StopCoroutine(_baseCombatFadeCoroutine);
        _baseCombatFadeCoroutine = StartCoroutine(FadeSourceVolume(_peacefulAudioSource, _baseVolumeInCombat, fadeDuration, false));

        if (_combatFadeCoroutine != null) StopCoroutine(_combatFadeCoroutine);
        _combatAudioSource.clip = combatClip;
        _combatAudioSource.loop = true;
        if (!_combatAudioSource.isPlaying)
        {
            _combatAudioSource.volume = 0f;
            _combatAudioSource.Play();
        }
        float finalCombatVolume = combatVolume < 0f ? _combatVolume : combatVolume;
        _combatFadeCoroutine = StartCoroutine(FadeSourceVolume(_combatAudioSource, finalCombatVolume, fadeDuration, false));
    }

    public void StopCombat(float fadeDuration = 1.0f)
    {
        if (!_isInCombat) return;
        _isInCombat = false;

        if (_peacefulSnapshot != null)
        {
            _peacefulSnapshot.TransitionTo(fadeDuration);
        }

        if (_baseCombatFadeCoroutine != null) StopCoroutine(_baseCombatFadeCoroutine);
        _baseCombatFadeCoroutine = StartCoroutine(FadeSourceVolume(_peacefulAudioSource, _baseVolume, fadeDuration, false));

        if (_combatFadeCoroutine != null) StopCoroutine(_combatFadeCoroutine);
        _combatFadeCoroutine = StartCoroutine(FadeSourceVolume(_combatAudioSource, 0f, fadeDuration, true));
    }

    public void PlayOverlay(string key, AudioClip clip, bool loop = true, float fadeDuration = 1.0f, float targetVolume = 1.0f)
    {
        if (clip == null || string.IsNullOrEmpty(key)) return;

        if (_activeOverlays.TryGetValue(key, out AudioSource source))
        {
            if (source.clip == clip && source.isPlaying) return;

            if (_overlayFadeCoroutines.TryGetValue(key, out Coroutine existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
            }
            _overlayFadeCoroutines[key] = StartCoroutine(TransitionOverlayCoroutine(key, source, clip, loop, fadeDuration, targetVolume));
        }
        else
        {
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            if (_audioMixer != null)
            {
                var masterGroup = _audioMixer.FindMatchingGroups("Master");
                newSource.outputAudioMixerGroup = masterGroup.Length > 0 ? masterGroup[0] : null;
            }
            newSource.loop = loop;
            newSource.volume = 0f;
            newSource.playOnAwake = false;

            _activeOverlays[key] = newSource;
            _overlayFadeCoroutines[key] = StartCoroutine(TransitionOverlayCoroutine(key, newSource, clip, loop, fadeDuration, targetVolume));
        }
    }

    public void StopOverlay(string key, float fadeDuration = 1.0f)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (_activeOverlays.TryGetValue(key, out AudioSource source))
        {
            if (_overlayFadeCoroutines.TryGetValue(key, out Coroutine existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
            }
            _overlayFadeCoroutines[key] = StartCoroutine(FadeAndDestroyOverlayCoroutine(key, source, fadeDuration));
        }
    }

    private IEnumerator TransitionOverlayCoroutine(string key, AudioSource source, AudioClip clip, bool loop, float fadeDuration, float targetVolume)
    {
        if (source.isPlaying && source.volume > 0f)
        {
            float startVol = source.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                yield return null;
            }
            source.volume = 0f;
            source.Stop();
        }

        source.clip = clip;
        source.loop = loop;
        source.Play();

        float elapsedIn = 0f;
        while (elapsedIn < fadeDuration)
        {
            elapsedIn += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, elapsedIn / fadeDuration);
            yield return null;
        }
        source.volume = targetVolume;
        _overlayFadeCoroutines.Remove(key);
    }

    private IEnumerator FadeAndDestroyOverlayCoroutine(string key, AudioSource source, float fadeDuration)
    {
        if (source.isPlaying && source.volume > 0f)
        {
            float startVol = source.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                yield return null;
            }
        }
        source.Stop();
        Destroy(source);
        _activeOverlays.Remove(key);
        _overlayFadeCoroutines.Remove(key);
    }

    private IEnumerator FadeSourceVolume(AudioSource source, float targetVolume, float duration, bool stopOnZero)
    {
        float startVol = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, targetVolume, elapsed / duration);
            yield return null;
        }
        source.volume = targetVolume;
        if (stopOnZero && targetVolume <= 0.001f)
        {
            source.Stop();
        }
    }

    public void StartAmbiencePlaylist()
    {
        if (_isAmbiencePlaylistActive) return;
        _isAmbiencePlaylistActive = true;
        _ambiencePlaylistCoroutine = StartCoroutine(AmbiencePlaylistLoop());
    }

    public void StopAmbiencePlaylist(float fadeDuration = 1.0f)
    {
        _isAmbiencePlaylistActive = false;
        if (_ambiencePlaylistCoroutine != null)
        {
            StopCoroutine(_ambiencePlaylistCoroutine);
            _ambiencePlaylistCoroutine = null;
        }
        StartCoroutine(FadeSourceVolume(_ambienceAudioSource, 0f, fadeDuration, true));
    }

    private IEnumerator AmbiencePlaylistLoop()
    {
        while (_isAmbiencePlaylistActive)
        {
            if (_ambienceMusic == null || _ambienceMusic.Count == 0)
            {
                yield return new WaitForSeconds(1.0f);
                continue;
            }

            if (_ambiencetrackQueue == null || _ambiencetrackQueue.Count == 0)
            {
                _ambiencetrackQueue = new Queue<AudioClip>(_ambienceMusic);
            }

            if (_ambiencetrackQueue.Count > 0)
            {
                AudioClip nextClip = _ambiencetrackQueue.Dequeue();
                if (nextClip != null)
                {
                    yield return StartCoroutine(TransitionAmbienceTrackCoroutine(nextClip, 1.5f, false));

                    while (_ambienceAudioSource.isPlaying && _ambienceAudioSource.clip == nextClip)
                    {
                        yield return new WaitForSeconds(1.0f);
                    }
                }
                else
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }
        }
    }

    public void PlayAmbienceTrack(AudioClip clip, float fadeDuration = 1.0f, bool loop = true)
    {
        _isAmbiencePlaylistActive = false;
        if (_ambiencePlaylistCoroutine != null)
        {
            StopCoroutine(_ambiencePlaylistCoroutine);
            _ambiencePlaylistCoroutine = null;
        }

        if (_ambienceFadeCoroutine != null) StopCoroutine(_ambienceFadeCoroutine);
        _ambienceFadeCoroutine = StartCoroutine(TransitionAmbienceTrackCoroutine(clip, fadeDuration, loop));
    }

    private IEnumerator TransitionAmbienceTrackCoroutine(AudioClip clip, float fadeDuration, bool loop)
    {
        if (_ambienceAudioSource.isPlaying && _ambienceAudioSource.volume > 0f)
        {
            float startVol = _ambienceAudioSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _ambienceAudioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                yield return null;
            }
            _ambienceAudioSource.volume = 0f;
            _ambienceAudioSource.Stop();
        }

        _ambienceAudioSource.clip = clip;
        _ambienceAudioSource.loop = loop;
        _ambienceAudioSource.volume = 0f;
        _ambienceAudioSource.Play();

        float elapsedIn = 0f;
        while (elapsedIn < fadeDuration)
        {
            elapsedIn += Time.deltaTime;
            _ambienceAudioSource.volume = Mathf.Lerp(0f, _ambienceVolume, elapsedIn / fadeDuration);
            yield return null;
        }
        _ambienceAudioSource.volume = _ambienceVolume;
    }

    #region Volume Sliders & API
    public float PeacefulVolume
    {
        get => _baseVolume;
        set => SetPeacefulVolume(value);
    }

    public float CombatVolume
    {
        get => _combatVolume;
        set => SetCombatVolume(value);
    }

    public float AmbienceVolume
    {
        get => _ambienceVolume;
        set => SetAmbienceVolume(value);
    }

    public float BaseVolumeInCombat
    {
        get => _baseVolumeInCombat;
        set => SetBaseVolumeInCombat(value);
    }

    public void SetPeacefulVolume(float volume)
    {
        _baseVolume = Mathf.Clamp01(volume);
        if (_peacefulAudioSource != null)
        {
            _peacefulAudioSource.volume = _isInCombat ? _baseVolumeInCombat : _baseVolume;
        }
    }

    public void SetCombatVolume(float volume)
    {
        _combatVolume = Mathf.Clamp01(volume);
        if (_combatAudioSource != null)
        {
            _combatAudioSource.volume = _isInCombat ? _combatVolume : 0f;
        }
    }

    public void SetAmbienceVolume(float volume)
    {
        _ambienceVolume = Mathf.Clamp01(volume);
        if (_ambienceAudioSource != null)
        {
            _ambienceAudioSource.volume = _ambienceVolume;
        }
    }

    public void SetBaseVolumeInCombat(float volume)
    {
        _baseVolumeInCombat = Mathf.Clamp01(volume);
        if (_peacefulAudioSource != null)
        {
            _peacefulAudioSource.volume = _isInCombat ? _baseVolumeInCombat : _baseVolume;
        }
    }

    void Update()
    {
        // Smoothly apply inspector slider updates in real-time if we are not fading
        if (_peacefulAudioSource != null && _baseCombatFadeCoroutine == null && _baseFadeCoroutine == null)
        {
            _peacefulAudioSource.volume = _isInCombat ? _baseVolumeInCombat : _baseVolume;
        }
        if (_combatAudioSource != null && _combatFadeCoroutine == null)
        {
            _combatAudioSource.volume = _isInCombat ? _combatVolume : 0f;
        }
        if (_ambienceAudioSource != null && _ambienceFadeCoroutine == null)
        {
            _ambienceAudioSource.volume = _ambienceVolume;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            if (_peacefulAudioSource != null && _baseCombatFadeCoroutine == null && _baseFadeCoroutine == null)
            {
                _peacefulAudioSource.volume = _isInCombat ? _baseVolumeInCombat : _baseVolume;
            }
            if (_combatAudioSource != null && _combatFadeCoroutine == null)
            {
                _combatAudioSource.volume = _isInCombat ? _combatVolume : 0f;
            }
            if (_ambienceAudioSource != null && _ambienceFadeCoroutine == null)
            {
                _ambienceAudioSource.volume = _ambienceVolume;
            }
        }
    }
#endif
    #endregion
}
