using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource sfxSource;

    private AudioSource musicSource;

    private AudioClip revealClip;
    private AudioClip sweepClip;
    private AudioClip flagClip;
    private AudioClip unflagClip;
    private AudioClip explosionClip;
    private AudioClip winClip;
    private AudioClip loseClip;
    private AudioClip ambientClip;

    private Coroutine musicFadeCoroutine;

    // User settings (0-1, from slider)
    private float userMusicVolume = 1f;
    private float userSfxVolume = 1f;
    private bool musicEnabled = true;
    private bool sfxEnabled = true;

    // Context volume (changes based on menu/game/gameover)
    private float contextMusicVolume = 0.05f;

    private const int SampleRate = 44100;

    private const string PrefMusicVol = "Minesweeper_MusicVolume";
    private const string PrefSfxVol = "Minesweeper_SfxVolume";
    private const string PrefMusicEnabled = "Minesweeper_MusicEnabled";
    private const string PrefSfxEnabled = "Minesweeper_SfxEnabled";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.SetMenuVolume();
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = 0f;

        GenerateAllClips();
        ambientClip = GenerateAmbientMusic();
        musicSource.clip = ambientClip;

        // Auto-start music at menu volume
        if (musicEnabled)
        {
            contextMusicVolume = 0.05f;
            musicSource.Play();
            FadeMusicTo(GetEffectiveMusicVolume(), 2f);
        }
    }

    // ========== VOLUME HELPERS ==========

    private float GetEffectiveMusicVolume()
    {
        return contextMusicVolume * userMusicVolume * (musicEnabled ? 1f : 0f);
    }

    private float GetEffectiveSfxVolume(float baseVolume)
    {
        return baseVolume * userSfxVolume * (sfxEnabled ? 1f : 0f);
    }

    private void ApplyMusicVolume(float fadeDuration)
    {
        FadeMusicTo(GetEffectiveMusicVolume(), fadeDuration);
    }

    // ========== SFX PUBLIC API ==========

    public void PlayReveal()
    {
        sfxSource.PlayOneShot(revealClip, GetEffectiveSfxVolume(0.4f));
    }

    public void PlaySweep()
    {
        sfxSource.PlayOneShot(sweepClip, GetEffectiveSfxVolume(0.5f));
    }

    public void PlayFlag()
    {
        sfxSource.PlayOneShot(flagClip, GetEffectiveSfxVolume(0.5f));
    }

    public void PlayUnflag()
    {
        sfxSource.PlayOneShot(unflagClip, GetEffectiveSfxVolume(0.5f));
    }

    public void PlayExplosion()
    {
        sfxSource.PlayOneShot(explosionClip, GetEffectiveSfxVolume(0.7f));
    }

    public void PlayWin()
    {
        sfxSource.PlayOneShot(winClip, GetEffectiveSfxVolume(0.6f));
    }

    public void PlayLose()
    {
        sfxSource.PlayOneShot(loseClip, GetEffectiveSfxVolume(0.5f));
    }

    // ========== MUSIC CONTEXT API ==========

    public void SetMenuVolume()
    {
        contextMusicVolume = 0.05f;
        ApplyMusicVolume(1f);
    }

    public void SetGameVolume()
    {
        contextMusicVolume = 0.15f;
        if (!musicSource.isPlaying && musicEnabled)
            musicSource.Play();
        ApplyMusicVolume(1f);
    }

    public void SetLowVolume()
    {
        contextMusicVolume = 0.08f;
        ApplyMusicVolume(0.5f);
    }

    // ========== SETTINGS API (for UI sliders/toggles) ==========

    public void SetMusicUserVolume(float vol)
    {
        userMusicVolume = Mathf.Clamp01(vol);
        ApplyMusicVolume(0.2f);
        SaveSettings();
    }

    public void SetSfxUserVolume(float vol)
    {
        userSfxVolume = Mathf.Clamp01(vol);
        SaveSettings();
    }

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        if (musicEnabled)
        {
            if (!musicSource.isPlaying)
                musicSource.Play();
            ApplyMusicVolume(0.5f);
        }
        else
        {
            FadeMusicTo(0f, 0.5f, true);
        }
        SaveSettings();
    }

    public void SetSfxEnabled(bool enabled)
    {
        sfxEnabled = enabled;
        SaveSettings();
    }

    public float GetMusicVolume() { return userMusicVolume; }
    public float GetSfxVolume() { return userSfxVolume; }
    public bool IsMusicEnabled() { return musicEnabled; }
    public bool IsSfxEnabled() { return sfxEnabled; }

    // ========== PERSISTENCE ==========

    private void LoadSettings()
    {
        userMusicVolume = PlayerPrefs.GetFloat(PrefMusicVol, 1f);
        userSfxVolume = PlayerPrefs.GetFloat(PrefSfxVol, 1f);
        musicEnabled = PlayerPrefs.GetInt(PrefMusicEnabled, 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt(PrefSfxEnabled, 1) == 1;
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(PrefMusicVol, userMusicVolume);
        PlayerPrefs.SetFloat(PrefSfxVol, userSfxVolume);
        PlayerPrefs.SetInt(PrefMusicEnabled, musicEnabled ? 1 : 0);
        PlayerPrefs.SetInt(PrefSfxEnabled, sfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ========== FADE SYSTEM ==========

    private void FadeMusicTo(float target, float duration, bool stopAfter = false)
    {
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeMusicCoroutine(target, duration, stopAfter));
    }

    private IEnumerator FadeMusicCoroutine(float target, float duration, bool stopAfter)
    {
        float startVol = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVol, target, elapsed / duration);
            yield return null;
        }

        musicSource.volume = target;
        if (stopAfter)
            musicSource.Stop();
        musicFadeCoroutine = null;
    }

    // ========== SFX CLIP GENERATION ==========

    private void GenerateAllClips()
    {
        revealClip = GenerateRevealClip();
        sweepClip = GenerateSweepClip();
        flagClip = GenerateFlagClip();
        unflagClip = GenerateUnflagClip();
        explosionClip = GenerateExplosionClip();
        winClip = GenerateWinClip();
        loseClip = GenerateLoseClip();
    }

    private AudioClip GenerateRevealClip()
    {
        float duration = 0.05f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        float freq = 1000f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = 1f - (float)i / sampleCount;
            envelope *= envelope;
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
        }

        AudioClip clip = AudioClip.Create("Reveal", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateSweepClip()
    {
        float duration = 0.15f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / sampleCount;
            float freq = Mathf.Lerp(300f, 600f, progress);
            float envelope = 1f - progress;

            float sine = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f;
            float noise = Random.Range(-1f, 1f) * 0.3f;
            samples[i] = (sine + noise) * envelope;
        }

        AudioClip clip = AudioClip.Create("Sweep", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateFlagClip()
    {
        float toneDur = 0.05f;
        float gap = 0.02f;
        float totalDur = toneDur * 2f + gap;
        int sampleCount = (int)(SampleRate * totalDur);
        float[] samples = new float[sampleCount];

        int tone1End = (int)(SampleRate * toneDur);
        int gapEnd = (int)(SampleRate * (toneDur + gap));
        int tone2End = sampleCount;

        for (int i = 0; i < tone1End; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = 1f - (float)i / tone1End;
            samples[i] = Mathf.Sin(2f * Mathf.PI * 600f * t) * envelope;
        }

        for (int i = gapEnd; i < tone2End; i++)
        {
            float t = (float)(i - gapEnd) / SampleRate;
            float progress = (float)(i - gapEnd) / (tone2End - gapEnd);
            float envelope = 1f - progress;
            samples[i] = Mathf.Sin(2f * Mathf.PI * 800f * t) * envelope;
        }

        AudioClip clip = AudioClip.Create("Flag", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateUnflagClip()
    {
        float toneDur = 0.05f;
        float gap = 0.02f;
        float totalDur = toneDur * 2f + gap;
        int sampleCount = (int)(SampleRate * totalDur);
        float[] samples = new float[sampleCount];

        int tone1End = (int)(SampleRate * toneDur);
        int gapEnd = (int)(SampleRate * (toneDur + gap));
        int tone2End = sampleCount;

        for (int i = 0; i < tone1End; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = 1f - (float)i / tone1End;
            samples[i] = Mathf.Sin(2f * Mathf.PI * 800f * t) * envelope;
        }

        for (int i = gapEnd; i < tone2End; i++)
        {
            float t = (float)(i - gapEnd) / SampleRate;
            float progress = (float)(i - gapEnd) / (tone2End - gapEnd);
            float envelope = 1f - progress;
            samples[i] = Mathf.Sin(2f * Mathf.PI * 500f * t) * envelope;
        }

        AudioClip clip = AudioClip.Create("Unflag", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateExplosionClip()
    {
        float duration = 0.4f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / sampleCount;

            float envelope;
            if (progress < 0.05f)
                envelope = progress / 0.05f;
            else
                envelope = Mathf.Exp(-(progress - 0.05f) * 5f);

            float freq = Mathf.Lerp(150f, 80f, progress);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f;
            float noise = Random.Range(-1f, 1f) * Mathf.Lerp(0.6f, 0.1f, progress);
            samples[i] = (sine + noise) * envelope;
        }

        AudioClip clip = AudioClip.Create("Explosion", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateWinClip()
    {
        float noteDur = 0.15f;
        float totalDur = noteDur * 3f;
        int sampleCount = (int)(SampleRate * totalDur);
        float[] samples = new float[sampleCount];

        float[] freqs = { 523f, 659f, 784f };

        for (int n = 0; n < 3; n++)
        {
            int noteStart = (int)(SampleRate * noteDur * n);
            int noteEnd = (int)(SampleRate * noteDur * (n + 1));

            for (int i = noteStart; i < noteEnd && i < sampleCount; i++)
            {
                float t = (float)(i - noteStart) / SampleRate;
                float noteProgress = (float)(i - noteStart) / (noteEnd - noteStart);

                float envelope;
                if (noteProgress < 0.1f)
                    envelope = noteProgress / 0.1f;
                else
                    envelope = 1f - (noteProgress - 0.1f) / 0.9f;

                samples[i] = Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * envelope;
            }
        }

        AudioClip clip = AudioClip.Create("Win", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateLoseClip()
    {
        float noteDur = 0.2f;
        float totalDur = noteDur * 2f;
        int sampleCount = (int)(SampleRate * totalDur);
        float[] samples = new float[sampleCount];

        float[] freqs = { 330f, 262f };

        for (int n = 0; n < 2; n++)
        {
            int noteStart = (int)(SampleRate * noteDur * n);
            int noteEnd = (int)(SampleRate * noteDur * (n + 1));

            for (int i = noteStart; i < noteEnd && i < sampleCount; i++)
            {
                float t = (float)(i - noteStart) / SampleRate;
                float noteProgress = (float)(i - noteStart) / (noteEnd - noteStart);

                float envelope;
                if (noteProgress < 0.05f)
                    envelope = noteProgress / 0.05f;
                else
                    envelope = 1f - (noteProgress - 0.05f) / 0.95f;

                envelope *= envelope;

                samples[i] = Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * envelope;
            }
        }

        AudioClip clip = AudioClip.Create("Lose", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // ========== AMBIENT MUSIC GENERATION ==========

    private AudioClip GenerateAmbientMusic()
    {
        float duration = 45f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        System.Random rng = new System.Random(42);

        float[] melodyFreqs = { 262f, 294f, 330f, 392f, 440f };
        float[] chimeFreqs = { 523f, 659f, 784f };

        int melodyCount = 50;
        float[] melodyStarts = new float[melodyCount];
        float[] melodyNotes = new float[melodyCount];
        float[] melodyDurations = new float[melodyCount];
        float melodyTime = 1f;
        for (int m = 0; m < melodyCount; m++)
        {
            melodyStarts[m] = melodyTime;
            melodyNotes[m] = melodyFreqs[rng.Next(melodyFreqs.Length)];
            melodyDurations[m] = 0.3f + (float)rng.NextDouble() * 0.2f;
            melodyTime += melodyDurations[m] + 0.5f + (float)rng.NextDouble() * 1f;
            if (melodyTime > duration - 3f) break;
        }

        int chimeCount = 12;
        float[] chimeStarts = new float[chimeCount];
        float[] chimeNotes = new float[chimeCount];
        float chimeTime = 2.5f;
        for (int c = 0; c < chimeCount; c++)
        {
            chimeStarts[c] = chimeTime;
            chimeNotes[c] = chimeFreqs[rng.Next(chimeFreqs.Length)];
            chimeTime += 3f + (float)rng.NextDouble() * 3f;
            if (chimeTime > duration - 3f) break;
        }

        float fadeZone = 2f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float sample = 0f;

            float loopEnv = 1f;
            if (t < fadeZone)
                loopEnv = t / fadeZone;
            else if (t > duration - fadeZone)
                loopEnv = (duration - t) / fadeZone;

            float padTremolo = 0.5f + 0.5f * Mathf.Sin(0.15f * 2f * Mathf.PI * t);
            sample += Mathf.Sin(2f * Mathf.PI * 131f * t) * 0.12f * padTremolo;
            sample += Mathf.Sin(2f * Mathf.PI * 196f * t) * 0.08f * padTremolo;
            sample += Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.06f * padTremolo;

            float padTremolo2 = 0.5f + 0.5f * Mathf.Sin(0.23f * 2f * Mathf.PI * t);
            sample += Mathf.Sin(2f * Mathf.PI * 98f * t) * 0.05f * padTremolo2;

            for (int m = 0; m < melodyCount; m++)
            {
                if (melodyStarts[m] == 0f) break;
                float noteStart = melodyStarts[m];
                float noteDur = melodyDurations[m];
                float noteEnd = noteStart + noteDur;

                if (t >= noteStart && t < noteEnd)
                {
                    float noteT = t - noteStart;
                    float noteProgress = noteT / noteDur;

                    float env;
                    if (noteProgress < 0.2f)
                        env = noteProgress / 0.2f;
                    else
                        env = 1f - (noteProgress - 0.2f) / 0.8f;

                    env *= env;
                    sample += Mathf.Sin(2f * Mathf.PI * melodyNotes[m] * noteT) * 0.14f * env;
                }
            }

            for (int c = 0; c < chimeCount; c++)
            {
                if (chimeStarts[c] == 0f) break;
                float noteStart = chimeStarts[c];
                float chimeDur = 1.5f;

                if (t >= noteStart && t < noteStart + chimeDur)
                {
                    float noteT = t - noteStart;
                    float noteProgress = noteT / chimeDur;

                    float env = Mathf.Exp(-noteProgress * 4f);
                    sample += Mathf.Sin(2f * Mathf.PI * chimeNotes[c] * noteT) * 0.06f * env;
                    sample += Mathf.Sin(2f * Mathf.PI * chimeNotes[c] * 2f * noteT) * 0.02f * env;
                }
            }

            samples[i] = Mathf.Clamp(sample * loopEnv, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Ambient", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
