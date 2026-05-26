using System;
using UnityEngine;

/// <summary>
/// Serializable save data container. Add fields as needed.
/// Note: This is serializable by JsonUtility.
/// </summary>
[Serializable]
public class SaveData
{
    public int saveVersion = 1;
    public string shopName;
    public int currentDay = 1;
    public DayOfWeek currentDayOfWeek = DayOfWeek.Senin;
    public int money = 0;
    public WeatherType todayWeather = WeatherType.Cerah;
    public WeatherType tomorrowWeather = WeatherType.Cerah;
    public DateTime lastPlayed;
    public string lastDrawnTextureFile; // filename used by TextureUtils (so we can reload phenotype)
}

public enum DayOfWeek { Senin = 0, Selasa = 1, Rabu = 2, Kamis = 3, Jumat = 4 }
public enum WeatherType { Cerah = 0, Berawan = 1, Gerimis = 2, Hujan = 3, Badai = 4 }