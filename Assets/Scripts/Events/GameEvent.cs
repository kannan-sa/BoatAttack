using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "GameEvent", menuName = "Events/GameEvent")]
public class GameEvent : ScriptableObject
{
    [SerializeField]
    private UnityEvent m_Event;

    public void AddListener(UnityAction action)
    {
        m_Event.AddListener(action);
    }

    public void RemoveListener(UnityAction action)
    {
        m_Event.RemoveListener(action);
    }

    public void Invoke()
    {
        m_Event.Invoke();
    }
}

public abstract class GameEvent<T> : ScriptableObject {
    [SerializeField]
    private UnityEvent<T> m_Event;

    public void AddListener(UnityAction<T> action) {
        m_Event.AddListener(action);
    }

    public void RemoveListener(UnityAction<T> action) {
        m_Event.RemoveListener(action);
    }

    public void Invoke(T value) {
        m_Event.Invoke(value);
    }
}
