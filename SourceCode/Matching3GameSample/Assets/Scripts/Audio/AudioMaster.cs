using UnityEngine;

namespace Match3Sample.Audio
{
    public class AudioMaster : MonoBehaviour
    {
        public static AudioMaster Instance { get; private set; }
        [SerializeField]
        private AudioClip battleMusic = null;
        [SerializeField]
        private AudioClip powerupSFX = null;
        private AudioSource backgroundAudioSource;
        private AudioSource sfxAudioSource;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
            backgroundAudioSource = gameObject.AddComponent<AudioSource>();
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            backgroundAudioSource.clip = battleMusic;
            backgroundAudioSource.loop = true;
            backgroundAudioSource.playOnAwake = false;
            sfxAudioSource.playOnAwake = false;
        }

        public void PlayMusic(float volume)
        {
            SetMusicVolume(volume);
            backgroundAudioSource.Play();
        }

        public void StopMusic()
        {
            backgroundAudioSource.Stop();
        }

        public void FadeOutMusic()
        {
            if (backgroundAudioSource.volume > 0)
                backgroundAudioSource.volume -= Time.deltaTime;
        }

        public void SetMusicVolume(float volume)
        {
            backgroundAudioSource.volume = volume;
        }

        public void PlaySFX(AudioClip clip)
        {
            sfxAudioSource.PlayOneShot(clip);
        }

        public void PlayPowerupSFX()
        {
            PlaySFX(powerupSFX);
        }
    }
}
