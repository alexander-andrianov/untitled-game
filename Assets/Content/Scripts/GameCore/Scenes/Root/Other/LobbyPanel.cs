using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Other
{
    public class LobbyPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text lobbyNameText;
        [SerializeField] private TMP_Text playersCountText;

        public Button Button { get; private set; }
        public Lobby Lobby { get; private set; }

        public void Initialize(Lobby lobby)
        {
            if (lobby == null)
            {
                Debug.LogError("Attempted to initialize LobbyPanel with null lobby");
                return;
            }

            Button = GetComponent<Button>();
            if (Button == null)
            {
                Debug.LogError("Button component not found on LobbyPanel");
                return;
            }

            UpdateDetails(lobby);
        }

        public void UpdateDetails(Lobby lobby)
        {
            if (lobby == null)
            {
                Debug.LogError("Attempted to update LobbyPanel with null lobby");
                return;
            }

            Lobby = lobby;

            if (lobbyNameText != null)
            {
                lobbyNameText.text = lobby.Name;
            }
            else
            {
                Debug.LogError("lobbyNameText is not assigned in LobbyPanel");
            }

            if (playersCountText != null)
            {
                playersCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
            }
            else
            {
                Debug.LogError("playersCountText is not assigned in LobbyPanel");
            }
        }
    }
}