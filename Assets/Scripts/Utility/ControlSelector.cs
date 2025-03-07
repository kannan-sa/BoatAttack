using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ControlSelector : MonoBehaviour
{
    public float delay = 0;
    public GameObject control;
    public GameObject otherControl;

    public GameObject[] controls;

    public Selectable[] selectables;

    public bool skipControlsOnDisable = true;

    private void OnEnable()
    {
        if (delay > 0)
            StartCoroutine(DelaySelectControl(control));
        else
            SelectControl(control);
    }

    private void OnDisable()
    {
         SelectControl(otherControl, skipControlsOnDisable);
    }

    private IEnumerator DelaySelectControl(GameObject control)
    {
        yield return new WaitForSeconds(delay);
        SelectControl(control);
    }

    private void SelectControl(GameObject control, bool skipControls = false)
    {
        if (control != null)
            EventSystem.current.SetSelectedGameObject(control);

        if (skipControls)
            return;

        foreach (var ctrl in controls)
        {
            if (ctrl == null)
                continue;

            if (ctrl.activeSelf) { 
                EventSystem.current.SetSelectedGameObject(ctrl);
                break;
            }
        }

        foreach (var selectable in selectables)
        {
            if (selectable == null)
                continue;

            if (selectable.interactable)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
                break;
            }
        }
    }
}
