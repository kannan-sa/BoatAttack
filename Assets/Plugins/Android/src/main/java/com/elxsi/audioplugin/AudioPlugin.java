package com.elxsi.audioplugin;

import android.content.Context;
import android.media.AudioDeviceInfo;
import android.media.AudioManager;
import android.util.Log;

public class AudioPlugin {
    private static final String TAG = "AudioPlugin";

    public static void setPreferredAudioDevice(Context context) {
        AudioManager audioManager = (AudioManager) context.getSystemService(Context.AUDIO_SERVICE);
        if (audioManager != null) {
            AudioDeviceInfo[] devices = audioManager.getDevices(AudioManager.GET_DEVICES_OUTPUTS);
            for (AudioDeviceInfo device : devices) {
                if (device.getType() == AudioDeviceInfo.TYPE_BLUETOOTH_A2DP) {
                    boolean result = audioManager.setPreferredDevice(null, device);
                    if (result) {
                        Log.d(TAG, "Preferred audio device set to Bluetooth A2DP.");
                    } else {
                        Log.d(TAG, "Failed to set preferred audio device.");
                    }
                    return;
                }
            }
            Log.d(TAG, "No Bluetooth A2DP device found.");
        } else {
            Log.d(TAG, "AudioManager is null.");
        }
    }
}
