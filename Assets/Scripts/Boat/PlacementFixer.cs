using BoatAttack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacementFixer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(FixRoutine(transform.position, transform.rotation));
    }

    IEnumerator FixRoutine(Vector3 pos, Quaternion rot)
    {
        while (Mathf.FloorToInt(pos.x) == Mathf.FloorToInt(transform.position.x) && !RaceManager.RaceStarted)
        {
            yield return null;
        }

        if (!RaceManager.RaceStarted)
        {
            transform.position = pos;
            transform.rotation = rot;
        }
    }
}
