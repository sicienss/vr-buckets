using UnityEngine;
using TMPro;

public class LobbyPlayerRow : MonoBehaviour
{
    [SerializeField] public TMP_Text nameLabel;

    public void SetPlayerName(string name)
    {
        nameLabel.text = name;
    }
}
