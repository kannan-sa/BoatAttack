using BoatAttack;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using BoatAttack.UI;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Lobbies;
using UnityEngine.Networking;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using System.Collections.Concurrent;
using Unity.Networking.Transport.Relay;
using System;

public class CustomCertificateHandler : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; // Accept all certificates (INSECURE)
    }
}

public class MultiplayerMenuHelper : MonoBehaviour
{
    public static string playerName;
    public static string lobbyName;

    [Header("Configuration")]
    public int maxPlayerInLobby = 4;
    public int notificationWaitSeconds = 2;
    public int joinGameWaitMilliseconds = 500;
    public bool isOffline = true;

    [Header("Panels")]
    public GameObject boatPanel;
    public GameObject playersPanel;
    public GameObject typePanel;

    [Header("Events")]
    public StringEvent selectLobby;
    public StringEvent kickPlayer;
    public StringEvent setPlayerName;
    public IntegerEvent setBoatType;
    public GameEvent playerStatUpdate;
    public GameEvent editBoat;

    [Header("Controls")]
    public Animator menuAnimator;
    public NetworkManager networkManager;
    public NetworkRaceManager networkRaceManager;
    public Slider primeColorSlider, trimColorSlider;

    public GameObject endSessionButton, leaveSessionButton;
    public Button createGameButton, joinGameButton, startGameButton, onlineModeButton;

    public LobbyView[] lobbies;
    public PlayerView[] players;

    [Space]
    public EnumSelector boatHullSelector;
    public ColorSelector boatPrimaryColorSelector;
    public ColorSelector boatTrimColorSelector;

    private Lobby currentLobby;
    private string lobbyID;
    public static MultiplayerMenuHelper Instance;
    private bool canPollLobbies, keepLobby = true;
    private ConcurrentQueue<string> createdLobbyIds = new ConcurrentQueue<string>();
    
    public static bool alreadySigned = false;
    internal static bool isServer = false;
    private static NetworkManager nManager = null;
    private static NetworkRaceManager nRaceManager = null;

    [SerializeField] ModeScreen modeScreen;

    #region Unity - Events
    private void Awake()
    {
        if (nManager == null)
        {
            nManager = networkManager;
        }
        else
        {
            Destroy(networkManager.gameObject);
            networkManager = nManager;
        }

        if (nRaceManager == null)
        {
            nRaceManager = networkRaceManager;
        }
        else
        {
            Destroy(networkRaceManager.gameObject);
            networkRaceManager = nRaceManager;
        }
    }

    private void OnEnable()
    {
        //isOffline = Application.internetReachability == NetworkReachability.NotReachable;

        Debug.Log($"is Offline {isOffline}");
        Instance = this;
        selectLobby.AddListener(OnSelectLobby);
        kickPlayer.AddListener(OnKickPlayer);
        playerStatUpdate.AddListener(OnPlayerStatsUpdate);
        editBoat.AddListener(ResetStatus);
        // boat stuff
        boatHullSelector.updateVal += setBoatType.Invoke;
        NetworkRaceManager.OnPlayerStatsUpdate += OnPlayerStatsUpdate;
    }

    private void OnDisable()
    {
        selectLobby.RemoveListener(OnSelectLobby);
        kickPlayer.RemoveListener(OnKickPlayer);
        playerStatUpdate.RemoveListener(OnPlayerStatsUpdate);
        editBoat.RemoveListener(ResetStatus);

        NetworkRaceManager.OnPlayerStatsUpdate -= OnPlayerStatsUpdate;
        canPollLobbies = false;
    }

    public static int deviceIndex;

    async void Start()
    {
        InitializeLobbies(new List<Lobby>());
        InitializePlayers(new List<Player>());

        ClearCameraPlayerCullingMask();

        deviceIndex = int.Parse(Application.productName[Application.productName.Length - 1].ToString());
        playerName = "Player " + deviceIndex;
        lobbyName = "Game " + ((deviceIndex * 10) + UnityEngine.Random.Range(0, 10));

        if (TryResumeGameSession())
            return;

        //isOffline = !await modeScreen.CheckInternetConnection();//!await CheckInternetConnection();
        //onlineModeButton.interactable = !isOffline;
       

        if (!alreadySigned)
            await SignInAnonymouslyAsync();
    }

