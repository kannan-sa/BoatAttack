using UnityEngine;

[CreateAssetMenu(fileName = "FloatEvent", menuName = "Events/FloatEvent")]
public class FloatEvent : GameEvent<float> {
    public string persistanceKey = string.Empty;
    public bool log;
    private void OnEnable()
    {
        if(!string.IsNullOrEmpty(persistanceKey))
        {
            value = PlayerPrefs.GetFloat(persistanceKey, 0f);
            if (log)
                Debug.Log($" reading {name} = {value}");
        }
    }

    public void OnDisable()
    {
        if (!string.IsNullOrEmpty(persistanceKey))
        {
            PlayerPrefs.SetFloat(persistanceKey, value);
            if (log)
                Debug.Log($" writing {name} = {value}");
        }
    }
}
