using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Player;

[DisallowMultipleComponent]
public class SceneMusicManager : MonoBehaviour
{
    [Serializable]
    public class SceneMusicEntry
    {
        public string sceneName = "";
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public static SceneMusicManager Instance { get; private set; }

    [Header("Scene Music")]
    public List<SceneMusicEntry> sceneTracks = new List<SceneMusicEntry>();

    [Header("Audio Source")]
    public AudioSource musicSource;
    public bool attachAudioSourceToPlayer = true;

    private readonly Dictionary<string, SceneMusicEntry> sceneTrackMap = new Dictionary<string, SceneMusicEntry>(StringComparer.OrdinalIgnoreCase);
    private bool bossMusicActive;
    private float nextAttachRetryTime;
    private Transform followedPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = GetComponentInChildren<AudioSource>(true);

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        EnsureReparentableMusicSource();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;

        TryAttachAudioSourceToPlayer(true);
        RebuildSceneTrackMap();
        PlaySceneMusic(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnValidate()
    {
        if (musicSource == null)
            musicSource = GetComponentInChildren<AudioSource>(true);

        RebuildSceneTrackMap();
    }

    private void LateUpdate()
    {
        if (!attachAudioSourceToPlayer)
            return;

        EnsureMusicSourceAvailable();

        if (musicSource == null)
            return;

        if (followedPlayer == null || !followedPlayer.gameObject.activeInHierarchy)
        {
            if (Time.unscaledTime >= nextAttachRetryTime)
            {
                nextAttachRetryTime = Time.unscaledTime + 0.5f;
                TryAttachAudioSourceToPlayer(false);
            }

            return;
        }

        musicSource.transform.position = followedPlayer.position;
    }

    public void PlayBossMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null || musicSource == null)
            return;

        bossMusicActive = true;
        PlayClipLooped(clip, Mathf.Clamp01(volume));
    }

    public void StopBossMusic()
    {
        if (!bossMusicActive)
            return;

        bossMusicActive = false;
        PlaySceneMusic(SceneManager.GetActiveScene().name);
    }

    public static bool TryGetSharedAudioSource(out AudioSource source)
    {
        source = null;

        if (Instance == null)
            return false;

        Instance.EnsureMusicSourceAvailable();
        source = Instance.musicSource;
        return source != null;
    }

    public void RebuildSceneTrackMap()
    {
        sceneTrackMap.Clear();

        for (int i = 0; i < sceneTracks.Count; i++)
        {
            SceneMusicEntry entry = sceneTracks[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.sceneName))
                continue;

            sceneTrackMap[entry.sceneName.Trim()] = entry;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bossMusicActive = false;
        followedPlayer = null;
        EnsureMusicSourceAvailable();
        TryAttachAudioSourceToPlayer(true);
        PlaySceneMusic(scene.name);
    }

    private void TryAttachAudioSourceToPlayer(bool forceFind)
    {
        if (!attachAudioSourceToPlayer || musicSource == null)
            return;

        if (musicSource.transform.parent != transform)
            musicSource.transform.SetParent(transform, false);

        if (!forceFind && followedPlayer != null && followedPlayer.gameObject.activeInHierarchy)
            return;

        PlayerBase player = FindObjectOfType<PlayerBase>();
        if (player == null) return;

        followedPlayer = player.transform;
        musicSource.transform.position = followedPlayer.position;
    }

    private void EnsureReparentableMusicSource()
    {
        if (musicSource == null)
            return;

        if (musicSource.transform.parent == transform && musicSource.transform != transform)
            return;

        GameObject audioChild = new GameObject("MusicAudioSource");
        audioChild.transform.SetParent(transform, false);
        AudioSource newSource = audioChild.AddComponent<AudioSource>();
        CopyAudioSourceSettings(musicSource, newSource);

        bool sourceBelongsToManager =
            musicSource.gameObject == gameObject ||
            musicSource.transform.parent == transform;

        if (sourceBelongsToManager)
        {
            if (Application.isPlaying)
                Destroy(musicSource);
            else
                DestroyImmediate(musicSource);
        }

        musicSource = newSource;
    }

    private void EnsureMusicSourceAvailable()
    {
        if (musicSource != null)
            return;

        Transform existing = transform.Find("MusicAudioSource");
        if (existing != null)
            musicSource = existing.GetComponent<AudioSource>();

        if (musicSource != null)
            return;

        GameObject audioChild = new GameObject("MusicAudioSource");
        audioChild.transform.SetParent(transform, false);
        musicSource = audioChild.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
    }

    private static void CopyAudioSourceSettings(AudioSource from, AudioSource to)
    {
        if (from == null || to == null)
            return;

        to.clip = from.clip;
        to.outputAudioMixerGroup = from.outputAudioMixerGroup;
        to.mute = from.mute;
        to.bypassEffects = from.bypassEffects;
        to.bypassListenerEffects = from.bypassListenerEffects;
        to.bypassReverbZones = from.bypassReverbZones;
        to.playOnAwake = from.playOnAwake;
        to.loop = from.loop;
        to.priority = from.priority;
        to.volume = from.volume;
        to.pitch = from.pitch;
        to.panStereo = from.panStereo;
        to.spatialBlend = from.spatialBlend;
        to.reverbZoneMix = from.reverbZoneMix;
        to.dopplerLevel = from.dopplerLevel;
        to.spread = from.spread;
        to.rolloffMode = from.rolloffMode;
        to.minDistance = from.minDistance;
        to.maxDistance = from.maxDistance;
    }

    private void PlaySceneMusic(string sceneName)
    {
        if (musicSource == null)
            return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            musicSource.Stop();
            musicSource.clip = null;
            return;
        }

        if (!sceneTrackMap.TryGetValue(sceneName.Trim(), out SceneMusicEntry entry) || entry == null || entry.clip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            return;
        }

        PlayClipLooped(entry.clip, Mathf.Clamp01(entry.volume));
    }

    private void PlayClipLooped(AudioClip clip, float volume)
    {
        if (musicSource == null || clip == null)
            return;

        bool sameClip = musicSource.clip == clip;
        musicSource.volume = volume;
        musicSource.loop = true;

        if (sameClip)
        {
            if (!musicSource.isPlaying)
                musicSource.Play();
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
    }
}