    void OnApplicationQuit()
    {
        DeleteAllLobbies();
    }
    #endregion

    #region UI

    private void InitializeLobbies(List<Lobby> results)
    {
        var joinableLobbies = results.Where(l => l.Players.Count < maxPlayerInLobby).ToList();
        try
        {
            for (int i = 0; i < lobbies.Length; i++)
            {
                if (i < joinableLobbies.Count)
                {
                    lobbies[i].gameObject.SetActive(true);
                    lobbies[i].Initialize(joinableLobbies[i]);
                }
                else
                {
                    lobbies[i].gameObject.SetActive(false);
                }
            }
        }
        catch
        {
            //Nothing..
        }
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

    private void InitializePlayers(List<PlayerStatus> results)
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
    #endregion

    #region Process - Try
    private bool TryResumeGameSession()
    {
        bool canResume = NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient;

        if (!canResume)
            return canResume;

        menuAnimator.Play("Menu_Multiplayer_BoatToPlayer", 0, 1f);
        startGameButton.interactable = isServer;
        endSessionButton.SetActive(isServer);
        leaveSessionButton.SetActive(!isServer);
        InitializePlayers(NetworkRaceManager.playerStats);

        if (isServer)
            EventSystem.current.SetSelectedGameObject(startGameButton.gameObject);
        else
            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;

        return canResume;
    }

    private string testUrl = "https://clients3.google.com/generate_204"; // Lightweight request

    public async Task<bool> CheckInternetConnection()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
        {
            request.certificateHandler = new CustomCertificateHandler();
            request.timeout = 5;
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            return request.result != UnityWebRequest.Result.ConnectionError && request.result != UnityWebRequest.Result.ProtocolError;
        }
    }

    private void ClearCameraPlayerCullingMask() {
        for (int i = 0; i < maxPlayerInLobby; i++)
            RaceManager.SetupCamera(i, true);
    }

    public void Clear() {
        if (!isServer)
            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
    }
    #endregion

    #region Process - Transition
    private void SwitchToBoatSelection()
    {
        menuAnimator.SetTrigger("Next");
        canPollLobbies = false;
        endSessionButton.SetActive(isServer);
        leaveSessionButton.SetActive(!isServer);
        startGameButton.interactable = isServer;

        primeColorSlider.value = 0;
        trimColorSlider.value = 0;
        Notification.Clear();
    }
    #endregion

    #region Multiplayer - Authentication 

    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.LogError("INitializesd");
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

    #endregion

    #region Event - Delegates
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

    private void OnPlayerStatsUpdate()
    {
        InitializePlayers(NetworkRaceManager.playerStats);

        if (playersPanel == null && !playersPanel.activeSelf)
            return;

        GameObject buttonToSelect = null;

        if (isServer)
        {
            startGameButton.interactable = NetworkRaceManager.playerStats.All(p => p.status.Value);
            if (startGameButton.interactable)
                buttonToSelect = startGameButton.gameObject;
        }
        else
            buttonToSelect = leaveSessionButton;

        if (!playersPanel.activeSelf) // for point 1
            return;

        if (buttonToSelect != null)
            EventSystem.current.SetSelectedGameObject(buttonToSelect);
    }

    private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        if (isServer)
            return;

        if (playersPanel == null)
            return;

        if (boatPanel == null)
            return;


