using UnityEngine;

public class AudioDeviceManager : MonoBehaviour {

    public bool autoSetOnStart;

    void Start() {
        if (autoSetOnStart) 
            SetPreferredAudioDevice();
    }

    public void SetPreferredAudioDevice() {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            using (AndroidJavaClass audioPlugin = new AndroidJavaClass("com.elxsi.audioplugin.AudioPlugin")) {
                audioPlugin.CallStatic("setPreferredAudioDevice", currentActivity);
            }
        }
    }
}
