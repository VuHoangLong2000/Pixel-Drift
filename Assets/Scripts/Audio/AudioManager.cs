using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AudioManager
/// ------------------------------------------------------------
/// Quản lý toàn bộ Audio trong game.
/// 
/// Chỉ chịu trách nhiệm:
/// - Music
/// - SFX
/// - Voice
/// - Volume
/// - Fade
/// ------------------------------------------------------------
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float voiceVolume = 1f;

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }
    private void Init()
    {
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        musicSource.volume = musicVolume * masterVolume;
        sfxSource.volume = sfxVolume * masterVolume;
        voiceSource.volume = voiceVolume * masterVolume;
    }
    #region MUSIC
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }
    public void StopMusic()
    {
        musicSource.Stop();
    }
    public void FadeMusic(AudioClip clip, float duration = 1f)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeMusicRoutine(clip, duration));
    }
    private IEnumerator FadeMusicRoutine(AudioClip clip, float duration)
    {
        float startVolume = musicSource.volume;

        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime / duration;
            yield return null;
        }
        musicSource.Stop();

        musicSource.clip = clip;
        musicSource.Play();

        while (musicSource.volume < musicVolume * masterVolume)
        {
            musicSource.volume += musicVolume * masterVolume * Time.deltaTime / duration;
            yield return null;
        }
    }
    #endregion

    #region SFX
    public void PlaySFX(AudioClip clip)
    {
        if(clip == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
    }
    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
    }
    #endregion

    #region VOICE
    public void PlayVoice(AudioClip clip)
    {
        if (clip == null)
            return;

        voiceSource.clip = clip;
        voiceSource.Play();
    }

    public void StopVoice()
    {
        voiceSource.Stop();
    }
    #endregion

    #region VOLUME
    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        ApplyVolume();
    }
    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        ApplyVolume();
    }
    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
    }

    public void SetVoiceVolume(float value)
    {
        voiceVolume = value;

        voiceSource.volume = voiceVolume * masterVolume;
    }
    #endregion
}
