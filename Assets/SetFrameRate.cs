using UnityEngine;

public class SetFrameRate : MonoBehaviour
{
    void Start()
    {
        // Set the target frame rate to 30 fps
        Application.targetFrameRate = 30;
    }
}
