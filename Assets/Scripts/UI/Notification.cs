using TMPro;
using UnityEngine;
using System.Threading.Tasks;

public class Notification : MonoBehaviour
{
    private static Notification instance = null;
    [Header("Labels")]
    public TextMeshProUGUI status;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    #region Interface
    public static async void ShowText(string text, int duration)
    {
        Debug.Log("Status : " + text);
        if (!instance)
            return;
        instance.status.text = text;
        instance.gameObject.SetActive(true);
        await Task.Delay(1000 * duration);
        Clear();
    }

    public static void ShowText(string text)
    {
        Debug.Log("Status : " + text);
        if (!instance)
            return;
        instance.status.text = text;
        instance.gameObject.SetActive(true);
    }

    public static void Clear()
    {
        if (!instance)
            return;
        instance.status.text = string.Empty;
        instance.gameObject.SetActive(false);
    }
    #endregion
}

