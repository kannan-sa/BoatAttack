using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ImageSequence : MonoBehaviour
{
    public Image BackGroundImage;
    public Image[] SequencialImages;      
    public float interval = 1f;
    private string scenename = "main_menu";
    void Start()
    {
        BackGroundImage.gameObject.SetActive(true);
        StartCoroutine(ActivateImagesInSequence());
    }

    IEnumerator ActivateImagesInSequence()
    {
        for (int index = 0; index < SequencialImages.Length; index++)
        {
            SequencialImages[index].gameObject.SetActive(true); 
            yield return new WaitForSeconds(interval); 

            if(index < SequencialImages.Length - 1)
            {
                SequencialImages[index].gameObject.SetActive(false);
            }
        }
        BackGroundImage.gameObject.SetActive(false);

        //SceneManager.LoadScene(scenename);
    }
}