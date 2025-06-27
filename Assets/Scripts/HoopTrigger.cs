using UnityEngine;

public class HoopTrigger : MonoBehaviour
{
    [SerializeField] GameObject floatingScoreTextPrefab;

    private void OnTriggerEnter(Collider other)
    {
        Basketball basketball = other.GetComponent<Basketball>();
        if (basketball != null && basketball.owner != null && !basketball.hasScored)
        {
            basketball.hasScored = true;
            PlayerModel model = basketball.owner.Model;

            // Only owner increments score
            if (basketball.owner.realtimeView.isOwnedLocally)
            {
                float threePointThreshold = 4; // distance in meters for 3-pointers // TODO: Don't hardcode this here
                int scoreToAward = basketball.shotDistance > threePointThreshold ? 3 : 2;
                model.playerScore += scoreToAward;
                model.playerShotStreak += 1;

                // Spawn effect -- NOTE: this is local to client who made shot
                Vector3 spawnPosition = transform.position + Vector3.up * 0.25f; // just above the hoop
                GameObject scoreText = Instantiate(floatingScoreTextPrefab, spawnPosition, Quaternion.identity);
                var tmp = scoreText.GetComponentInChildren<TMPro.TMP_Text>();
                if (tmp != null) tmp.text = $"+{scoreToAward}";

                basketball.PlaySwish(); // SFX
            }
        }
    }
}