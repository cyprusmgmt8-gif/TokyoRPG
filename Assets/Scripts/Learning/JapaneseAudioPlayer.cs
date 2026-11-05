using UnityEngine;

/// <summary>
/// Plays one local recording at a time.
/// Learning panels, and later NPC dialogue or quizzes, can call Play with their own clip.
/// Slow speech, natural speed, and different voices can be added later as extra clips.
/// </summary>
public class JapaneseAudioPlayer : MonoBehaviour
{
    public static JapaneseAudioPlayer Instance { get; private set; }

    AudioSource source;
    AudioClip lastClip;
    AudioClip testTone;

    public static JapaneseAudioPlayer Ensure()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject playerObject = new GameObject("Japanese Audio");
        return playerObject.AddComponent<JapaneseAudioPlayer>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool IsPlaying(AudioClip clip)
    {
        return clip != null && source != null && source.isPlaying && source.clip == clip;
    }

    /// <summary>
    /// Starts a clip from the beginning and stops anything already playing.
    /// Calling it again for the same clip restarts that clip.
    /// </summary>
    public void Play(AudioClip clip)
    {
        if (clip == null || source == null)
        {
            return;
        }

        source.Stop();
        source.clip = clip;
        source.time = 0f;
        source.Play();
        lastClip = clip;
    }

    public void Stop()
    {
        if (source != null)
        {
            source.Stop();
        }
    }

    /// <summary>
    /// Plays the last clip again from the beginning.
    /// </summary>
    public void Replay()
    {
        Play(lastClip);
    }

    /// <summary>
    /// Plays a short beep so the speaker can be tested before Japanese recordings are added.
    /// This is not speech.
    /// </summary>
    public void PlayTestTone()
    {
        if (testTone == null)
        {
            testTone = CreateTestTone();
        }

        if (IsPlaying(testTone))
        {
            Stop();
            return;
        }

        Play(testTone);
    }

    public bool IsPlayingTestTone()
    {
        return IsPlaying(testTone);
    }

    static AudioClip CreateTestTone()
    {
        const int sampleRate = 44100;
        const float seconds = 0.35f;
        int sampleCount = Mathf.CeilToInt(sampleRate * seconds);
        float[] samples = new float[sampleCount];
        const float frequency = 523f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float fade = 1f;
            float edge = 0.03f;
            if (time < edge)
            {
                fade = time / edge;
            }
            else if (time > seconds - edge)
            {
                fade = (seconds - time) / edge;
            }

            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.2f * fade;
        }

        AudioClip clip = AudioClip.Create("Test tone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
