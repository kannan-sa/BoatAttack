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
using BoatAttack;
using BoatAttack.UI;
using UnityEngine.UI;

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
    public StringEvent setPlayerName;
    public IntegerEvent setBoatType;
    public IntegerEvent setPrimaryColor;
    public IntegerEvent setTrimColor;
    [Header("Controls")]
    public Animator menuAnimator;
    public NetworkRaceManager projectSceneManager;
    public GameObject boatPlayerName;
    public GameObject playerInputControl;
    public Button raceButton;

    public LobbyView[] lobbies;
    public PlayerView[] players;

    [Space]
    public EnumSelector boatHullSelector;
    public ColorSelector boatPrimaryColorSelector;
    public ColorSelector boatTrimColorSelector;

    private bool isServer;
    private bool canPollLobbies;
    private bool keepLobby = true;
    private string lobbyID;
    private Lobby currentLobby;
    private ConcurrentQueue<string> createdLobbyIds = new ConcurrentQueue<string>();
    public static MultiplayerMenuHelper Instance;
    public string PlayerName { get => playerName; 
        set 
        { 
            playerName = value;
            setPlayerName.Invoke(value);
        } 
    }
    public string LobbyName { get => lobbyName; set { lobbyName = value; } }

    public static bool alreadySigned = false;

    private bool isOffline = false;

    private void OnEnable()
    {
        isOffline = Application.internetReachability == NetworkReachability.NotReachable;
        Debug.Log($"is Offline {isOffline}");
        Instance = this;
        selectLobby.AddListener(OnSelectLobby);
        kickPlayer.AddListener(OnKickPlayer);

        // boat stuff
        boatHullSelector.updateVal += setBoatType.Invoke;
        //boatPrimaryColorSelector.updateVal += setPrimaryColor.Invoke;
        //boatTrimColorSelector.updateVal += setTrimColor.Invoke;
    }

    private void OnDisable()
    {
        selectLobby.RemoveListener(OnSelectLobby);
        kickPlayer.RemoveListener(OnKickPlayer);
    }

    async void Start()
    {
        int deviceIndex = int.Parse(Application.productName[Application.productName.Length - 1].ToString());

        playerName = "Player " + deviceIndex;
        lobbyName = "Game " + ((deviceIndex * 10 ) + Random.Range(0, 10));


        InitializeLobbies(new List<Lobby>());
        InitializePlayers(new List<Player>());

        if (isOffline)
            return;

        if (!alreadySigned)
            await SignInAnonymouslyAsync();
    }

    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            string validProfileName = System.Guid.NewGuid().ToString("N").Substring(0, 30);
            AuthenticationService.Instance.SwitchProfile(validProfileName);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            alreadySigned = true;
#if DEBUG_ENABLED
            // Shows how to get the playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}, {AuthenticationService.Instance.PlayerName}");
#endif

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
        //playerInputControl.SetActive(true);
        menuAnimator.SetTrigger("Next");
        canPollLobbies = false;
    }
 

    #region Events
    private void OnSelectLobby(string lobby)
    {
#if DEBUG_ENABLED
        Debug.Log($"Selected Lobby is {lobby}");
#endif
        lobbyID = lobby;
    }

    private void OnKickPlayer(string playerID)
    {

    }
