using BoatAttack;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerStatus : NetworkBehaviour
{
    [Header("Events")]
    public StringEvent onSetPlayerName;
    public IntegerEvent onSelectBoatType;
    public IntegerEvent onSelectPrimaryColor;
    public IntegerEvent onSelectTrimColor;

    public NetworkVariable<FixedString128Bytes> boatName = new NetworkVariable<FixedString128Bytes>(string.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> boatType = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> primaryColor = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> trimColor = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        name = $"player stat {OwnerClientId}";
        NetworkRaceManager.playerStats.Add(this);

        var boat = new BoatData();
        boat.human = true;
        RaceManager.RaceData.boats.Add(boat);
        RaceManager.RaceData.boatCount = RaceManager.RaceData.boats.Count;

        //SetDefaults
        OnBoatTypeSet(0, 0);
        //


        boatName.OnValueChanged += OnBoatNameSet;
        boatType.OnValueChanged += OnBoatTypeSet;
        primaryColor.OnValueChanged += OnPrimaryColorSet;
        trimColor.OnValueChanged += OnTrimColorSet;

        if(!IsOwner) return;

        boatName.Value = MultiplayerMenuHelper.Instance.PlayerName;

        onSetPlayerName.AddListener(OnSetPlayerName);
        onSelectBoatType.AddListener(OnSelectBoatType);
        onSelectPrimaryColor.AddListener(OnSelectPrimaryColor);
        onSelectTrimColor.AddListener(OnSelectTrimColor);
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!IsOwner) return;

        onSetPlayerName.RemoveListener(OnSetPlayerName);
        onSelectBoatType.RemoveListener(OnSelectBoatType);
        onSelectPrimaryColor.RemoveListener(OnSelectPrimaryColor);
        onSelectTrimColor.RemoveListener(OnSelectTrimColor);
    }

    private void OnBoatNameSet(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        int index = (int)OwnerClientId;
        RaceManager.RaceData.boats[index].boatName = newValue.ToString();
        Debug.Log($"Setting Player name {newValue.ToString()} ,on {OwnerClientId}");
    }

    private void OnBoatTypeSet(int previousValue, int newValue)
    {
        int index = (int)OwnerClientId;
        RaceManager.SetHull(index, newValue);
        Debug.Log($"Setting Boat hull {newValue.ToString()} ,on {OwnerClientId}");
    }

    private void OnPrimaryColorSet(int previousValue, int newValue)
    {
        int index = (int)OwnerClientId;
        var c = RaceManager.RaceData.boats[index].livery.primaryColor = ConstantData.GetPaletteColor(newValue);
        Debug.Log($"Setting Primary Color {newValue.ToString()} ,on {OwnerClientId}, {c}");
    }

    private void OnTrimColorSet(int previousValue, int newValue)
    {
        int index = (int)OwnerClientId;
        var c = RaceManager.RaceData.boats[index].livery.trimColor = ConstantData.GetPaletteColor(newValue);
        Debug.Log($"Setting Trim Color {newValue.ToString()} ,on {OwnerClientId}, {c}");
    }

    private void OnSetPlayerName(string playerName)
    {
        boatName.Value = playerName;

        Debug.Log($"Setting Player name {playerName} ,on {OwnerClientId}");
    }

    private void OnSelectBoatType(int value)
    {
        boatType.Value = value;
    }

    private void OnSelectPrimaryColor(int color)
    {
        primaryColor.Value = color;
    }

    private void OnSelectTrimColor(int color)
    {
        trimColor.Value = color;
    }
}
