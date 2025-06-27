using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Normal.Realtime;

public class LobbyPlayerRow : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text playerNameLabel;
    private RealtimeAvatarVoice realtimeAvatarVoice;
    private Color defaultColor;

    private void Awake()
    {
        if (background != null)
        {
            defaultColor = background.color;
        }
    }

    public void SetPlayer(string name, RealtimeAvatarVoice realtimeAvatarVoice)
    {
        playerNameLabel.text = name;
        this.realtimeAvatarVoice = realtimeAvatarVoice;
    }

    private void Update()
    {
        if (realtimeAvatarVoice == null || background == null) return;

        // Set color depending on whether the player is speaking
        bool isSpeaking = realtimeAvatarVoice.voiceVolume > 0.025f;
        background.color = isSpeaking ? Color.green : defaultColor;
    }
}