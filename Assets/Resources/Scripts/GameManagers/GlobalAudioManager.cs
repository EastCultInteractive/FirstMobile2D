using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Resources.Scripts.Audio
{
    [DisallowMultipleComponent]
    public sealed class GlobalAudioManager : MonoBehaviour
    {
        #region Singleton

        public static GlobalAudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            InitializeSfxSource();
            InitializeMusicSource();
            RegisterButtonSounds();
        }

        private void Start()
        {
            // Автоматически запускаем первый трек при старте, если задан плейлист
            if (musicPlaylist.Count > 0)
                PlayMusic(0, loop: true);
        }

        #endregion

        [Header("Audio Mixer (Optional)")]
        [Tooltip("Main project AudioMixer with SFX, Music groups, etc.")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("SFX/UI Settings")]
        [Tooltip("AudioSource for playing SFX and UI sounds")]
        [SerializeField] private AudioSource sfxSource;
        [Tooltip("Default SFX/UI volume (0–1)")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultSfxVolume = 1f;
        [Tooltip("Mixer Group for SFX/UI")]
        [SerializeField] private AudioMixerGroup sfxMixerGroup;

        [Header("Tick Sound (e.g. for drawing tools)")]
        [Tooltip("Short tick AudioClip")]
        [SerializeField] private AudioClip drawingTickClip;
        [Tooltip("Tick volume (0–1)")]
        [Range(0f, 1f)]
        [SerializeField] private float drawingTickVolume = 1f;

        [Header("Tick Rhythm Settings")]
        [Tooltip("Initial tick interval in seconds")]
        [SerializeField] private float startTickInterval = 1f;
        [Tooltip("Minimum interval at the end in seconds")]
        [SerializeField] private float endTickInterval = 0.2f;
        [Tooltip("Pitch at the start of the tick rhythm")]
        [SerializeField] private float tickMinPitch = 1f;
        [Tooltip("Pitch at the end of the tick rhythm")]
        [SerializeField] private float tickMaxPitch = 1.5f;

        [Header("Background Music (BGM)")]
        [Tooltip("AudioSource for background music")]
        [SerializeField] private AudioSource musicSource;
        [Tooltip("Playlist for background music")]
        [SerializeField] private List<AudioClip> musicPlaylist = new List<AudioClip>();
        [Tooltip("Music volume (0–1)")]
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 1f;
        [Tooltip("Cross-fade duration between tracks")]
        [SerializeField] private float crossFadeDuration = 1f;

        private int currentTrackIndex;
        private Coroutine musicFadeCoroutine;
        private Coroutine tickRoutine;

        #region Initialization

        private void InitializeSfxSource()
        {
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            sfxSource.volume = defaultSfxVolume;
            if (sfxMixerGroup != null)
                sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        }

        private void InitializeMusicSource()
        {
            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();

            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.volume = musicVolume; // Устанавливаем заданную громкость

            if (audioMixer != null)
            {
                var groups = audioMixer.FindMatchingGroups("Music");
                if (groups.Length > 0)
                    musicSource.outputAudioMixerGroup = groups[0];
            }
        }

        #endregion

        #region Button Registration

        [Header("Button Bindings")]
        [Tooltip("List of buttons and their click/hover sound settings")]
        [SerializeField] private List<ButtonAudioConfig> buttonConfigs = new List<ButtonAudioConfig>();

        private void RegisterButtonSounds()
        {
            foreach (var config in buttonConfigs)
                config.Register(this);
        }

        [Serializable]
        private class ButtonAudioConfig
        {
            [Tooltip("UI Button to attach sounds to")]
            [SerializeField] private Button button;
            [Tooltip("Click sound")]
            [SerializeField] private AudioClip clickClip;
            [Tooltip("Click volume")]
            [Range(0f, 1f)]
            [SerializeField] private float clickVolume = 1f;
            [Tooltip("Hover sound")]
            [SerializeField] private AudioClip hoverClip;
            [Tooltip("Hover volume")]
            [Range(0f, 1f)]
            [SerializeField] private float hoverVolume = 1f;

            public void Register(GlobalAudioManager mgr)
            {
                if (button == null)
                    return;

                if (clickClip != null)
                    button.onClick.AddListener(() => mgr.PlaySfx(clickClip, clickVolume));

                if (hoverClip != null)
                {
                    var trigger = button.GetComponent<EventTrigger>()
                                  ?? button.gameObject.AddComponent<EventTrigger>();

                    var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                    entry.callback.AddListener(_ => mgr.PlaySfx(hoverClip, hoverVolume));
                    trigger.triggers.Add(entry);
                }
            }
        }

        #endregion

        #region SFX & Tick Playback

        /// <summary>Plays a UI/SFX sound clip at a specified or default volume.</summary>
        public void PlaySfx(AudioClip clip, float? volume = null)
        {
            if (clip == null)
                return;

            if (sfxSource == null)
                InitializeSfxSource();

            float vol = Mathf.Clamp01(volume ?? defaultSfxVolume);
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(clip, vol);
        }

        /// <summary>Plays a single tick sound with specified pitch.</summary>
        private void PlayTickOnce(float pitch)
        {
            if (drawingTickClip == null)
                return;

            if (sfxSource == null)
                InitializeSfxSource();

            float prevPitch = sfxSource.pitch;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(drawingTickClip, drawingTickVolume);
            sfxSource.pitch = prevPitch;
        }

        /// <summary>Starts a ticking rhythm over a given duration.</summary>
        public void StartTickRhythm(float duration)
        {
            if (tickRoutine != null)
                StopCoroutine(tickRoutine);

            tickRoutine = StartCoroutine(TickRhythmCoroutine(duration));
        }

        /// <summary>Stops the ticking rhythm if active.</summary>
        public void StopTickRhythm()
        {
            if (tickRoutine != null)
            {
                StopCoroutine(tickRoutine);
                tickRoutine = null;
            }
        }

        private IEnumerator TickRhythmCoroutine(float duration)
        {
            if (drawingTickClip == null)
                yield break;

            float startTime = Time.time;
            while (Time.time - startTime < duration)
            {
                float elapsed = Time.time - startTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float interval = Mathf.Lerp(startTickInterval, endTickInterval, t);
                float pitch = Mathf.Lerp(tickMinPitch, tickMaxPitch, t);

                PlayTickOnce(pitch);
                yield return new WaitForSeconds(interval);
            }
        }

        /// <summary>Sets the default SFX volume.</summary>
        public void SetSfxVolume(float vol)
        {
            defaultSfxVolume = Mathf.Clamp01(vol);
            if (sfxSource != null)
                sfxSource.volume = defaultSfxVolume;
        }

        #endregion

        #region Music Playback

        public void PlayNextMusic() =>
            PlayMusic((currentTrackIndex + 1) % musicPlaylist.Count);

        public void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
                musicSource.Pause();
        }

        public void ResumeMusic()
        {
            if (musicSource != null && !musicSource.isPlaying)
                musicSource.UnPause();
        }

        public void SetMusicVolume(float vol)
        {
            musicVolume = Mathf.Clamp01(vol);
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }

        private void PlayMusic(int index = 0, bool loop = true)
        {
            if (musicPlaylist == null || musicPlaylist.Count == 0 || musicSource == null)
                return;

            index = Mathf.Clamp(index, 0, musicPlaylist.Count - 1);
            currentTrackIndex = index;

            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);

            musicFadeCoroutine = StartCoroutine(FadeAndPlayMusic(musicPlaylist[index], loop));
        }

        private IEnumerator FadeAndPlayMusic(AudioClip clip, bool loop)
        {
            if (musicSource.isPlaying)
                yield return FadeCoroutine(musicSource, musicSource.volume, 0f, crossFadeDuration / 2);

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();

            yield return FadeCoroutine(musicSource, 0f, musicVolume, crossFadeDuration / 2);
        }

        private static IEnumerator FadeCoroutine(AudioSource src, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            src.volume = to;
        }

        #endregion
    }
}
