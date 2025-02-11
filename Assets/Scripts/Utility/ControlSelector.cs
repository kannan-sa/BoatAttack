using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ControlSelector : MonoBehaviour
{
    public float delay = 0;
    public GameObject control;
    public GameObject otherControl;
    private void OnEnable()
    {
        if (!control)
            return;

        if (delay > 0)
            StartCoroutine(DelaySelectControl(control));
        else
            EventSystem.current.SetSelectedGameObject(control);
    }

    private void OnDisable()
    {
        if (!otherControl)
            return;

        if (delay > 0)
            StartCoroutine(DelaySelectControl(otherControl));
        else
            EventSystem.current.SetSelectedGameObject(otherControl);
    }

    private IEnumerator DelaySelectControl(GameObject control)
    {
        yield return new WaitForSeconds(delay);
        EventSystem.current.SetSelectedGameObject(control);
    }
}
