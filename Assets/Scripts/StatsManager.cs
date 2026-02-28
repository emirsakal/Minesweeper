using System.Collections.Generic;
using UnityEngine;

public struct DifficultyStats
{
    public int gamesPlayed;
    public int gamesWon;
    public float winRate;
    public int currentStreak;
    public int bestStreak;
}

public class StatsManager : MonoBehaviour
{
    private const string KeyPrefix = "Minesweeper_";
    private const int MaxBestTimes = 5;

    public bool RecordGame(string difficulty, bool won, int timeSeconds)
    {
        bool isNewRecord = false;

        int played = GetInt(difficulty, "GamesPlayed") + 1;
        SetInt(difficulty, "GamesPlayed", played);

        int wonCount = GetInt(difficulty, "GamesWon");
        if (won) wonCount++;
        SetInt(difficulty, "GamesWon", wonCount);

        int currentStreak = GetInt(difficulty, "CurrentStreak");
        int bestStreak = GetInt(difficulty, "BestStreak");

        if (won)
        {
            currentStreak++;
            if (currentStreak > bestStreak)
                bestStreak = currentStreak;
        }
        else
        {
            currentStreak = 0;
        }

        SetInt(difficulty, "CurrentStreak", currentStreak);
        SetInt(difficulty, "BestStreak", bestStreak);

        if (won)
        {
            isNewRecord = TryAddBestTime(difficulty, timeSeconds);
        }

        PlayerPrefs.Save();
        return isNewRecord;
    }

    public DifficultyStats GetStats(string difficulty)
    {
        int played = GetInt(difficulty, "GamesPlayed");
        int won = GetInt(difficulty, "GamesWon");

        return new DifficultyStats
        {
            gamesPlayed = played,
            gamesWon = won,
            winRate = played > 0 ? (float)won / played * 100f : 0f,
            currentStreak = GetInt(difficulty, "CurrentStreak"),
            bestStreak = GetInt(difficulty, "BestStreak")
        };
    }

    public List<int> GetBestTimes(string difficulty)
    {
        var times = new List<int>();
        for (int i = 0; i < MaxBestTimes; i++)
        {
            int t = GetInt(difficulty, "BestTime" + i, -1);
            if (t >= 0) times.Add(t);
        }
        return times;
    }

    public void ResetStats(string difficulty)
    {
        DeleteKey(difficulty, "GamesPlayed");
        DeleteKey(difficulty, "GamesWon");
        DeleteKey(difficulty, "CurrentStreak");
        DeleteKey(difficulty, "BestStreak");
        for (int i = 0; i < MaxBestTimes; i++)
            DeleteKey(difficulty, "BestTime" + i);
        PlayerPrefs.Save();
    }

    public void ResetAllStats()
    {
        ResetStats("Easy");
        ResetStats("Medium");
        ResetStats("Hard");
    }

    private bool TryAddBestTime(string difficulty, int timeSeconds)
    {
        List<int> times = GetBestTimes(difficulty);

        if (times.Count >= MaxBestTimes && timeSeconds >= times[MaxBestTimes - 1])
            return false;

        int insertIdx = times.Count;
        for (int i = 0; i < times.Count; i++)
        {
            if (timeSeconds < times[i])
            {
                insertIdx = i;
                break;
            }
        }

        times.Insert(insertIdx, timeSeconds);

        if (times.Count > MaxBestTimes)
            times.RemoveAt(MaxBestTimes);

        for (int i = 0; i < times.Count; i++)
            SetInt(difficulty, "BestTime" + i, times[i]);

        return insertIdx == 0;
    }

    private string Key(string difficulty, string stat)
    {
        return KeyPrefix + difficulty + "_" + stat;
    }

    private int GetInt(string difficulty, string stat, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(Key(difficulty, stat), defaultValue);
    }

    private void SetInt(string difficulty, string stat, int value)
    {
        PlayerPrefs.SetInt(Key(difficulty, stat), value);
    }

    private void DeleteKey(string difficulty, string stat)
    {
        PlayerPrefs.DeleteKey(Key(difficulty, stat));
    }
}
