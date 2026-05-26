using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game manager (Singleton) — manages state transitions and global data.
/// Uses event-based architecture to notify listeners when state or key values change.
/// </summary>
public enum GameState { MainMenu, Cutscene, Lobby, Dialog, Drawing, Dyeing, Drying, Result }

public class GameManager : Singleton<GameManager>
{
    // Events
    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnMoneyChanged;
    public event Action<SaveData> OnSaveLoaded;

    public SaveData currentSave;
    public CustomerOrder currentOrder; // simple DTO for current customer/order
    public float batikAccuracy;
    public float colorAccuracy;
    public float dryOpacity;

    // Time constants: 8 jam game = 20 menit nyata => 1200 detik real / 8 jam = 150s per game hour
    public const float REAL_SECONDS_PER_GAME_DAY = 20f * 60f;
    public const float GAME_HOURS_PER_DAY = 8f; // 9 -> 17
    public const float SHOP_OPEN_HOUR = 9f;
    public const float SHOP_CLOSE_HOUR = 17f;

    public float currentGameHour = SHOP_OPEN_HOUR;
    public bool isPaused = false;

    private GameState _state = GameState.MainMenu;
    public GameState State
    {
        get => _state;
        private set
        {
            _state = value;
            OnGameStateChanged?.Invoke(_state);
        }
    }

    protected override bool PersistBetweenScenes => true;

    protected override void Awake()
    {
        base.Awake();
    }

    public void StartNewGame(string shopName)
    {
        currentSave = new SaveData()
        {
            shopName = shopName,
            currentDay = 1,
            currentDayOfWeek = DayOfWeek.Senin,
            money = 0,
            todayWeather = WeatherType.Cerah,
            tomorrowWeather = WeatherType.Cerah
        };

        //Debug.Log(JsonUtility.ToJson(currentSave, true)); //Testing

        try
        {
            SaveSystem.Instance.SaveGame(currentSave);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SaveSystem.SaveGame error: {ex}");
            return;
        }

        LoadGame(currentSave);

        //LoadScene("01_Cutscene"); //Jika sudah ada cutscene, bisa langsung ke sana. Untuk sekarang langsung ke lobby saja.


    }

    public void LoadGame(SaveData data)
    {
        currentSave = data;
        OnSaveLoaded?.Invoke(data);
        currentGameHour = SHOP_OPEN_HOUR;
        LoadScene("02_Lobby");
        Debug.Log(JsonUtility.ToJson(currentSave, true));
    }

    public void AddMoney(int amount)
    {
        if (currentSave == null) return;
        currentSave.money += amount;
        OnMoneyChanged?.Invoke(currentSave.money);
        SaveSystem.Instance.SaveGame(currentSave);
    }

    public void ChangeState(GameState next)
    {
        State = next;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    /// <summary>
    /// Move to a named scene and update GameState accordingly (optional)
    /// </summary>
    public void LoadScene(string sceneName, GameState? forcedState = null)
    {
        SceneManager.LoadScene(sceneName);
        if (forcedState.HasValue) State = forcedState.Value;
    }

    /// <summary>
    /// End the day: update day index, rotate day-of-week, set new weather forecast, save.
    /// </summary>
    public void EndDay()
    {
        if (currentSave == null) return;
        currentSave.currentDay++;
        currentSave.currentDayOfWeek = (DayOfWeek)(((int)currentSave.currentDayOfWeek + 1) % 5);
        currentSave.todayWeather = currentSave.tomorrowWeather;
        currentSave.tomorrowWeather = WeatherSystem.Instance.GenerateWeather(currentSave.currentDayOfWeek);
        SaveSystem.Instance.SaveGame(currentSave);
    }
}