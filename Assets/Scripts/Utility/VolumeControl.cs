using UnityEngine;
using UnityEngine.UI;

public class VolumeControl : MonoBehaviour
{

    public Slider volumeSlider;
    public float increament = .1f;

    public void IncreaseVolume()
    {
        float volume = volumeSlider.value;
        volume = Mathf.Clamp(volume + increament, 0, 1);
        volumeSlider.value = volume;
    }

    public void DecreaseVolume()
    {
        float volume = volumeSlider.value;
        volume = Mathf.Clamp(volume - increament, 0, 1);
        volumeSlider.value = volume;    
    }
}
