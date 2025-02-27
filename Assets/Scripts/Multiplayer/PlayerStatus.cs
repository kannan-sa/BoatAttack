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

    public NetworkVariable<FixedString128Bytes> boatName = new NetworkVariable<FixedString128Bytes>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> boatType = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> primaryColor = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> trimColor = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> status = new NetworkVariable<bool>(false, writePerm: NetworkVariableWritePermission.Owner);

    public static int index = 0;

    public int selfIndex;

    private BoatData boat;

    public override void OnNetworkSpawn()
    {
        name = $"player stat {OwnerClientId}";
        NetworkRaceManager.playerStats.Add(this);

        boat = new BoatData();
        boat.human = true;
        boat.boatName = boatName.Value.ToString();
        RaceManager.RaceData.boats.Add(boat);
        RaceManager.RaceData.boatCount = RaceManager.RaceData.boats.Count;
        selfIndex = (int)OwnerClientId;
        //SetDefaults
        OnBoatTypeSet(0, 0);
        
        
        boatName.OnValueChanged += OnBoatNameSet;
        boatType.OnValueChanged += OnBoatTypeSet;
        primaryColor.OnValueChanged += OnPrimaryColorSet;
        trimColor.OnValueChanged += OnTrimColorSet;
        
        if (IsOwner)
        {
            index = (int)OwnerClientId;
            boatName.Value = MultiplayerMenuHelper.Instance.PlayerName;
            onSetPlayerName.AddListener(OnSetPlayerName);
            onSelectBoatType.AddListener(OnSelectBoatType);
            onSelectPrimaryColor.AddListener(OnSelectPrimaryColor);
            onSelectTrimColor.AddListener(OnSelectTrimColor);
        }
    }
    
    public override void OnNetworkDespawn()
    {
        boatName.OnValueChanged -= OnBoatNameSet;
        boatType.OnValueChanged -= OnBoatTypeSet;
        primaryColor.OnValueChanged -= OnPrimaryColorSet;
        trimColor.OnValueChanged -= OnTrimColorSet;
        NetworkRaceManager.playerStats.Remove(this);
        RaceManager.RaceData.boats.Remove(boat);
        RaceManager.RaceData.boatCount = RaceManager.RaceData.boats.Count;
        RaceManager.Instance._boatTimes.Remove(selfIndex);

        if (IsOwner)
        {
            onSetPlayerName.RemoveListener(OnSetPlayerName);
            onSelectBoatType.RemoveListener(OnSelectBoatType);
            onSelectPrimaryColor.RemoveListener(OnSelectPrimaryColor);
            onSelectTrimColor.RemoveListener(OnSelectTrimColor);
        }
    }

    private void OnBoatNameSet(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        int index = (int)OwnerClientId;
        RaceManager.RaceData.boats[index].boatName = newValue.ToString();
        #if DEBUG_ENABLED
        Debug.Log($"Setting Player name {newValue.ToString()} ,on {OwnerClientId}");
        #endif
    }

    private void OnBoatTypeSet(int previousValue, int newValue)
    {
        int index = (int)OwnerClientId;
        RaceManager.SetHull(index, newValue);
        #if DEBUG_ENABLED
            Debug.Log($"Setting Boat hull {newValue.ToString()} ,on {OwnerClientId}");
        #endif
    }

    private void OnPrimaryColorSet(float previousValue, float newValue)
    {
        int index = (int)OwnerClientId;
        var c = RaceManager.RaceData.boats[index].livery.primaryColor = Color.HSVToRGB(newValue , 0.75f, 1f); // ConstantData.GetPaletteColor(newValue);
#if DEBUG_ENABLED
            Debug.Log($"Setting Primary Color {newValue.ToString()} ,on {OwnerClientId}, {c}");
#endif
    }

    private void OnTrimColorSet(float previousValue, float newValue)
    {
        int index = (int)OwnerClientId;
        var c = RaceManager.RaceData.boats[index].livery.trimColor = Color.HSVToRGB(newValue, 0.75f, 1f); // ConstantData.GetPaletteColor(newValue);
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
