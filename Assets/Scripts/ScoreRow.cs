using Normal.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreRow : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text playerNameLabel;
    [SerializeField] private TMP_Text playerScoreLabel;
    [SerializeField] private TMP_Text playerShotStreakLabel;

    private PlayerModel playerModel;

    private Color defaultColor;
    private RealtimeAvatarVoice realtimeAvatarVoice;

    public void Bind(PlayerModel model, RealtimeAvatarVoice voice)
    {
        playerModel = model;
        realtimeAvatarVoice = voice;

        playerNameLabel.text = model.playerName;
        playerScoreLabel.text = model.playerScore.ToString();

        model.playerScoreDidChange += OnPlayerScoreChanged;
        model.playerShotStreakDidChange += OnPlayerShotStreakChanged;
    }

    private void OnDestroy()
    {
        if (playerModel != null)
        {
            playerModel.playerScoreDidChange -= OnPlayerScoreChanged;
            playerModel.playerShotStreakDidChange -= OnPlayerShotStreakChanged;
        }
    }

    private void OnPlayerScoreChanged(PlayerModel model, int value)
    {
        playerScoreLabel.text = value.ToString();
    }

    private void OnPlayerShotStreakChanged(PlayerModel model, int value)
    {
        playerShotStreakLabel.text = value.ToString();
    }



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
        //if (realtimeAvatarVoice == null || background == null) return;

        //// Set color depending on whether the player is speaking
        //bool isSpeaking = realtimeAvatarVoice.voiceVolume > 0.025f;
        //background.color = isSpeaking ? Color.green : defaultColor;
    }
}