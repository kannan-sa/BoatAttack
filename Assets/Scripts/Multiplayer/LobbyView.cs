using Unity.Services.Lobbies.Models;
using UnityEngine;
using TMPro;

public class LobbyView : MonoBehaviour, IView<Lobby>
{
    [SerializeField]
    private TextMeshProUGUI LobbyName;

    private string lobbyID;

    public StringEvent selectLobby;

    public void Initialize(Lobby lobby) {
        LobbyName.text = lobby.Name;
        lobbyID = lobby.Id;
    }

    public void SelectLobby(bool state)
    {
        if(state)
            selectLobby?.Invoke(lobbyID);
    }
}
