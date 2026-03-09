using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
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
    private float musicTargetVolume = 0.15f;
    private bool musicEnabled = true;

    private const int SampleRate = 44100;

    private void Awake()
    {
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
    }

    // ========== SFX PUBLIC API ==========

    public void PlayReveal()
    {
        sfxSource.PlayOneShot(revealClip, 0.4f);
    }

    public void PlaySweep()
    {
        sfxSource.PlayOneShot(sweepClip, 0.5f);
    }

    public void PlayFlag()
    {
        sfxSource.PlayOneShot(flagClip, 0.5f);
    }

    public void PlayUnflag()
    {
        sfxSource.PlayOneShot(unflagClip, 0.5f);
    }

    public void PlayExplosion()
    {
        sfxSource.PlayOneShot(explosionClip, 0.7f);
    }

    public void PlayWin()
    {
        sfxSource.PlayOneShot(winClip, 0.6f);
    }

    public void PlayLose()
    {
        sfxSource.PlayOneShot(loseClip, 0.5f);
    }

    // ========== MUSIC PUBLIC API ==========

    public void StartMusic()
    {
        if (!musicEnabled) return;
        musicTargetVolume = 0.15f;
        if (!musicSource.isPlaying)
            musicSource.Play();
        FadeMusicTo(musicTargetVolume, 2f);
    }

    public void StopMusic()
    {
        FadeMusicTo(0f, 1f, true);
    }

    public void SetMusicVolume(float volume)
    {
        musicTargetVolume = volume;
        FadeMusicTo(volume, 0.5f);
    }

    public void ToggleMusic()
    {
        musicEnabled = !musicEnabled;
        if (musicEnabled)
            StartMusic();
        else
            StopMusic();
    }

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

        // Deterministic random for consistent music
        System.Random rng = new System.Random(42);

        // Pre-calculate melody note events
        float[] melodyFreqs = { 262f, 294f, 330f, 392f, 440f }; // C4 D4 E4 G4 A4
        float[] chimeFreqs = { 523f, 659f, 784f }; // C5 E5 G5

        // Generate melody schedule: note start times and frequencies
        int melodyCount = 50;
        float[] melodyStarts = new float[melodyCount];
        float[] melodyNotes = new float[melodyCount];
        float[] melodyDurations = new float[melodyCount];
        float melodyTime = 1f;
        for (int m = 0; m < melodyCount; m++)
        {
            melodyStarts[m] = melodyTime;
            melodyNotes[m] = melodyFreqs[rng.Next(melodyFreqs.Length)];
            melodyDurations[m] = 0.3f + (float)rng.NextDouble() * 0.2f; // 0.3-0.5s
            melodyTime += melodyDurations[m] + 0.5f + (float)rng.NextDouble() * 1f; // 0.5-1.5s gap
            if (melodyTime > duration - 3f) break;
        }

        // Generate chime schedule
        int chimeCount = 12;
        float[] chimeStarts = new float[chimeCount];
        float[] chimeNotes = new float[chimeCount];
        float chimeTime = 2.5f;
        for (int c = 0; c < chimeCount; c++)
        {
            chimeStarts[c] = chimeTime;
            chimeNotes[c] = chimeFreqs[rng.Next(chimeFreqs.Length)];
            chimeTime += 3f + (float)rng.NextDouble() * 3f; // 3-6s gap
            if (chimeTime > duration - 3f) break;
        }

        float fadeZone = 2f; // fade in/out zone for smooth loop

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float sample = 0f;

            // Loop crossfade envelope: fade in first 2s, fade out last 2s
            float loopEnv = 1f;
            if (t < fadeZone)
                loopEnv = t / fadeZone;
            else if (t > duration - fadeZone)
                loopEnv = (duration - t) / fadeZone;

            // === PAD LAYER ===
            float padTremolo = 0.5f + 0.5f * Mathf.Sin(0.15f * 2f * Mathf.PI * t);
            sample += Mathf.Sin(2f * Mathf.PI * 131f * t) * 0.12f * padTremolo;  // C3
            sample += Mathf.Sin(2f * Mathf.PI * 196f * t) * 0.08f * padTremolo;  // G3
            sample += Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.06f * padTremolo;  // E3

            // Slow pad drift with secondary tremolo
            float padTremolo2 = 0.5f + 0.5f * Mathf.Sin(0.23f * 2f * Mathf.PI * t);
            sample += Mathf.Sin(2f * Mathf.PI * 98f * t) * 0.05f * padTremolo2;  // G2 sub

            // === MELODY LAYER ===
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

                    // Soft attack (20%) and decay (80%)
                    float env;
                    if (noteProgress < 0.2f)
                        env = noteProgress / 0.2f;
                    else
                        env = 1f - (noteProgress - 0.2f) / 0.8f;

                    env *= env; // smoother curve
                    sample += Mathf.Sin(2f * Mathf.PI * melodyNotes[m] * noteT) * 0.14f * env;
                }
            }

            // === CHIME LAYER ===
            for (int c = 0; c < chimeCount; c++)
            {
                if (chimeStarts[c] == 0f) break;
                float noteStart = chimeStarts[c];
                float chimeDur = 1.5f; // long ring

                if (t >= noteStart && t < noteStart + chimeDur)
                {
                    float noteT = t - noteStart;
                    float noteProgress = noteT / chimeDur;

                    // Very fast attack, long exponential decay
                    float env = Mathf.Exp(-noteProgress * 4f);
                    sample += Mathf.Sin(2f * Mathf.PI * chimeNotes[c] * noteT) * 0.06f * env;
                    // Add octave harmonic for shimmer
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
