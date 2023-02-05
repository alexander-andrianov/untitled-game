using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Other
{
    public class LobbyPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text lobbyNameText, playersCountText;

        public Button Button { get; private set; }
        public Lobby Lobby { get; private set; }

        public void Initialize(Lobby lobby)
        {
            Button = GetComponent<Button>();

            UpdateDetails(lobby);
        }

        public void UpdateDetails(Lobby lobby)
        {
            Lobby = lobby;
            lobbyNameText.text = lobby.Name;

            playersCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
        }
    }
}