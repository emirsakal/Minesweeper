using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource sfxSource;

    private AudioClip revealClip;
    private AudioClip sweepClip;
    private AudioClip flagClip;
    private AudioClip unflagClip;
    private AudioClip explosionClip;
    private AudioClip winClip;
    private AudioClip loseClip;

    private const int SampleRate = 44100;

    private void Awake()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        GenerateAllClips();
    }

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

    // ========== PUBLIC API ==========

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

    // ========== CLIP GENERATION ==========

    private AudioClip GenerateRevealClip()
    {
        // Short click/tick - high frequency sine with fast fade
        float duration = 0.05f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        float freq = 1000f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = 1f - (float)i / sampleCount;
            envelope *= envelope; // quadratic fade out
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
        }

        AudioClip clip = AudioClip.Create("Reveal", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateSweepClip()
    {
        // Swoosh - frequency sweep low to high with light white noise
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
        // Two-tone beep: 600Hz then 800Hz
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
        // Reverse two-tone: 800Hz then 500Hz
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
        // Low boom: white noise + low sine, fast attack slow decay
        float duration = 0.4f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / sampleCount;

            // Fast attack (first 5%), slow exponential decay
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
        // Melodic rising three notes: C5 E5 G5
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

                // Smooth attack and release
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
        // Sad descending two notes: E4 C4
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

                // Slow fade out
                float envelope;
                if (noteProgress < 0.05f)
                    envelope = noteProgress / 0.05f;
                else
                    envelope = 1f - (noteProgress - 0.05f) / 0.95f;

                envelope *= envelope; // quadratic for sadder feel

                samples[i] = Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * envelope;
            }
        }

        AudioClip clip = AudioClip.Create("Lose", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
