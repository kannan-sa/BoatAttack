using BoatAttack;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

public class PlayerStatus : NetworkBehaviour
{
    [Header("Events")]
    public StringEvent onSetPlayerName;
    public IntegerEvent onSelectBoatType;
    public FloatEvent onSelectPrimaryColor;
    public FloatEvent onSelectTrimColor;
    public GameEvent onStatsUpdate;

    public NetworkVariable<FixedString128Bytes> boatName = new NetworkVariable<FixedString128Bytes>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> boatType = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> primaryColor = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> trimColor = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> status = new NetworkVariable<bool>(false, writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> finished = new NetworkVariable<bool>(false, writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> beginRace = new NetworkVariable<bool>(false, writePerm: NetworkVariableWritePermission.Owner);


    public static int index = 0;

    public int selfIndex;

    public BoatData boat;

    public static BoatData playerBoat;
    public static PlayerStatus current;

    public override void OnNetworkSpawn()
    {
        Debug.Log("Playerstatus OnNetworkSpawn called") ;
        name = $"player stat {OwnerClientId}";

        boat = new BoatData();
        boat.human = true;
        boat.boatName = boatName.Value.ToString();
        RaceManager.RaceData.boats.Add(boat);
        RaceManager.RaceData.boatCount = RaceManager.RaceData.boats.Count;
        selfIndex = RaceManager.RaceData.boats.IndexOf(boat);
        //SetDefaults
        OnBoatTypeSet(0, 0);
        
        
        boatName.OnValueChanged += OnBoatNameSet;
        boatType.OnValueChanged += OnBoatTypeSet;
        primaryColor.OnValueChanged += OnPrimaryColorSet;
        trimColor.OnValueChanged += OnTrimColorSet;
        status.OnValueChanged += OnStatusUpade;
        
        NetworkRaceManager.Add(this);
        if (IsOwner)
        {
            playerBoat = boat;
            current = this;
            index = RaceManager.RaceData.boats.IndexOf(boat);
            boatName.Value = MultiplayerMenuHelper.playerName;
            onSetPlayerName.AddListener(OnSetPlayerName);
            onSelectBoatType.AddListener(OnSelectBoatType);
            onSelectPrimaryColor.AddListener(OnSelectPrimaryColor);
            onSelectTrimColor.AddListener(OnSelectTrimColor);
        }
    }

    private void OnStatusUpade(bool previousValue, bool newValue)
    {
        onStatsUpdate.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        boatName.OnValueChanged -= OnBoatNameSet;
        boatType.OnValueChanged -= OnBoatTypeSet;
        primaryColor.OnValueChanged -= OnPrimaryColorSet;
        trimColor.OnValueChanged -= OnTrimColorSet;
        status.OnValueChanged -= OnStatusUpade;

        ClearMarkers(boat);
        RaceManager.RaceData.boats.Remove(boat);
        RaceManager.RaceData.boatCount = RaceManager.RaceData.boats.Count;
        RaceManager.Instance._boatTimes.Remove(selfIndex);

        NetworkRaceManager.Remove(this);
        if (IsOwner)
        {
            onSetPlayerName.RemoveListener(OnSetPlayerName);
            onSelectBoatType.RemoveListener(OnSelectBoatType);
            onSelectPrimaryColor.RemoveListener(OnSelectPrimaryColor);
            onSelectTrimColor.RemoveListener(OnSelectTrimColor);

            RaceManager.SetupCamera(index, true); // setup camera for player 1
        }
    }

    private void ClearMarkers(BoatData boat)
    {
        if(boat.mapMarker != null)
            Destroy(boat.mapMarker);

        if(boat.playerMarker != null) 
            Destroy(boat.playerMarker);
    }

    private void OnBoatNameSet(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        int index = RaceManager.RaceData.boats.IndexOf(boat);
        RaceManager.RaceData.boats[index].boatName = newValue.ToString();
        onStatsUpdate.Invoke();
#if DEBUG_ENABLED
        Debug.Log($"Setting Player name {newValue.ToString()} ,on {OwnerClientId}");
#endif
    }

    private void OnBoatTypeSet(int previousValue, int newValue)
    {
        int index = RaceManager.RaceData.boats.IndexOf(boat);
        RaceManager.SetHull(index, newValue);
        onStatsUpdate.Invoke();
#if DEBUG_ENABLED
            Debug.Log($"Setting Boat hull {newValue.ToString()} ,on {OwnerClientId}");
#endif
    }

    private void OnPrimaryColorSet(float previousValue, float newValue)
    {
        int index = RaceManager.RaceData.boats.IndexOf(boat);
        var c = RaceManager.RaceData.boats[index].livery.primaryColor = Color.HSVToRGB(newValue , 0.75f, 1f); // ConstantData.GetPaletteColor(newValue);
#if DEBUG_ENABLED
            Debug.Log($"Setting Primary Color {newValue.ToString()} ,on {OwnerClientId}, {c}");
#endif
    }

    private void OnTrimColorSet(float previousValue, float newValue)
    {
        int index = RaceManager.RaceData.boats.IndexOf(boat);
        var c = RaceManager.RaceData.boats[index].livery.trimColor = Color.HSVToRGB(newValue, 0.75f, 0.6f); // ConstantData.GetPaletteColor(newValue);
#if DEBUG_ENABLED
            Debug.Log($"Setting Trim Color {newValue.ToString()} ,on {OwnerClientId}, {c}");
#endif
    }

    private void OnSetPlayerName(string playerName)
    {
        boatName.Value = playerName;

        #if DEBUG_ENABLED
            Debug.Log($"Setting Player name {playerName} ,on {OwnerClientId}");
        #endif
    }

    private void OnSelectBoatType(int value)
    {
        if (!IsOwner)
            return;
        boatType.Value = value;
    }

    private void OnSelectPrimaryColor(float color)
    {
        if (!IsOwner)
            return;

        #if DEBUG_ENABLED
            Debug.Log("Setting Primary Color from UI " +  color);
        #endif
        primaryColor.Value = color;
    }

    private void OnSelectTrimColor(float color)
    {
        if (!IsOwner)
            return;

        #if DEBUG_ENABLED
            Debug.Log("Setting Trim Color from UI " +  color);
        #endif

        trimColor.Value = color;
    }
}
