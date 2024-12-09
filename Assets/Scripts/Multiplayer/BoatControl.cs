using BoatAttack;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BoatControl : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();


        int index = (int)OwnerClientId;
        var boat = RaceManager.RaceData.boats[index];

        gameObject.name = boat.boatName;
        Boat boatController = GetComponent<Boat>();
        boat.SetController(gameObject, boatController);
        boatController.Setup(index + 1, boat.human, boat.livery);

        if (!IsOwner)
            return;
        RaceManager.Instance._boatTimes.Add(index, 0f);
        StartCoroutine(SetupRoutine(index));
        StartCoroutine(RaceManager.BeginRace());
    }

    private IEnumerator SetupRoutine(int index)
    {
        yield return RaceManager.Instance.StartCoroutine(RaceManager.CreatePlayerUi(index));
        RaceManager.SetupCamera(index); // setup camera for player 1
    }
}
