using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ImageSequence : MonoBehaviour
{
    public Image[] SequencialImages;
    public Image waitingImage;
    public float interval = 1f;


    public void ShowWaiting()
    {
        gameObject.SetActive(true);
        waitingImage.gameObject.SetActive(true);
    }

    public IEnumerator StartSequence()
    {
        gameObject.SetActive(true);
        waitingImage.gameObject.SetActive(false);
        for (int index = 0; index < SequencialImages.Length; index++)
        {
            SequencialImages[index].gameObject.SetActive(true); 

            yield return new WaitForSeconds(interval); 

            if(index < SequencialImages.Length - 1)
                SequencialImages[index].gameObject.SetActive(false);
        }
        gameObject.SetActive(false);
    }
}