using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class MenuSetter : MonoBehaviour
{
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
