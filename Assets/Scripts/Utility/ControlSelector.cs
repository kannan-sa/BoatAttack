using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ControlSelector : MonoBehaviour
{
    public float delay = 0;
    public GameObject control;
    private void OnEnable()
    {
        if (delay > 0)
            StartCoroutine(DelaySelectControl());
        else
            EventSystem.current.SetSelectedGameObject(control);
    }

    private IEnumerator DelaySelectControl()
    {
        yield return new WaitForSeconds(delay);
        EventSystem.current.SetSelectedGameObject(control);
    }
}
