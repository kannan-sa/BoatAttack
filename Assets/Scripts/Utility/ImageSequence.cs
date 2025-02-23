using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ImageSequence : MonoBehaviour
{
    public Image[] SequencialImages;      
    public float interval = 1f;

    public IEnumerator StartSequence()
    {
        gameObject.SetActive(true);
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