using Unity.Services.Lobbies.Models;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LobbyView : MonoBehaviour, IView<Lobby>
{
    [SerializeField]
    private TextMeshProUGUI LobbyName;

    public Toggle selectionToggle;

    public string lobbyID;

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
