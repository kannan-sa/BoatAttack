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
    [SerializeField]
    private string m_SceneName;

    public GameObject playerPrefab;
    public GameObject UIPanel;

    [Header("Asset References")]
    public AssetReference loadingScreen;

    private GameObject loadingScreenObject;

    public MainMenuHelper mainMenuHelper;

    private RaceManager raceManager;

#if UNITY_EDITOR
    public UnityEditor.SceneAsset SceneAsset;
    private void OnValidate()
    {
        if (SceneAsset != null)
        {
            m_SceneName = SceneAsset.name;
        }
    }
#endif

    public static List<PlayerStatus> playerStats = new List<PlayerStatus>();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        var loadingScreenLoading = loadingScreen.InstantiateAsync();
        yield return loadingScreenLoading;
        loadingScreenObject = loadingScreenLoading.Result;
        loadingScreenObject.SendMessage("SetLoad", .95f);
        DontDestroyOnLoad(loadingScreenObject);

        loadingScreenObject.SetActive(false);
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

    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        bool isLevel = m_SceneName.Equals(sceneEvent.SceneName, System.StringComparison.OrdinalIgnoreCase); //sceneEvent.SceneName.Equals("Level", System.StringComparison.OrdinalIgnoreCase);
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
                if(isLevel)
                    loadingScreenObject.SetActive(true);
                break;
            case SceneEventType.LoadEventCompleted:
                if (isLevel)
                    loadingScreenObject.SetActive(false);
                canStart = isLevel;
                //Hide loading..
                break;
        }


        if (!IsHost)
            return;

        if (canStart)
            StartCoroutine(SetupRace());
    }

    private IEnumerator SetupRace()
    {
        while (WaypointGroup.Instance == null) // TODO need to re-write whole game loading/race setup logic as it is dirty
        {
            yield return null;
        }
        WaypointGroup.Instance.Setup(RaceManager.RaceData.reversed);
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
        }
    }

    public void SpawnPlayer(ulong clientId)
    {
        // Instantiate the player object (only on the server).
        GameObject playerInstance = Instantiate(playerPrefab);

        // Get the NetworkObject component and spawn it.
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId); // Assigns ownership to the client.
    }
}