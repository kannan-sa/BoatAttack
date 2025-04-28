using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BoatAttack.UI
{
    public class MainMenuHelper : MonoBehaviour
    {
        [Header("Level Selection")] public EnumSelector levelSelector;
        public EnumSelector lapSelector;
        public EnumSelector reverseSelector;

        [Header("Boat Selection")] public GameObject[] boatMeshes;
        public EnumSelector boatHullSelector;
        public ColorSelector boatPrimaryColorSelector;
        public ColorSelector boatTrimColorSelector;

        public GameObject TypePanel;
        public GameObject OptionPanel;

        public Animator menuAnimator;
        public int playerIndex = 0;
        
        private InputControls _controls;

        public Button SinglePlayer_BoatScreen_Colors_Right,BoatScreen_RaceButton, BoatScreen_PrimaryColorRightButton, BoatScreen_SecondaryColorLeftButton;

        private void Awake()
        {
            _controls = new InputControls();
        }

        private void OnEnable()
        {
            Application.runInBackground = true;
            //Screen.fullScreen = true; // Force fullscreen

            // level stuff
            levelSelector.updateVal += SetLevel;
            lapSelector.updateVal += SetLaps;
            reverseSelector.updateVal += SetReverse;
            // boat stuff
            boatHullSelector.updateVal += UpdateBoat;
            boatPrimaryColorSelector.updateColor += UpdatePrimaryColor;
            boatTrimColorSelector.updateColor += UpdateTrimColor;
            _controls.BoatControls.Enable();
            _controls.BoatControls.Back.performed += OnBackKey;
        }

        private void OnDisable()
        {
            _controls.BoatControls.Disable();
            _controls.BoatControls.Back.performed -= OnBackKey;
        }

        private void OnBackKey(InputAction.CallbackContext context)
        {
            //Debug.Log("On Back");
            if(!TypePanel.activeSelf)
                menuAnimator.SetTrigger("Back");
            OptionPanel.SetActive(false);
        }

        private void SetupDefaults()
        {
            // level stuff
            SetLevel(levelSelector.CurrentOption);
            SetLaps(lapSelector.CurrentOption);
            SetReverse(reverseSelector.CurrentOption);
            // boat stuff

            int deviceIndex = int.Parse(Application.productName[Application.productName.Length - 1].ToString());
            string playerName = "Player " + deviceIndex;
            SetSinglePlayerName(playerName);
            UpdateBoat(playerIndex);
            UpdateBoatColor(boatPrimaryColorSelector.value, true);
            UpdateBoatColor(boatTrimColorSelector.value, false);
            //UpdateBoatColor(boatPrimaryColorSelector.CurrentOption, true);
            //UpdateBoatColor(boatTrimColorSelector.CurrentOption, false);
        }

        private void UpdateBoat(int index)
        {
            RaceManager.SetHull(playerIndex, index);
            return;
            for (var i = 0; i < boatMeshes.Length; i++)
            {
                boatMeshes[i].SetActive(i == index);
            }
        }

        public void SetupSingleplayerGame()
        {
            RaceManager.SetGameType(RaceManager.GameType.Singleplayer);
            SetupDefaults();
        }

        public void SetupSpectatorGame()
        {
            RaceManager.SetGameType(RaceManager.GameType.Spectator);
            SetupDefaults();
        }

        public void SetupMultiplayerGame()
        {
            RaceManager.SetGameType(RaceManager.GameType.Multiplayer);
            SetupDefaults();
            RaceManager.RaceData.boats.Clear();
        }

        private static void SetLevel(int index) => RaceManager.SetLevel(index);

        private static void SetLaps(int index) => RaceManager.RaceData.laps = ConstantData.Laps[index];

        private static void SetReverse(int reverse) => RaceManager.RaceData.reversed = reverse == 1;

        public void StartRace() => RaceManager.LoadGame();

        public void SetSinglePlayerName(string playerName) => RaceManager.RaceData.boats[playerIndex].boatName = playerName;

        private void UpdatePrimaryColor(int index) => UpdateBoatColor(index, true);

        private void UpdateTrimColor(int index) => UpdateBoatColor(index, false);

        private void UpdateBoatColor(int index, bool primary)
        {
            // update racedata
            if (primary)
            {
                RaceManager.RaceData.boats[playerIndex].livery.primaryColor = ConstantData.GetPaletteColor(index);
            }
            else
            {
                RaceManager.RaceData.boats[playerIndex].livery.trimColor = ConstantData.GetPaletteColor(index);
            }

            return;
            // update menu boats
            foreach (var t in boatMeshes)
            {
                var renderers = t.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var rend in renderers)
                {
                    rend.material.SetColor(primary ? "_Color1" : "_Color2", ConstantData.GetPaletteColor(index));
                }
            }
        }

        private void UpdatePrimaryColor(Color color) => UpdateBoatColor(color, true);

        private void UpdateTrimColor(Color color) => UpdateBoatColor(color, false);

        private void UpdateBoatColor(Color color, bool primary)
        {
            // update racedata
            if (primary)
            {
                if(RaceManager.RaceData.boats.Count > playerIndex) 
                    RaceManager.RaceData.boats[playerIndex].livery.primaryColor = color;
            }
            else
            {
                if(RaceManager.RaceData.boats.Count > playerIndex) 
                    RaceManager.RaceData.boats[playerIndex].livery.trimColor = color;
            }

            foreach (var t in boatMeshes)
            {
                var renderers = t.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var rend in renderers)
                {
                    rend.material.SetColor(primary ? "_Color1" : "_Color2", color);
                }
            }
        }

        /// <summary>
        /// This function is for the bug where when you go up from race button (to the colors-right button), you are not able to return back to race button.
        /// This was happening because we have hard coded the ui navigation to the "Next" button which this animation will deactivate.
        /// </summary>
        public void SinglePlayer_ColorsRightButton_Dynamic_UINavigation()
        {
            print("dynamic nav called?");
            //dynamic ui nav for "SinglePlayer_BoatScreen_Colors_Right" button
            Navigation colorsRightNavigation = SinglePlayer_BoatScreen_Colors_Right.navigation;
            colorsRightNavigation.mode = Navigation.Mode.Explicit;

            colorsRightNavigation.selectOnDown = BoatScreen_RaceButton;
            colorsRightNavigation.selectOnUp = BoatScreen_PrimaryColorRightButton;
            colorsRightNavigation.selectOnRight = BoatScreen_SecondaryColorLeftButton;
            colorsRightNavigation.selectOnLeft = BoatScreen_SecondaryColorLeftButton;

            // reassign the struct data to the button
            SinglePlayer_BoatScreen_Colors_Right.navigation = colorsRightNavigation;
        }
    }
}
