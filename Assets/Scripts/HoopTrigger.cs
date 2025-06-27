using UnityEngine;

public class HoopTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Basketball basketball = other.GetComponent<Basketball>();
        if (basketball != null && basketball.owner != null)
        {
            PlayerModel model = basketball.owner.Model;

            // Only owner increments score
            if (basketball.owner.realtimeView.isOwnedLocally)
            {
                model.playerScore += 1;
            }
        }
    }
}