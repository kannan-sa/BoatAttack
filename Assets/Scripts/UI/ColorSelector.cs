using BoatAttack;
using UnityEngine;
using UnityEngine.UI;

namespace BoatAttack.UI
{
    public class ColorSelector : MonoBehaviour
    {
        public Color value;
        public bool loop;
        public int startOption;
        private int _currentOption;

        public FloatEvent changeEvent;


        public delegate void UpdateValue(int index);

        public UpdateValue updateVal;

        public delegate void UpdateColor2(Color color);

        public UpdateColor2 updateColor;

        public Slider slider;

        //

        public float hue, saturation, val;
        public Image image;

        public float HUE
        {
            get => hue;
            set
            {
                hue = value;
                this.value = Color.HSVToRGB(hue, saturation, val);
                updateColor?.Invoke(this.value);
                changeEvent?.Invoke(hue);
                if (image)
                    image.color = this.value;
                if (slider)
                    slider.value = hue;
            }
        } 


        private void ValueUpdate(int i)
        {
            updateVal?.Invoke(i);
        }

        private void Awake()
        {
            _currentOption = startOption;
            UpdateColor();
        }

        public void NextOption()
        {
            _currentOption = ValidateIndex(_currentOption + 1);
            HUE = ValidateValue(hue + .1f); 
            UpdateColor();
            ValueUpdate(_currentOption);
        }

        public void PreviousOption()
        {
            _currentOption = ValidateIndex(_currentOption - 1);
            HUE = ValidateValue(hue - .1f);
            UpdateColor();
            ValueUpdate(_currentOption);
        }

        public int CurrentOption
        {
            get => _currentOption;
            set
            {
                _currentOption = ValidateIndex(value);
                UpdateColor();
                ValueUpdate(_currentOption);
            }
        }

        private void UpdateColor()
        {
            value = ConstantData.GetPaletteColor(_currentOption);
        }

        private int ValidateIndex(int index)
        {
            if (loop)
            {
                return (int)Mathf.Repeat(index, ConstantData.ColorPalette.Length);
            }

            return Mathf.Clamp(index, 0, ConstantData.ColorPalette.Length);
        }

        private float ValidateValue(float value)
        {
            if (loop)
            {
                return Mathf.Repeat(value, 1f);
            }

            return Mathf.Clamp(value, 0f, 1f);
        }
    }
}