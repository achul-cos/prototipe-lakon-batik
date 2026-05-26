using UnityEngine;

/// <summary>
/// Generic Singleton base class for Unity MonoBehaviours.
/// - Protects against multiple instances.
/// - Default behaviour: instance persists across scenes (DontDestroyOnLoad).
/// Derived classes may override PersistBetweenScenes to change behavior.
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// If true (default) the singleton GameObject will not be destroyed on scene load.
    /// Override in derived classes to change.
    /// </summary>
    protected virtual bool PersistBetweenScenes => true;

    /// <summary>
    /// Global instance accessor. If not present in scene, this will try to find one.
    /// If still null, it will create a new GameObject with the component.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton<{typeof(T)}>] Instance requested after application quit. Returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject($"{typeof(T).Name} (Singleton)");
                        _instance = go.AddComponent<T>();
                        var singleton = _instance as Singleton<T>;
                        if (singleton != null && singleton.PersistBetweenScenes)
                            DontDestroyOnLoad(go);
                        Debug.Log($"[Singleton<{typeof(T)}>] Created new instance.");
                    }
                }
                return _instance;
            }
        }
    }

    /// <summary>
    /// Awake: ensure uniqueness and optionally persist between scenes.
    /// Derived classes MUST call base.Awake() if they override.
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            if (PersistBetweenScenes)
                DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton<{typeof(T)}>] Duplicate instance found on {gameObject.name}, destroying duplicate.");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
}