using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class ProjectSceneManager : NetworkBehaviour
{
    [SerializeField]
    private string m_SceneName;

    public GameObject playerPrefab;
    public GameObject UIPanel;

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

    public override void OnNetworkSpawn()
    {
        Debug.Log($"OnNetworkSpawn {OwnerClientId}, isOwner {IsOwner} ");
        NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
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
        bool isLevel = string.Equals("Level", sceneEvent.SceneName, System.StringComparison.OrdinalIgnoreCase); //sceneEvent.SceneName.Equals("Level", System.StringComparison.OrdinalIgnoreCase);
        
        bool canStart = isLevel && sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted;

        Debug.Log($"OnSceneEvent {OwnerClientId}, isOwner {IsOwner}");
        Debug.Log($"Scene Name = {sceneEvent.SceneName} ,Event Type = {sceneEvent.SceneEventType}");

        //if (canStart)
            //UIPanel.SetActive(false);

        if (!IsHost)
            return;

        if(canStart)
            SpawnPlayerForAllClients();
    }

    private void SpawnPlayerForAllClients()
    {
        Debug.Log($"SpawnPlayerForAllClients {OwnerClientId}, IsHost {IsHost}");

        if (!IsHost)
            return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            SpawnPlayer(clientId);
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