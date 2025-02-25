using BoatAttack;
using UnityEngine;
using BoatAttack.UI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class NetworkRaceManager : NetworkBehaviour
{
    //[Header("Scenes")]
    [SerializeField]
    private string m_SceneName, m_MenuScene;

    [Header("Events")]
    public GameEvent finishGame;

    [Header("Asset References")]
    public AssetReference loadingScreen;

    private static GameObject loadingScreenObject;

    public MainMenuHelper mainMenuHelper;

    private RaceManager raceManager;

    private int loadedCount;

    private List<NetworkObject> networkObjects = new List<NetworkObject>();

#if UNITY_EDITOR
    public UnityEditor.SceneAsset SceneAsset, MenuScene;
    private void OnValidate()
    {
        if (SceneAsset != null)
        {
            m_SceneName = SceneAsset.name;
        }

        if (MenuScene != null)
        {
            m_MenuScene = MenuScene.name;
        }
    }
#endif

    public static List<PlayerStatus> playerStats = new List<PlayerStatus>();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        finishGame.AddListener(LoadMenuScene);
    }

    private void OnDisable()
    {
        finishGame.RemoveListener(LoadMenuScene);
    }

    private IEnumerator Start()
    {
        if (loadingScreenObject == null)
        {
            var loadingScreenLoading = loadingScreen.InstantiateAsync();
            yield return loadingScreenLoading;

            Debug.Log("NetworkRaceManager Start Loading");

            loadingScreenObject = loadingScreenLoading.Result;
            loadingScreenObject.SendMessage("SetLoad", .95f);
            DontDestroyOnLoad(loadingScreenObject);

            loadingScreenObject.SetActive(false);
        }
    }

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        base.OnOwnershipChanged(previous, current);

        #if DEBUG_ENABLED
            Debug.Log($"Owner Ship Changed {current}, previous {previous}");
        #endif
    }

    public override void OnNetworkSpawn()
    {
        #if DEBUG_ENABLED
            Debug.Log($"OnNetworkSpawn {OwnerClientId}, isOwner {IsOwner} ");
        #endif
        
        NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;

        raceManager = RaceManager.Instance;
    }

    public void LoadGameScene()
    {
        var status = NetworkManager.Singleton.SceneManager.LoadScene(m_SceneName, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Failed to load {m_SceneName} " +
                  $"with a {nameof(SceneEventProgressStatus)}: {status}");
        }
    }

    public void LoadMenuScene()
    {
        if (IsHost)
        {
            LoadMenuSceneGeneral();
        }
        else
        {
            LoadMenuSceneRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void LoadMenuSceneRpc()
    {
        LoadMenuSceneGeneral();
    }

    private void LoadMenuSceneGeneral()
    {
        DestroyBoats();
        var status = NetworkManager.Singleton.SceneManager.LoadScene(m_MenuScene, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Failed to load {m_SceneName} " +
                  $"with a {nameof(SceneEventProgressStatus)}: {status}");
        }
    }

    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        bool isLevel = m_SceneName.Equals(sceneEvent.SceneName, System.StringComparison.OrdinalIgnoreCase); //sceneEvent.SceneName.Equals("Level", System.StringComparison.OrdinalIgnoreCase);
        bool isMenu = m_MenuScene.Equals(sceneEvent.SceneName, System.StringComparison.OrdinalIgnoreCase);
        bool canStart = false;

        #if DEBUG_ENABLED
            Debug.Log($"Scene Name = {sceneEvent.SceneName} ,Event Type = {sceneEvent.SceneEventType}");
        #endif

        //if (canStart)
        //UIPanel.SetActive(false);

        switch(sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
                //Show loading..
                if(isLevel || isMenu)
                    loadingScreenObject.SetActive(true);
                break;
            case SceneEventType.LoadEventCompleted:
                //Hide loading..
                if (isLevel || isMenu)
                    loadingScreenObject.SetActive(false);

                if (isMenu)
                    RaceManager.Instance.ResetGame();

                canStart = isLevel;
                break;
        }


        if (canStart)
        {
            StartCoroutine(SetupWaypoints());
        }

        //if (!IsHost)
        //    return;

        //if (canStart)
        //    StartCoroutine(SetupRace());
    }

    private IEnumerator SetupWaypoints()
    {
        while (WaypointGroup.Instance == null) // TODO need to re-write whole game loading/race setup logic as it is dirty
        {
            yield return null;
        }
        WaypointGroup.Instance.Setup(RaceManager.RaceData.reversed);
        WayPointInitializedRpc();
    }

    [Rpc(SendTo.Server)]
    private void WayPointInitializedRpc()
    {
        bool canSpawn = loadedCount == (NetworkManager.Singleton.ConnectedClients.Count -1);
        Debug.Log($"Loaded {loadedCount++}, client id {OwnerClientId}, canSpawn = {canSpawn}");

        if(canSpawn)
            StartCoroutine(SetupRace());
    }

    private IEnumerator SetupRace()
    {
        //yield return StartCoroutine(SetupWaypoints());
        yield return StartCoroutine(CreateBoats()); // spawn boats;
    }

    private IEnumerator CreateBoats()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            int i = (int)clientId;
            var boat = RaceManager.RaceData.boats[i]; // boat to setup

            // Load prefab
            var startingPosition = WaypointGroup.Instance.StartingPositions[i];
            var startPosition = startingPosition.GetColumn(3);
            Debug.Log($" boat {i} @position {startPosition}");
            AsyncOperationHandle<GameObject> boatLoading = Addressables.InstantiateAsync(boat.boatPrefab, startPosition,
                    Quaternion.LookRotation(startingPosition.GetColumn(2)));
            yield return boatLoading; // wait for boat asset to load
            GameObject newBoat = boatLoading.Result;
            NetworkObject boatObject = newBoat.GetComponent<NetworkObject>();
            boatObject.SpawnAsPlayerObject(clientId);
            networkObjects.Add(boatObject);
        }
    }

    private void DestroyBoats()
    {
        foreach (var item in networkObjects)
        {
            item.Despawn();
        }
    }
}