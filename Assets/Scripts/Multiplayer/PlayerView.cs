using System;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerView : MonoBehaviour, IView<Player>
{
    [SerializeField]
    private Text playerName;

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

    public void Kick()
    {
        kickEvent?.Invoke(playerID);
    }
}