        switch (data.EventType)
        {
            case ConnectionEvent.ClientDisconnected:

                InitializeLobbies(new List<Lobby>());//on other projects also

                if (playersPanel.activeSelf)
                    menuAnimator.SetTrigger("EndSession");
                else if (boatPanel.activeSelf)
                    menuAnimator.SetTrigger("Back");

                Debug.Log("OnConnectionEvent,  " + data.EventType);

                NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;

                if (!isOffline)
                    PollLobbies();
                break;
        }
    }

    public void SetMultiplayerMode(bool mode)
    {
        isOffline = mode;

        if (isOffline)
            InitializeLobbies(new List<Lobby>());
    }
    #endregion

    #region Multiplayer Services - Netcode
    public void StartRace()
    {
        if (RaceManager.RaceData.game != RaceManager.GameType.Multiplayer)
            return;

        nRaceManager.LoadGameScene();
        keepLobby = false;
    }

    public void CreateGame()
    {
        if (RaceManager.RaceData.game != RaceManager.GameType.Multiplayer)
            return;

        try
        {
            if (isOffline && NetworkManager.Singleton.TryGetComponent(out UnityTransport unityTransport))
                unityTransport.SetConnectionData("127.0.0.1", 7777);

            NetworkManager.Singleton.StartHost();
            isServer = true;
            SwitchToBoatSelection();
        }
        catch (System.Exception e)
        {
            Notification.ShowText(e.Message, notificationWaitSeconds);
        }
    }

    public async void JoinGame()
    {
        try
        {
            if (isOffline && NetworkManager.Singleton.TryGetComponent(out UnityTransport unityTransport))
                unityTransport.SetConnectionData("127.0.0.1", 7777);

            NetworkManager.Singleton.StartClient();

            if (isOffline)
            {
                Notification.ShowText("Finding Game..", 1);
                await Task.Delay(joinGameWaitMilliseconds);
                bool isConnected = NetworkManager.Singleton.IsConnectedClient;
                bool lobbyFull = NetworkRaceManager.playerStats.Count > maxPlayerInLobby;
                if (!isConnected || lobbyFull)
                {
                    Notification.ShowText(lobbyFull ? "Lobby Is Full" : "No Game Found To Join", notificationWaitSeconds);
                    NetworkManager.Singleton.Shutdown();
                    return;
                }
            }
            else
            {
                await Task.Delay(6000);//and explicit nav
                bool isConnected = NetworkManager.Singleton.IsConnectedClient;
                Debug.Log("Connected " + isConnected);
                if (!isConnected)
                {
                    Notification.ShowText("COULD NOT JOIN GAME, RETRY...", notificationWaitSeconds);
                    NetworkManager.Singleton.Shutdown();
                    return;
                }
            }

            isServer = false;
            SwitchToBoatSelection();
            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
        }
        catch (System.Exception e)
        {
            Notification.ShowText(e.Message, notificationWaitSeconds);
        }
    }

    public void EndSession()
    {
        if (RaceManager.RaceData.game != RaceManager.GameType.Multiplayer)
            return;

        if (isServer)
        {
            keepLobby = false;
            isServer = false;
            DeleteAllLobbies();
        }
        else // fix for bug : When one player leaves the Online multiplayer game lobby and try to join again it shows Player already member of the lobby
        {
            if (!isOffline && !string.IsNullOrEmpty(lobbyID))
            {
                Lobbies.Instance.RemovePlayerAsync(lobbyID, AuthenticationService.Instance.PlayerId);
            }
            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
        }

        if (!isOffline)
            PollLobbies();

        NetworkManager.Singleton.Shutdown();
        if (playersPanel.activeSelf)
            menuAnimator.SetTrigger("EndSession");
    }

    public async void PollLobbies()
    {
        if (isOffline)
            return;

        canPollLobbies = true;

        while (canPollLobbies)
        {
            SearchLobby();
            await Task.Delay(1000 * 2);
        }
    }

    public void SetStatus()
    {
        if (RaceManager.RaceData.game != RaceManager.GameType.Multiplayer)
            return;

        //int index = PlayerStatus.index;
        //NetworkRaceManager.playerStats[index].status.Value = true;
        PlayerStatus.current.status.Value = true;
        if (isServer)
            startGameButton.interactable = NetworkRaceManager.playerStats.All(p => p.status.Value);
    }

    public void ResetStatus()
    {
        //int index = PlayerStatus.index;
        //NetworkRaceManager.playerStats[index].status.Value = false;
        PlayerStatus.current.status.Value = false;

        menuAnimator.SetTrigger("Back");
    }

    #endregion

    #region Multiplayer Services - Lobby 
    public async void CreateLobby()
    {
        if (isOffline)
        {
            CreateGame();
            return;
        }
        else
        {

            bool online = await modeScreen.CheckInternetConnection();//CheckInternetConnection();   //it may cause more delay in creating lobby...
            Debug.LogError("Lobby "+ online);

            if (!online)
            {
                Notification.ShowText("No Internet", notificationWaitSeconds);
                return;
            }
        }



        try
        {
            Notification.ShowText("CREATING GAME...");
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
            Debug.LogError("Lobby creating lobby");


            lobbyName = "Game " + ((deviceIndex * 10) + UnityEngine.Random.Range(0, 10));

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayerInLobby, options);
            lobbyID = currentLobby.Id;

            Debug.Log("lobbyID : "+lobbyID +"   and joinCode: "+ options.Data["JoinCode"] + "  lobbyName: " + lobbyName);

#if DEBUG_ENABLED
            Debug.Log("Lobby created " + lobby.Name);
#endif

            // Heartbeat the lobby every 15 seconds.
            HeartbeatLobbyCoroutine(currentLobby.Id, 15);
            createdLobbyIds.Enqueue(currentLobby.Id);

            CreateRelay();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            Notification.ShowText(e.Message, notificationWaitSeconds);
        }
    }

    public async void JoinLobby()
    {

        if (isOffline)
        {
            JoinGame();
            return;
        }
        else
        {

            bool online = await modeScreen.CheckInternetConnection();   //it may cause more delay in creating lobby...

            if (!online)
            {
                Notification.ShowText("No Internet", notificationWaitSeconds);
                return;
            }

        }

        if (string.IsNullOrEmpty(lobbyID))
        {
            Notification.ShowText("Select Lobby", notificationWaitSeconds);
            return;
        }

        try
        {
            Notification.ShowText("JOINING GAME...");
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions()
            {
                Player = GetPlayer(),
            };

            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID, options);
            currentLobby = joinedLobby;
            lobbyID = joinedLobby.Id;
            lobbyName = joinedLobby.Name;
            string joinCode = joinedLobby.Data["JoinCode"].Value;

            Debug.Log("lobbyID : " + lobbyID + "   and joinCode: " + joinCode + "  lobbyName: " + lobbyName);

            JoinRelay(joinCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            Notification.ShowText(e.Message, notificationWaitSeconds);
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

            Debug.Log("lobbies.Results : " + lobbies.Results + "    lobbyId: "+lobbyID + "  lobbyName: "+lobbyName);

            InitializeLobbies(lobbies.Results);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void UpdateLobbyData(string joinCode)
    {

        Debug.Log("joinCode: " + joinCode + "   and lobbyID: "+lobbyID + "  lobbyName: "+lobbyName);

        UpdateLobbyOptions options = new UpdateLobbyOptions()
        {
            Data = new Dictionary<string, DataObject>()
            {
                {"JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
            }
        };
        Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(lobbyID, options);
    }

    private Player GetPlayer()
    {
        Debug.Log("in get player()  and lobbyID: " + lobbyID + "  lobbyName: " + lobbyName + "playerName: "+playerName);
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>() {
                    { "playerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                }
        };
        
    }

    private async void HeartbeatLobbyCoroutine(string lobbyId, int waitTimeSeconds)
    {
        Debug.Log("lobbyID: " + lobbyId);
        while (keepLobby)
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            await Task.Delay(1000 * waitTimeSeconds);
        }
    }

    private void DeleteAllLobbies()
    {
        while (createdLobbyIds.TryDequeue(out var lobbyId))
        {
            Debug.Log("lobbyID: " + lobbyId);
            LobbyService.Instance.DeleteLobbyAsync(lobbyId);
        }
    }
    #endregion

    #region Multiplayer Services - Relay
    private async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3); //For 4 player excluding host..
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("joinCode: "+ joinCode);
            UpdateLobbyData(joinCode);
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            CreateGame();
        }
        catch (RelayServiceException ex)
        {
            Debug.LogException(ex);
        }
    }

    private async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log("joinCode: " + joinCode);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            JoinGame();
        }
        catch (RelayServiceException ex)
        {
            Debug.LogException(ex);
        }
    }
    #endregion
}