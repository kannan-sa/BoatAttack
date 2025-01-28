using System.Collections;
using UnityEngine;

public class AudioStreamer : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const string PluginClassName = "com.feature.AudioTrackPlugin";
    private AndroidJavaObject audioTrackPlugin;

    private void Start()
    {
        int deviceIndex = int.Parse(Application.productName[Application.productName.Length - 1].ToString()) - 1;
        AudioConfiguration audioConfiguration = AudioSettings.GetConfiguration();
        AudioSettings.GetDSPBufferSize(out int bufferLength, out int numBuffers);
        
        Debug.Log($"Sample Rate = {audioConfiguration.sampleRate}");
        Debug.Log($"buffer Length = {bufferLength}, numBuffers = {numBuffers}");
        Debug.Log("Device Index = " + deviceIndex);

        int sampleRate = audioConfiguration.sampleRate;
        int bufferSize = bufferLength * sizeof(float);
        audioTrackPlugin = new AndroidJavaObject(PluginClassName);
        audioTrackPlugin.Call("initialize", sampleRate, bufferSize, deviceIndex);
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        // Send the audio data to the Android plugin
        if (audioTrackPlugin == null)
            return;

        audioTrackPlugin.Call("write", data);
    }

    private void OnDestroy()
    {
        if (audioTrackPlugin == null)
            return;

        audioTrackPlugin.Call("release");
    }
#endif
}
