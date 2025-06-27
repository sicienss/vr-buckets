using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class HoopTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Basketball basketball = other.GetComponent<Basketball>();
        if (basketball != null )
        {
            if (gameObject.name == "HoopTopTrigger")
            {
                basketball.enteredTop = true;
                basketball.ResetTopAfterDelay();
            }
            else if (gameObject.name == "HoopBottomTrigger" && basketball.enteredTop)
            {
                float threePointThreshold = 4; // distance in meters for 3-pointers // TODO: Don't hardcode this here
                int scoreToAward = basketball.shotDistance > threePointThreshold ? 3 : 2;
                basketball.owner.Model.playerScore += scoreToAward;
                basketball.owner.Model.playerShotStreak += 1;

                // Spawn effect -- NOTE: this is local to client who made shot
                Vector3 spawnPosition = transform.position + Vector3.up * 0.25f; // just above the hoop
                GameObject scoreText = Instantiate(GameManager.instance.floatingScoreTextPrefab, spawnPosition, Quaternion.identity);
                var tmp = scoreText.GetComponentInChildren<TMPro.TMP_Text>();
                if (tmp != null) tmp.text = $"+{scoreToAward}";

                basketball.PlaySwish(); // SFX
            }
        }
    }
}
