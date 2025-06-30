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

    public PlayerModel playerModel;

    private Color defaultColor;
    private RealtimeAvatarVoice realtimeAvatarVoice;

    private void Awake()
    {
        if (background != null)
        {
            defaultColor = background.color;
        }
    }

    public void Bind(PlayerComponent playerComponent, RealtimeAvatarVoice voice)
    {
        playerModel = playerComponent.Model;
        realtimeAvatarVoice = voice;

        playerNameLabel.text = $"{playerComponent.Model.playerName}" + (playerComponent.realtimeView.isOwnedLocallySelf ? " (you)" : "");
        playerScoreLabel.text = playerComponent.Model.playerScore.ToString();
        playerShotStreakLabel.text = playerComponent.Model.playerShotStreak.ToString();

        playerComponent.Model.playerScoreDidChange += OnPlayerScoreChanged;
        playerComponent.Model.playerShotStreakDidChange += OnPlayerShotStreakChanged;
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

    private void Update()
    {
        //if (realtimeAvatarVoice == null || background == null) return;

        //// Set color depending on whether the player is speaking
        //bool isSpeaking = realtimeAvatarVoice.voiceVolume > 0.025f;
        //background.color = isSpeaking ? Color.green : defaultColor;
    }
}