using BoatAttack;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

public class BoatControl : NetworkBehaviour
{
    private bool endCheck;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            StartCoroutine(FixRoutine(transform.position, transform.rotation));

            int index = (int)OwnerClientId;
        //LogAndSelect(index);

        PlayerStatus playerStatus = NetworkRaceManager.playerStats[index];

        var boat = RaceManager.RaceData.boats[index];

        boat.boatName = playerStatus.boatName.Value.ToString();
        boat.livery.primaryColor = ConstantData.GetPaletteColor(playerStatus.primaryColor.Value);
        boat.livery.trimColor = ConstantData.GetPaletteColor(playerStatus.trimColor.Value);

        gameObject.name = boat.boatName;

        Boat boatController = GetComponent<Boat>();
        boat.SetController(gameObject, boatController);
        boatController.Setup(index + 1, boat.human, boat.livery);
            RaceManager.Instance._boatTimes.Add(index, 0f);

        //LogAndSelect(index);

        if (IsOwner)
        {
            StartCoroutine(SetupRoutine(index));
        }
    }

    //Need to find exact fix for clients position reseting to zero..
    IEnumerator FixRoutine(Vector3 pos, Quaternion rot)
    {
        while (Mathf.FloorToInt(pos.x) == Mathf.FloorToInt(transform.position.x) && !endCheck)
        {
#if DEBUG_ENABLED
            Debug.Log($" Same in {OwnerClientId}");
#endif
            yield return null;
        }
        transform.position = pos;
        transform.rotation = rot;
    }


    private void LogAndSelect(int index)
    {
        if (IsOwner)
        {
            Debug.Log($"client {index} @ {transform.position}");

#if UNITY_EDITOR
            UnityEditor.Selection.activeGameObject = gameObject;
#endif
        }
    }

    private IEnumerator SetupRoutine(int index)
    {
        yield return RaceManager.Instance.StartCoroutine(RaceManager.CreatePlayerUi(index));
        RaceManager.SetupCamera(index); // setup camera for player 1
        yield return StartCoroutine(RaceManager.BeginRace());
        endCheck = true;
    }
}
