
using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance;

    [Header("Sound Effects")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Background Music")]
    [SerializeField] private AudioSource musicSource;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null)
            return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }
}
