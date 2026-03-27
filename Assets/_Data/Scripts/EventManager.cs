using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phải Awake trước các script gọi Instance.Subscribe trong Awake (ScoreManager, v.v.).
/// </summary>
[DefaultExecutionOrder(-500)]
public class EventManager : Singleton<EventManager>
{
    private Dictionary<GameEvent, Action<object>> eventDictionary = new Dictionary<GameEvent, Action<object>>();

    public void Subscribe(GameEvent eventType, Action<object> listener)
    {
        if (eventDictionary.ContainsKey(eventType))
        {
            eventDictionary[eventType] += listener;
        }
        else
        {
            eventDictionary.Add(eventType, listener);
        }
    }

    public void Unsubscribe(GameEvent eventType, Action<object> listener)
    {
        if (!eventDictionary.ContainsKey(eventType)) return;

        eventDictionary[eventType] -= listener;
    }

    public void Notify(GameEvent eventType, object data = null)
    {
        if (!eventDictionary.ContainsKey(eventType)) return;

        eventDictionary[eventType]?.Invoke(data);
    }
}

public enum GameEvent
{
    PlayerDied,
    EnemySpawned,
    ItemCollected,
    LevelCompleted,
    GameOver,
    UpdateTimer,
    PlatformPassed,
    QuizTriggered,
    QuizAnswered,
    QuizClosed,
    GameStarted,
    GamePaused,
    GameResumed,
    ScoreChanged,
    PlayerJump,
    PlayerLand,
    MajorSelected,
}