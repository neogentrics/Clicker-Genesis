using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace ClickerGenesis.Core
{
    /// <summary>Which scene groups share a music track (Sound-System-Design.html). Scenes not
    /// listed in AudioManager's SceneToZone map (SettingsScreen, SaveSlotScreen,
    /// NewGameSetupScreen) are pass-through - they never trigger a zone change, whatever was
    /// already playing keeps playing underneath.</summary>
    public enum MusicZone { None, Menu, CoreGameplay, BuyVerse, SkillTree, Achievements, Store, Credits, Settings, SaveSlot, NewGameSetup }

    /// <summary>
    /// Persistent singleton (spawned once on GameRoot in MainMenu.unity, same
    /// Instance/DontDestroyOnLoad pattern as GameLoopController/SceneTransitioner) owning the
    /// MasterMixer and all runtime audio playback. Sound-System-Design.html, built 2026-08-11.
    ///
    /// No audio clips exist in the project yet (placeholder-silence build, per explicit user
    /// choice) - PlaySfx/CrossfadeMusic/PlayVoice are all null-safe and simply do nothing without
    /// a clip assigned, so this infrastructure is ready for clips to be dropped in later with no
    /// further code changes.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioMixer mixer;

        [Header("Music zone clips (empty = silence, drop clips in later)")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip coreGameplayMusic;
        [SerializeField] private AudioClip buyVerseMusic;
        [SerializeField] private AudioClip skillTreeMusic;
        [SerializeField] private AudioClip achievementsMusic;
        [SerializeField] private AudioClip storeMusic;
        [SerializeField] private AudioClip creditsMusic;
        [SerializeField] private AudioClip settingsMusic;
        [SerializeField] private AudioClip saveSlotMusic;
        [SerializeField] private AudioClip newGameSetupMusic;

        [Header("SFX pool - overlapping sounds (e.g. rapid tapping) shouldn't cut each other off")]
        [SerializeField] private int sfxPoolSize = 6;

        [Header("Generic SFX (default sound for any button without a specific one assigned - see UIButtonClickSfx)")]
        [SerializeField] private AudioClip genericClickSfx;

        [Header("Purchase SFX (2026-08-12) - Scribe/Manager/Support/Verse Buy buttons, book-switch selects")]
        [SerializeField] private AudioClip purchaseClickSfx;

        [Header("Crossfade")]
        [SerializeField] private float musicCrossfadeSeconds = 1.5f;

        private AudioSource musicSourceA;
        private AudioSource musicSourceB;
        private bool musicAIsActive;
        private Coroutine crossfadeRoutine;

        private AudioSource[] sfxSources;
        private int nextSfxSourceIndex;

        private AudioSource voiceSource;

        private MusicZone currentZone = MusicZone.None;

        /// <summary>REVISED 2026-08-16 (real user correction): BuyVerseScreen is no longer part of
        /// CoreGameplay - it gets its own dedicated BuyVerse zone/clip instead, deliberately mixed
        /// low enough to sit under a future TTS/read-aloud system without competing with it (user's
        /// own call after actually listening to the track). CoreGameplay is ClickerScreen only now.
        /// Every real screen gets an explicit entry, even the ones that play nothing (MusicZone.None)
        /// - this replaces the old "absent from the map = pass-through, don't touch whatever's
        /// playing" behavior, which was the actual root cause of music bleeding from one screen into
        /// screens that were never supposed to have any (confirmed live: SettingsScreen/
        /// SaveSlotScreen/NewGameSetupScreen/StoreScreen were all missing entirely, so navigating to
        /// them never triggered a fade to silence at all).</summary>
        private static readonly Dictionary<string, MusicZone> SceneToZone = new Dictionary<string, MusicZone>
        {
            { "MainMenu", MusicZone.Menu },
            { "ClickerScreen", MusicZone.CoreGameplay },
            { "BuyVerseScreen", MusicZone.BuyVerse },
            { "PrestigeScreen", MusicZone.SkillTree },
            { "AchievementScreen", MusicZone.Achievements },
            { "CreditsScreen", MusicZone.Credits },
            { "SettingsScreen", MusicZone.Settings },
            { "SaveSlotScreen", MusicZone.SaveSlot },
            { "NewGameSetupScreen", MusicZone.NewGameSetup },
            { "StoreScreen", MusicZone.Store },
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // See GameLoopController.Awake() for why this is guarded.
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            BuildAudioSources();
            ApplyAllVolumes();
            GameSettings.OnChanged += ApplyAllVolumes;

            // AudioManager is only ever first-spawned in MainMenu.unity (same as GameLoopController's
            // GameRoot) - hardcode rather than read gameObject.scene.name, since that name is only
            // valid before DontDestroyOnLoad moves this object into the special DDOL pseudo-scene.
            NotifySceneChanging("MainMenu");
        }

        private void OnDestroy()
        {
            GameSettings.OnChanged -= ApplyAllVolumes;
        }

        private void BuildAudioSources()
        {
            var musicGroup = FindGroup("Music");
            var sfxGroup = FindGroup("SFX");
            var voiceGroup = FindGroup("Voice");

            musicSourceA = NewSource("MusicSourceA", musicGroup, loop: true);
            musicSourceB = NewSource("MusicSourceB", musicGroup, loop: true);

            sfxSources = new AudioSource[Mathf.Max(1, sfxPoolSize)];
            for (int i = 0; i < sfxSources.Length; i++)
                sfxSources[i] = NewSource($"SfxSource{i}", sfxGroup, loop: false);

            voiceSource = NewSource("VoiceSource", voiceGroup, loop: false);
        }

        private AudioSource NewSource(string sourceName, AudioMixerGroup group, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.outputAudioMixerGroup = group;
            src.volume = loop ? 0f : 1f; // music sources start silent - CrossfadeRoutine brings them up
            return src;
        }

        private AudioMixerGroup FindGroup(string groupName)
        {
            if (mixer == null) return null;
            var groups = mixer.FindMatchingGroups(groupName);
            return groups.Length > 0 ? groups[0] : null;
        }

        // ---- Volume plumbing ----

        /// <summary>Mirrors GameSettings' four sliders + master mute onto the mixer's exposed
        /// params. SetFloat only actually takes effect once the mixer is part of a live audio
        /// graph (confirmed 2026-08-11: it silently no-ops in the Editor outside Play mode, works
        /// correctly in Play mode and in a real build) - harmless to call from Awake either way.</summary>
        private void ApplyAllVolumes()
        {
            if (mixer == null) return;
            SetMixerVolume("MasterVolume", GameSettings.MasterMuted ? 0f : GameSettings.MasterVolume);
            SetMixerVolume("SfxVolume", GameSettings.SfxVolume);
            SetMixerVolume("MusicVolume", GameSettings.MusicVolume);
            SetMixerVolume("VoiceVolume", GameSettings.VoiceVolume);
        }

        private void SetMixerVolume(string param, float linear)
        {
            float dB = linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
            mixer.SetFloat(param, dB);
        }

        // ---- Public API ----

        /// <summary>Single funnel every SFX call goes through (PlayGenericClick/PlayPurchaseClick/
        /// GameLoopController's direct verse-unlock/chapter/book/achievement calls) - gated by the
        /// per-scene SFX mute (2026-08-13, real user ask) so one check here covers all of them.
        /// Music/Voice are untouched by this - confirmed scope, Music already crossfades per zone
        /// and a per-scene music mute would fight that rather than complement it.</summary>
        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSources == null || sfxSources.Length == 0) return;
            if (GameSettings.IsSceneSfxMuted(SceneManager.GetActiveScene().name)) return;
            var src = sfxSources[nextSfxSourceIndex];
            nextSfxSourceIndex = (nextSfxSourceIndex + 1) % sfxSources.Length;
            src.PlayOneShot(clip, volumeScale);
        }

        /// <summary>The generic UI click SFX category (Sound-System-Design.html) - the default
        /// sound for any button without a specific one assigned. See UIButtonClickSfx for the
        /// attach-and-go component that calls this from a Button's onClick.</summary>
        public void PlayGenericClick() => PlaySfx(genericClickSfx);

        /// <summary>Dedicated purchase/transaction click (2026-08-12, user-curated SwishSwoosh "Free
        /// UI Click Sound Pack") - the coin-like Plastic click, used by every Buy/spend button
        /// across Scribes/Managers/Support/BuyVerse instead of the generic click.</summary>
        public void PlayPurchaseClick() => PlaySfx(purchaseClickSfx);

        public void PlayVoice(AudioClip clip)
        {
            if (clip == null || voiceSource == null) return;
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();
        }

        public void CrossfadeMusic(AudioClip clip)
        {
            if (!Application.isPlaying) return;
            if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = StartCoroutine(CrossfadeRoutine(clip));
        }

        /// <summary>Called by SceneTransitioner right before a scene load starts (it already knows
        /// the target scene name synchronously) so the crossfade begins in parallel with the load,
        /// not after it finishes. Every real screen now has an explicit SceneToZone entry (2026-08-16
        /// - see the dictionary's own doc comment for why the old "absent = pass-through" behavior
        /// was removed); a scene name genuinely missing from the map is only a real bug or a
        /// not-yet-integrated screen, not an intentional silence case anymore.</summary>
        public void NotifySceneChanging(string sceneName)
        {
            if (!SceneToZone.TryGetValue(sceneName, out var zone)) return;
            if (zone == currentZone) return;
            currentZone = zone;
            CrossfadeMusic(GetClipForZone(zone));
        }

        private AudioClip GetClipForZone(MusicZone zone) => zone switch
        {
            MusicZone.Menu => menuMusic,
            MusicZone.CoreGameplay => coreGameplayMusic,
            MusicZone.BuyVerse => buyVerseMusic,
            MusicZone.SkillTree => skillTreeMusic,
            MusicZone.Achievements => achievementsMusic,
            MusicZone.Store => storeMusic,
            MusicZone.Credits => creditsMusic,
            MusicZone.Settings => settingsMusic,
            MusicZone.SaveSlot => saveSlotMusic,
            MusicZone.NewGameSetup => newGameSetupMusic,
            _ => null,
        };

        private IEnumerator CrossfadeRoutine(AudioClip newClip)
        {
            var outgoing = musicAIsActive ? musicSourceA : musicSourceB;
            var incoming = musicAIsActive ? musicSourceB : musicSourceA;
            musicAIsActive = !musicAIsActive;

            if (newClip != null)
            {
                incoming.clip = newClip;
                incoming.volume = 0f;
                incoming.Play();
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, musicCrossfadeSeconds);
            float outgoingStartVolume = outgoing.volume;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                outgoing.volume = Mathf.Lerp(outgoingStartVolume, 0f, t);
                if (newClip != null) incoming.volume = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            outgoing.volume = 0f;
            outgoing.Stop();
            outgoing.clip = null;
            if (newClip != null) incoming.volume = 1f;

            crossfadeRoutine = null;
        }
    }
}
