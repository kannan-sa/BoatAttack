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
using UnityEngine.InputSystem;
using System.Linq;
using BoatAttack;

public class MultiplayerMenuHelper : MonoBehaviour
{
    [SerializeField]
    private string playerName;
    [SerializeField]
    private string lobbyName;

    [Header("Labels")]
    public TextMeshProUGUI playerNameLabel;
    public TextMeshProUGUI lobbyNameLabel;
    public TextMeshProUGUI boatTitleLabel;
    public TextMeshProUGUI status;
    public TextMeshProUGUI caption;
    [Header("Panels")]
    public GameObject statusPanel;
    public Transform lobbyContent;
    public Transform playerContent;
    public LobbyView lobbyView;
    public PlayerView playerView;
    [Header("Events")]
    public StringEvent selectLobby;
    public StringEvent kickPlayer;
    [Header("Controls")]
    public Animator menuAnimator;
    public ProjectSceneManager projectSceneManager;
    public GameObject boatPlayerName;
    public GameObject playerInputControl;
    public TMP_Dropdown inputControlOptions;

    private bool isServer;
    private bool canPollLobbies;
    private string lobbyID;
    private Lobby currentLobby;
    private ConcurrentQueue<string> createdLobbyIds = new ConcurrentQueue<string>();

    public string PlayerName { get => playerName; set { playerName = value; } }
    public string LobbyName { get => lobbyName; set { lobbyName = value; } }

    private void OnEnable()
    {
        selectLobby.AddListener(OnSelectLobby);
        kickPlayer.AddListener(OnKickPlayer);
    }

    private void OnDisable()
    {
        selectLobby.RemoveListener(OnSelectLobby);
        kickPlayer.RemoveListener(OnKickPlayer);
    }

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

    private void SetStatus(string text, float duration)
    {
        status.text = text;
        StartCoroutine(StatusRoutine(duration));
    }

    private IEnumerator StatusRoutine(float duration)
    {
        statusPanel.SetActive(true);
        yield return new WaitForSeconds(duration);
        statusPanel.SetActive(false);
    }

    private void SwitchToBoatSelection()
    {
        //Switch to BoatSelection
        boatTitleLabel.text = "MULTIPLAYER";
        boatPlayerName.SetActive(false);
        playerInputControl.SetActive(true);
        menuAnimator.SetTrigger("Next");
        canPollLobbies = false;
        inputControlOptions.ClearOptions();

        //List<string> gamePadNames = new List<string> { "Gamepad 1", "Gamepad 2" };
        List<string> gamePadNames = Gamepad.all.Select(g => g.name).ToList();

        if (gamePadNames.Any())
            inputControlOptions.AddOptions(gamePadNames.Select(g => new TMP_Dropdown.OptionData(g)).ToList());
        else
            StartCoroutine(SetCaption("No Gamepads"));
    }

    private IEnumerator SetCaption(string text)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        inputControlOptions.interactable = false;
        inputControlOptions.captionText.text = text;
    }    

    #region Events
    private void OnSelectLobby(string lobby)
    {
        Debug.Log($"Selected Lobby is {lobby}");
        lobbyID = lobby;
    }

    private void OnKickPlayer(string playerID)
    {

    }
    #endregion

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
        if (RaceManager.RaceData.game != RaceManager.GameType.Multiplayer)
            return;

        if(!isServer)
        {
            SetStatus("Only Server can start race", 4f);
            return;
        }

        NetworkManager.Singleton.StartHost();
        UpdateLobbyData("TEST");
        StartCoroutine(StatusRoutine());
    }

    public void JoinGame()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void PollLobbies()
    {
        canPollLobbies = true;
        StartCoroutine(PollLobbies(1.5f));
    }

    public void PollPlayers()
    {

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
        if (string.IsNullOrEmpty(playerName))
        {
            SetStatus("Enter Player Name", 6f);
            return;
        }

        if (string.IsNullOrEmpty(lobbyName))
        {
            SetStatus("Enter Lobby Name", 6f);
            return;
        }

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
        isServer = true;
        Debug.Log("Lobby created " + lobby.Name);

        // Heartbeat the lobby every 15 seconds.
        StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));
        StartCoroutine(PollLobbyForUpdates(lobby.Id, 1.1f));
        createdLobbyIds.Enqueue(lobby.Id);

        SwitchToBoatSelection();
    }

    public async void JoinLobby()
    {
        if (string.IsNullOrEmpty(playerName))
        {
            SetStatus("Enter Player Name", 6f);
            return;
        }

        if (string.IsNullOrEmpty(lobbyID))
        {
            SetStatus("Select Any Lobby", 6f);
            return;
        }

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
            SwitchToBoatSelection();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void SearchLobby()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder>()
            {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbies = await LobbyService.Instance.QueryLobbiesAsync(options);
            lobbyContent.DestroyChildren();
            LobbyView[] lobbyViews = lobbyContent.Populate(lobbyView, lobbies.Results);
            foreach (var view in lobbyViews)
                view.selectionToggle.isOn = view.lobbyID == lobbyID;
            //...
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void UpdateLobbyData(string joinCode)
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
        //playerNameLabel.text = playerName;
        lobbyNameLabel.text = lobbyName;
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

    IEnumerator PollLobbies(float waitTimeSeconds)
    {
        while(canPollLobbies)
        {
            SearchLobby();
            yield return new WaitForSecondsRealtime(waitTimeSeconds);
        }
    }
}
