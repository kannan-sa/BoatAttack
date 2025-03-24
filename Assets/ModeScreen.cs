using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ModeScreen : MonoBehaviour
{
    private string testUrl = "https://clients3.google.com/generate_204"; // Lightweight request

    //internal bool isOffline=true;

    public Button onlineModeButton, offlineModeButton, backModeButton;

    [SerializeField] MultiplayerMenuHelper multiplayerMenuHelper;
    
    // Start is called before the first frame update
    async void Start()
    {
        multiplayerMenuHelper.isOffline = !await CheckInternetConnection();
        onlineModeButton.interactable = !multiplayerMenuHelper.isOffline;

        //add dynamic ui nav based on isoffline
        DynamicNav();
    }

    private void DynamicNav()
    {
        if (multiplayerMenuHelper.isOffline)
        {
            //no navigation for online button
            
            //1. ofline btn
                // get the Navigation data
            Navigation offlineBtnNavigation = offlineModeButton.navigation;

                // switch mode to Explicit to allow for custom assigned behavior
            offlineBtnNavigation.mode = Navigation.Mode.Explicit;

                // highlight the back button if the down arrow key is pressed
            offlineBtnNavigation.selectOnDown = backModeButton;
            offlineBtnNavigation.selectOnUp = backModeButton;
            offlineBtnNavigation.selectOnRight = backModeButton;
            offlineBtnNavigation.selectOnLeft = backModeButton;

            // reassign the struct data to the button
            offlineModeButton.navigation = offlineBtnNavigation;


            //2. back btn
                // get the Navigation data
            Navigation backBtnNavigation = backModeButton.navigation;

                // switch mode to Explicit to allow for custom assigned behavior
            backBtnNavigation.mode = Navigation.Mode.Explicit;

                
            backBtnNavigation.selectOnDown = offlineModeButton;
            backBtnNavigation.selectOnUp = offlineModeButton;
            backBtnNavigation.selectOnRight = offlineModeButton;
            backBtnNavigation.selectOnLeft = offlineModeButton;

                // reassign the struct data to the button
            backModeButton.navigation = backBtnNavigation;

        }
        else
        {
            //1. online btn
            // get the Navigation data
            Navigation onlineBtnNavigation = onlineModeButton.navigation;

            // switch mode to Explicit to allow for custom assigned behavior
            onlineBtnNavigation.mode = Navigation.Mode.Explicit;

            
            onlineBtnNavigation.selectOnDown = offlineModeButton;
            onlineBtnNavigation.selectOnUp = backModeButton;
            onlineBtnNavigation.selectOnRight = offlineModeButton;
            onlineBtnNavigation.selectOnLeft = backModeButton;

            // reassign the struct data to the button
            onlineModeButton.navigation = onlineBtnNavigation;

            //2. offline btn with net on
            Navigation offlineBtnNav = offlineModeButton.navigation;

            offlineBtnNav.mode = Navigation.Mode.Explicit;

            offlineBtnNav.selectOnUp = onlineModeButton;
            offlineBtnNav.selectOnDown = backModeButton;
            offlineBtnNav.selectOnRight = backModeButton;
            offlineBtnNav.selectOnLeft = onlineModeButton;

            offlineModeButton.navigation = offlineBtnNav;

            //3. back btn with net on
            Navigation backBtnNav= backModeButton.navigation;

            backBtnNav.mode = Navigation.Mode.Explicit;

            backBtnNav.selectOnRight = onlineModeButton;
            backBtnNav.selectOnLeft = offlineModeButton;
            backBtnNav.selectOnUp = offlineModeButton;
            backBtnNav.selectOnDown = onlineModeButton;

            backModeButton.navigation = backBtnNav;


        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

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

}