#endregion

    #region Multiplayer Services - Netcode
    public void StartRace()
    {
        if (!isServer)
        {
            SetStatus("Only Server can start race", 4f);
            return;
        }

        projectSceneManager.LoadGameScene();
        keepLobby = false;
/*        
        Debug.Log(isServer ? "Creating Game" : "Joining Game");

        if (isServer)
            CreateGame();
            //CreateRelay();
        else
            JoinGame();
*/
    }

    public void CreateGame()
    {
        if (RaceManager.RaceData.game != RaceManager.GameType.Multiplayer)
            return;

        if(isOffline)
            isServer = true;

        if(!isServer)
        {
            SetStatus("Only Server can start race", 4f);
            return;
        }

        try
        {
            NetworkManager.Singleton.StartHost();
            SwitchToBoatSelection();
            if (isOffline)
                StartCoroutine(UpdatePlayersRoutine());
        }
        catch (System.Exception e)
        {
            SetStatus(e.Message, 4f);
        }

        //UpdateLobbyData("TEST");
        //StartCoroutine(StatusRoutine());
    }

    public void JoinGame()
    {
        try
        {
            NetworkManager.Singleton.StartClient();
            SwitchToBoatSelection();

            if(isOffline)
                StartCoroutine(UpdatePlayersRoutine());

            raceButton.interactable = false;
        }
        catch (System.Exception e) {
            SetStatus(e.Message, 4f);
        }
    }

    private IEnumerator UpdatePlayersRoutine() {
        while(enabled) {
            InitializePlayers(NetworkRaceManager.playerStats);
            yield return new WaitForSeconds(.5f);
        }
    }

    public void PollLobbies()
    {
        if (isOffline)
            return;

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

#if DEBUG_ENABLED
        Debug.Log($"All clients connected lets start the game..");
#endif
        projectSceneManager.LoadGameScene();
    }
    #endregion

    #region Multiplayer Services - Lobby 
    public async void CreateLobby()
    {
        if (isOffline) {
            CreateGame();
            return;
        }

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

        try
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
            isServer = true;
#if DEBUG_ENABLED
            Debug.Log("Lobby created " + lobby.Name);
#endif

            // Heartbeat the lobby every 15 seconds.
            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));
            StartCoroutine(PollLobbyForUpdates(lobby.Id, 1.1f));
            createdLobbyIds.Enqueue(lobby.Id);

            //SwitchToBoatSelection();
            CreateGame();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            SetStatus(e.Message, 4f);
        }
    }

    public async void JoinLobby()
    {
        if (isOffline) {
            JoinGame();
            return;
        }

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
#if DEBUG_ENABLED
            Debug.Log($"Joined lobby id :{joinedLobby.Id}");
#endif
            lobbyName = joinedLobby.Name;

            StartCoroutine(PollLobbyForUpdates(lobbyID, 1.1f));
            //SwitchToBoatSelection();
            JoinGame();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            SetStatus(e.Message, 4f);
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
            InitializeLobbies(lobbies.Results);
            //lobbyContent.DestroyChildren();
            //LobbyView[] lobbyViews = lobbyContent.Populate(lobbyView, lobbies.Results);
            //foreach (var view in lobbyViews)
            //    view.selectionToggle.isOn = view.lobbyID == lobbyID;
            //...
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void InitializeLobbies(List<Lobby> results)
    {
        for (int i = 0; i < lobbies.Length; i++)
        {
            if(i < results.Count)
            {
                lobbies[i].gameObject.SetActive(true);
                lobbies[i].Initialize(results[i]);
            }else
            {
                lobbies[i].gameObject.SetActive(false);
            }
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
        //lobbyNameLabel.text = lobbyName;
        //playerContent.DestroyChildren();
        //playerContent.Populate(playerView, lobby.Players);
        InitializePlayers(lobby.Players);
    }

    private void InitializePlayers(List<Player> results)
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (i < results.Count)
            {
                players[i].gameObject.SetActive(true);
                players[i].Initialize(results[i]);
            }
            else
            {
                players[i].gameObject.SetActive(false);
            }
        }
    }

    private void InitializePlayers(List<PlayerStatus> results) {
        for (int i = 0; i < players.Length; i++) {
            if (i < results.Count) {
                players[i].gameObject.SetActive(true);
                players[i].Initialize(results[i]);
            }
            else {
                players[i].gameObject.SetActive(false);
            }
        }
    }

    IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (keepLobby)
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
        while (keepLobby)
        {
            LobbyService.Instance.GetLobbyAsync(lobbyId);
            yield return delay;
            ShowPlayers(currentLobby, isServer);

            if (!isServer)
            {
                gamestarted = !string.IsNullOrEmpty(joinCode = currentLobby.Data["JoinCode"].Value);
                //Debug.Log($"Join Code - {joinCode}");
            }
        }

        //if (!isServer)
        //    StartGame();
        //JoinRelay(joinCode);
    }

    IEnumerator PollLobbies(float waitTimeSeconds)
    {
        while(canPollLobbies || enabled)
        {
            SearchLobby();
            yield return new WaitForSecondsRealtime(waitTimeSeconds);
        }
    }
}
