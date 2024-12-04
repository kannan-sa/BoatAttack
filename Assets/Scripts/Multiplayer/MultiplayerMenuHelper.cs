using UnityEngine;
using System.Collections;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using System.Collections.Concurrent;
using Unity.Netcode;
using TMPro;

public class MultiplayerMenuHelper : MonoBehaviour
{
    [SerializeField]
    private string playerName;
    [SerializeField]
    private string lobbyName;

    public Animator menuAnimator;

    [Header("Labels")]
    public TextMeshProUGUI playerNameLabel;


    [Header("Panels")]
    public Transform lobbyContent;
    public Transform playerContent;
    public PlayerView playerView;

    private bool isServer;
    private string lobbyID;
    private Lobby currentLobby;

    public ProjectSceneManager projectSceneManager;

    private ConcurrentQueue<string> createdLobbyIds = new ConcurrentQueue<string>();

    public string PlayerName { get => playerName; set { playerName = value; } }
    public string LobbyName { get => lobbyName; set { lobbyName = value; } }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        await SignInAnonymouslyAsync();
    }

    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");

            // Shows how to get the playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}, {AuthenticationService.Instance.PlayerName}");

        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    #region Multiplayer Services - Netcode
    public void StartGame()
    {
        Debug.Log(isServer ? "Creating Game" : "Joining Game");

        if (isServer)
            CreateGame();
            //CreateRelay();
        else
            JoinGame();
    }

    public void CreateGame()
    {
        NetworkManager.Singleton.StartHost();
        UpdateJoiningCode("TEST");
        StartCoroutine(StatusRoutine());
    }

    public void JoinGame()
    {
        NetworkManager.Singleton.StartClient();
    }

    private IEnumerator StatusRoutine()
    {
        bool clientsConnected = false;
        while (!clientsConnected)
        {
            //status.text = $"Connected clients {NetworkManager.Singleton.ConnectedClients.Count - 1}";

            clientsConnected = currentLobby.Players.Count == NetworkManager.Singleton.ConnectedClients.Count;
            yield return new WaitForSeconds(.5f);
        }

        Debug.Log($"All clients connected lets start the game..");
        projectSceneManager.LoadGameScene();
    }
    #endregion

    #region Multiplayer Services - Lobby 

    public async void CreateLobby()
    {
        CreateLobbyOptions options = new CreateLobbyOptions()
        {
            IsPrivate = false,
            Player = GetPlayer(),
            Data = new Dictionary<string, DataObject>()
            {
                {"JoinCode", new DataObject(DataObject.VisibilityOptions.Public, string.Empty) }
            }
        };
        options.IsPrivate = false;

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 4, options);
        currentLobby = lobby;
        lobbyID = lobby.Id;
        Debug.Log("Lobby created " + lobby.Name);

        // Heartbeat the lobby every 15 seconds.
        StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));
        StartCoroutine(PollLobbyForUpdates(lobby.Id, 1.1f));
        createdLobbyIds.Enqueue(lobby.Id);

        //Switch to BoatSelection
        menuAnimator.SetTrigger("Next");
    }

    public async void JoinLobby()
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions()
            {
                Player = GetPlayer(),
            };

            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID, options);
            currentLobby = joinedLobby;
            lobbyID = joinedLobby.Id;
            Debug.Log($"Joined lobby id :{joinedLobby.Id}");
            lobbyName = joinedLobby.Name;

            StartCoroutine(PollLobbyForUpdates(lobbyID, 1.1f));
            menuAnimator.SetTrigger("Next");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void UpdateJoiningCode(string joinCode)
    {
        UpdateLobbyOptions options = new UpdateLobbyOptions()
        {
            Data = new Dictionary<string, DataObject>()
            {
                {"JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
            }
        };
        Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(lobbyID, options);

    }

    void OnApplicationQuit()
    {
        while (createdLobbyIds.TryDequeue(out var lobbyId))
        {
            LobbyService.Instance.DeleteLobbyAsync(lobbyId);
        }
    }

    #endregion

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>() {
                    { "playerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                }
        };
    }

    private void ShowPlayers(Lobby lobby, bool forServer = false)
    {
        playerNameLabel.text = lobbyName;
        playerContent.DestroyChildren();
        playerContent.Populate(playerView, lobby.Players);
    }

    IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (enabled)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    IEnumerator PollLobbyForUpdates(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        bool gamestarted = false;
        string joinCode = string.Empty;
        while (!gamestarted)
        {
            LobbyService.Instance.GetLobbyAsync(lobbyId);
            yield return delay;
            ShowPlayers(currentLobby, isServer);

            if (!isServer)
            {
                gamestarted = !string.IsNullOrEmpty(joinCode = currentLobby.Data["JoinCode"].Value);
                Debug.Log($"Join Code - {joinCode}");
            }
        }

        if (!isServer)
            StartGame();
        //JoinRelay(joinCode);
    }
}
