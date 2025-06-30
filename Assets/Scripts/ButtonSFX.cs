using UnityEngine;
using UnityEngine.EventSystems;


[RequireComponent(typeof(AudioSource))]
public class ButtonSFX : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerClickHandler
{
    public AudioClip hoverClip;
    public AudioClip selectClip;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverClip, 0.25f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (hoverClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(selectClip, 0.25f);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        //if (selectClip != null && audioSource != null)
        //{
        //    audioSource.PlayOneShot(selectClip, 0.5f);
        //}
    }
}