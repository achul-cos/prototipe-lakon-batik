using UnityEngine;

/// <summary>
/// Simple weather system. Deterministic if seed provided (helpful for debugging).
/// Also provides multiplier affecting customer count.
/// </summary>
public class WeatherSystem : Singleton<WeatherSystem>
{
    private System.Random _rng = new System.Random();

    // multipliers for customers (index order matches WeatherType enum)
    private readonly float[] weatherMultiplier = { 1.0f, 0.85f, 0.65f, 0.45f, 0.25f };
    private readonly float[] dayMultiplier = { 0.7f, 0.75f, 0.85f, 0.95f, 1.0f }; // senin..jumat

    protected override void Awake()
    {
        base.Awake();
    }

    public void SetSeed(int seed)
    {
        _rng = new System.Random(seed);
    }

    /// <summary>
    /// Simple weather generator. Tunable thresholds here.
    /// </summary>
    public WeatherType GenerateWeather(DayOfWeek day)
    {
        double roll = _rng.NextDouble();
        if (roll < 0.40) return WeatherType.Cerah;
        if (roll < 0.65) return WeatherType.Berawan;
        if (roll < 0.80) return WeatherType.Gerimis;
        if (roll < 0.93) return WeatherType.Hujan;
        return WeatherType.Badai;
    }

    public int CalculateCustomerCount(DayOfWeek day, WeatherType weather)
    {
        float baseCount = UnityEngine.Random.Range(5f, 16f);
        float wMult = weatherMultiplier[(int)weather];
        float dMult = dayMultiplier[(int)day];
        int result = Mathf.RoundToInt(baseCount * wMult * dMult);
        return Mathf.Clamp(result, 1, 15);
    }
}