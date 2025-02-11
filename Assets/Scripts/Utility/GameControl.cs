using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameControl : MonoBehaviour
{
    public TMP_Dropdown inputControlOptions;
    public static int index = 0;



    private void Awake()
    {
#if !UNITY_EDITOR || !UNITY_STANDALONE_WIN
        Destroy(gameObject);
#endif
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceConnected;
        InitializeGameControlOptions();
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange += OnDeviceConnected;
    }

    private void OnDeviceConnected(InputDevice device, InputDeviceChange change)
    {
        InitializeGameControlOptions();
    }

    private void InitializeGameControlOptions()
    {
        inputControlOptions.ClearOptions();

        //List<string> gamePadNames = new List<string> { "Gamepad 1", "Gamepad 2" };
        List<string> gamePadNames = Gamepad.all.Select(g => g.name).ToList();
        bool hasGamePads = gamePadNames.Any();
        List<TMP_Dropdown.OptionData> options = hasGamePads ? gamePadNames.Select(g => new TMP_Dropdown.OptionData(g)).ToList()
            : new List<TMP_Dropdown.OptionData>() { new TMP_Dropdown.OptionData("No Gamepads") };
        inputControlOptions.interactable = hasGamePads;
        inputControlOptions.AddOptions(options);
    }

    public void SetControlIndex(int index)
    {
        GameControl.index = index;
    }
}
