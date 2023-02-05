using TMPro;
using UnityEngine;

namespace Content.Scripts.GameCore.Scenes.Root.Other
{
    public class LobbyPlayerPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText, statusText;

        public ulong PlayerId { get; private set; }

        public void Initialize(ulong playerId)
        {
            PlayerId = playerId;
            nameText.text = $"Player {playerId}";
        }

        public void UpdateReadyButton(bool isReady)
        {
            if (isReady)
            {
                statusText.text = "Ready";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = "Waiting";
                statusText.color = Color.red;
            }
        }
    }
}