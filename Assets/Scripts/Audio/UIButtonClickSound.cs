using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonClickSound : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip clickClip;
    [Range(0f, 1f)] public float volume = 0.8f;

    [Header("Buttons")]
    public bool includeInactiveButtons = true;
    public bool rescanButtonsOnEnable = true;

    private void OnEnable()
    {
        if (rescanButtonsOnEnable)
            RegisterButtonsInChildren();
    }

    [ContextMenu("Register Buttons In Children")]
    public void RegisterButtonsInChildren()
    {
        Button[] buttons = GetComponentsInChildren<Button>(includeInactiveButtons);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            UIButtonClickSoundRelay relay = button.GetComponent<UIButtonClickSoundRelay>();
            if (relay == null)
                relay = button.gameObject.AddComponent<UIButtonClickSoundRelay>();

            relay.Configure(this, button);
        }
    }

    public void PlayClickSound()
    {
        if (clickClip == null)
            return;

        ResolveAudioSource();

        if (audioSource == null)
            return;

        audioSource.PlayOneShot(clickClip, volume);
    }

    private void ResolveAudioSource()
    {
        if (SceneMusicManager.TryGetSharedAudioSource(out AudioSource sharedSource))
        {
            audioSource = sharedSource;
            return;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

    }
}

public class UIButtonClickSoundRelay : MonoBehaviour, IPointerDownHandler, ISubmitHandler
{
    private UIButtonClickSound owner;
    private Button button;
    private int lastPlayedFrame = -1;

    public void Configure(UIButtonClickSound soundOwner, Button targetButton)
    {
        owner = soundOwner;
        button = targetButton;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TryPlay();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        TryPlay();
    }

    private void TryPlay()
    {
        if (lastPlayedFrame == Time.frameCount)
            return;

        if (owner == null || !owner.isActiveAndEnabled)
            return;

        if (button != null && !button.IsInteractable())
            return;

        lastPlayedFrame = Time.frameCount;
        owner.PlayClickSound();
    }
}
