using BoatAttack;
using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BoatControl : NetworkBehaviour
{
    private bool endCheck;
    public override void OnNetworkSpawn()
    {
        int index = (int)OwnerClientId;
        StartCoroutine(SetupRoutine(index));
    }

    //Need to find exact fix for clients position reseting to zero..
    private IEnumerator SetupRoutine(int index)
    {
        Debug.Log($"spawned {OwnerClientId} @ {transform.position}");

        //if(IsOwner)
        //    StartCoroutine(FixRoutine(transform.position, transform.rotation));

        //if(!IsHost && IsOwner) 
        //     yield return StartCoroutine(SetupWaypoints());

        PlayerStatus playerStatus = NetworkRaceManager.playerStats[index];

        var boat = RaceManager.RaceData.boats[index];

        boat.boatName = playerStatus.boatName.Value.ToString();
        boat.livery.primaryColor = ConstantData.GetPaletteColor(playerStatus.primaryColor.Value);
        boat.livery.trimColor = ConstantData.GetPaletteColor(playerStatus.trimColor.Value);

        gameObject.name = boat.boatName;

        Boat boatController = GetComponent<Boat>();
        boat.SetController(gameObject, boatController);
        boatController.Setup(index + 1, boat.human, boat.livery, IsOwner);
        RaceManager.Instance._boatTimes.Add(index, 0f);

        if (!IsOwner)
            yield break;

        yield return RaceManager.Instance.StartCoroutine(RaceManager.CreatePlayerUi(index));
        RaceManager.SetupCamera(index); // setup camera for player 1
        yield return StartCoroutine(RaceManager.BeginRace());
        endCheck = true;
    }

    private IEnumerator SetupWaypoints()
    {
        while (WaypointGroup.Instance == null) // TODO need to re-write whole game loading/race setup logic as it is dirty
        {
            yield return null;
        }
        WaypointGroup.Instance.Setup(RaceManager.RaceData.reversed);

        yield return new WaitForSeconds(3);
    }

    IEnumerator FixRoutine(Vector3 pos, Quaternion rot)
    {
        while (Mathf.FloorToInt(pos.x) == Mathf.FloorToInt(transform.position.x) && !endCheck)
        {
#if DEBUG_ENABLED
            Debug.Log($" Same in {OwnerClientId}");
#endif
            yield return null;
        }

        if (!endCheck)
        {
            transform.position = pos;
            transform.rotation = rot;
        }
    }

}
