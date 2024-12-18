using UnityEditor;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using Unity.Multiplayer.Playmode;
#endif

[ExecuteInEditMode]
public class MenuSetter : MonoBehaviour
{
    public bool setInputTexts;
    public TMP_InputField playerName;
    public TMP_InputField[] inputFields;
    public string[] values;

    public GameObject[] ActiveObjects;
    public GameObject[] NonActiveObjects;


#if UNITY_EDITOR
    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnStateChange;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnStateChange;
    }

    private void OnStateChange(PlayModeStateChange change)
    {
        switch (change)
        {
            case PlayModeStateChange.ExitingEditMode:
                Reverse(); 
                break;

            case PlayModeStateChange.EnteredEditMode:
                Setup();
                break;
        }
    }

    private void Start()
    {
        if (setInputTexts)
        {
            for (int i = 0; i < inputFields.Length && i < values.Length; i++)
                inputFields[i].text = values[i];

#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
            if (playerName)
                playerName.text = string.Join(",", CurrentPlayer.ReadOnlyTags());
#endif
        }
    }
#endif

    [ContextMenu("Editor Mode")]
    private void Setup()
    {
        SetActive(false);
    }

    [ContextMenu("Play Mode")]
    private void Reverse()
    {
        SetActive(true);
    }

    private void SetActive(bool active)
    {
        foreach (var item in ActiveObjects)
            item.SetActive(active);

        foreach (var item in NonActiveObjects)
            item.SetActive(!active);
    }
}
