using System;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class PlayerView : MonoBehaviour, IView<Player>
{
    [SerializeField]
    private TextMeshProUGUI playerName;
    [SerializeField]
    private TextMeshProUGUI playerStatus;

    public Action KickPlayer;

    [SerializeField]
    private GameObject KickOption;
    private string playerID;

    //public PlayerDetails playerDetails;

    public StringEvent kickEvent;

    public void Initialize(Player player) {
        playerName.text = player.Data["playerName"].Value;
        playerID = player.Id;
        //KickOption.SetActive(playerDetails.IsHost);
    }

    public void Initialize(PlayerStatus player) {
        playerName.text = player.boatName.Value.ToString();
        playerStatus.text = player.status.Value ? "Ready" : "Not Ready";
        //playerID = player.Id;
        //KickOption.SetActive(playerDetails.IsHost);
    }

    

    public void Kick()
    {
        kickEvent?.Invoke(playerID);
    }
}
