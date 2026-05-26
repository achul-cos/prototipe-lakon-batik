using UnityEngine;
using System;

/// <summary>
/// Handles conversion between real-time and game-time hours.
/// Raises event when hour changes and when shop is closed.
/// </summary>
public class TimeManager : Singleton<TimeManager>
{
    public event Action<float> OnHourChanged;
    public event Action OnShopClosed;

    private float _gameHoursPerSecond;
    private bool _isRunning = true;

    protected override void Awake()
    {
        base.Awake();
        // We map 8 game-hours -> REAL_SECONDS_PER_GAME_DAY real seconds
        _gameHoursPerSecond = GameManager.GAME_HOURS_PER_DAY / GameManager.REAL_SECONDS_PER_GAME_DAY;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (GameManager.Instance.isPaused || !_isRunning) return;

        GameManager.Instance.currentGameHour += _gameHoursPerSecond * Time.deltaTime;
        OnHourChanged?.Invoke(GameManager.Instance.currentGameHour);

        if (GameManager.Instance.currentGameHour >= GameManager.SHOP_CLOSE_HOUR)
        {
            // once
            _isRunning = false;
            OnShopClosed?.Invoke();
        }
    }

    public string GetTimeString()
    {
        int hour = Mathf.FloorToInt(GameManager.Instance.currentGameHour);
        int minute = Mathf.FloorToInt((GameManager.Instance.currentGameHour - hour) * 60f);
        return $"{hour:00}:{minute:00}";
    }

    public void ResetForNewDay()
    {
        GameManager.Instance.currentGameHour = GameManager.SHOP_OPEN_HOUR;
        _isRunning = true;
    }
}