using LeTai.Asset.TranslucentImage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraReference : MonoBehaviour
{
    public static TranslucentImageSource ImageSource;
    public static Camera CanvasCamera;

    public TranslucentImageSource imageSource;
    public Camera canvasCamera;

    void Start()
    {
        ImageSource = imageSource;
        CanvasCamera = canvasCamera;
    }
}
