using UnityEngine;
using UnityEngine.UI;

public class VolumeControl : MonoBehaviour
{
    public Slider volumeSlider;
    public float increament = .1f;

    public string eventName;

    private FloatEvent valueChangeEvent;


    private void Start()
    {
        valueChangeEvent = Resources.Load<FloatEvent>(eventName);   
        if (valueChangeEvent != null )
            volumeSlider.value = (float)valueChangeEvent;
    }


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

    public void OnValueChange(float newValue)
    {
        valueChangeEvent.Invoke(newValue);
    }
}
