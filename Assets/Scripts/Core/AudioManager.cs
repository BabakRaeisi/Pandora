using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [System.Serializable]
    public class Sound
    {
        public AudioClip clip;
        public bool loop;
        public SoundType type;

        [HideInInspector] public string id;
    }

    public enum SoundType
    {
        Music,
        Ambient,
        SFX
    }

    [Header("Sound Library")]
    public List<Sound> sounds = new List<Sound>();

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource ambientSource;
    public AudioSource sfxSource;

    Dictionary<string, Sound> lookup = new Dictionary<string, Sound>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var s in sounds)
        {
            if (s.clip == null)
                continue;

            s.id = s.clip.name;

            if (!lookup.ContainsKey(s.id))
                lookup.Add(s.id, s);
        }
    }

    public void Play(string clipName)
    {
        if (!lookup.TryGetValue(clipName, out Sound sound))
            return;

        switch (sound.type)
        {
            case SoundType.Music:
                musicSource.clip = sound.clip;
                musicSource.loop = sound.loop;
                musicSource.Play();
                break;

            case SoundType.Ambient:
                ambientSource.clip = sound.clip;
                ambientSource.loop = sound.loop;
                ambientSource.Play();
                break;

            case SoundType.SFX:
                sfxSource.PlayOneShot(sound.clip);
                break;
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void StopAmbient()
    {
        ambientSource.Stop();
    }

    public void StopAll()
    {
        musicSource.Stop();
        ambientSource.Stop();
        sfxSource.Stop();
    }
}