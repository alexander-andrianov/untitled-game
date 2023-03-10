using Content.Scripts.GameCore.Scenes.Common.Tools;
using Content.Scripts.GameCore.Scenes.Root.Layouts;
using Content.Scripts.GameCore.Services;
using System;
using System.Collections.Generic;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using VivoxUnity;

namespace Content.Scripts.GameCore.Scenes.Root.Views
{
    public class LobbyController : NetworkBehaviour
    {
        private const string GameSceneName = "Game";
        private const string LoadingGameText = "Loading game...";

        private readonly CompositeDisposable disposables = new();
        private readonly Dictionary<ulong, bool> playersInLobby = new();

        [SerializeField] private LobbyLayout lobbyLayout;

        public override void OnDestroy()
        {
            base.OnDestroy();
            disposables.Dispose();

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
        }

        private void OnEnable()
        {
            InitializeObservableListeners();

            NetworkObject.DestroyWithScene = true;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                playersInLobby.Add(NetworkManager.Singleton.LocalClientId, false);
                UpdateInterface();
            }

            // Client uses this in case host destroys the lobby
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        public void ClearPlayers()
        {
            playersInLobby.Clear();
            UpdateInterface();
        }

        private void InitializeObservableListeners()
        {
            lobbyLayout.OnReady.Subscribe(HandleReadyButton).AddTo(disposables);
            lobbyLayout.OnPlay.Subscribe(HandlePlayButton).AddTo(disposables);
            lobbyLayout.OnSwitchMicro.Subscribe(HandleSwitchMicroButton).AddTo(disposables);
        }

        private void OnClientConnectedCallback(ulong playerId)
        {
            if (!IsServer) return;

            // Add locally
            if (!playersInLobby.ContainsKey(playerId))
            {
                playersInLobby.Add(playerId, false);
            }

            PropagateToClients();
            UpdateInterface();
        }

        private void OnClientDisconnectCallback(ulong playerId)
        {
            if (IsServer)
            {
                if (playersInLobby.ContainsKey(playerId)) playersInLobby.Remove(playerId);

                // Propagate all clients
                RemovePlayerClientRpc(playerId);
                UpdateInterface();
            }
            else
            {
                OnLobbyLeft();
            }
        }

        private async void OnLobbyLeft()
        {
            try
            {
                ClearPlayers();
                DisableAllListeners();

                ChatManager.Instance.LeaveChannel();
                NetworkManager.Singleton.Shutdown();
                await MatchmakingService.LeaveLobby();
            }
            catch (Exception e)
            {
                CanvasUtilities.Instance.ShowError(e, "Failed leaving lobby");
            }
        }

        private void PropagateToClients()
        {
            foreach (var player in playersInLobby)
            {
                UpdatePlayerClientRpc(player.Key, player.Value);
            }
        }

        private void HandleReadyButton(Unit unit)
        {
            // updated for all players in lobby
            SetReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        private async void HandlePlayButton(Unit unit)
        {
            try
            {
                await CanvasUtilities.Instance.Toggle(true, LoadingGameText);

                await MatchmakingService.LockLobby();
                NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);

                await CanvasUtilities.Instance.Toggle(false, LoadingGameText);
            }
            catch (Exception e)
            {
                CanvasUtilities.Instance.ShowError(e, "Failed to start the game");
            }
        }

        private void HandleSwitchMicroButton(Unit unit)
        {
            var isMicroEnabled = ChatManager.Instance.TransmissionMode != TransmissionMode.None;
            
            if (isMicroEnabled)
            {
                ChatManager.Instance.MuteMyself();
            }
            else
            {
                ChatManager.Instance.UnmuteMyself();
            }

            lobbyLayout.UpdateMicroSwitchButtonState(isMicroEnabled == false);
        }

        private void UpdateInterface()
        {
            lobbyLayout.UpdateLobby(playersInLobby);
        }

        private void DisableAllListeners()
        {
            if (!IsServer) return;

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetReadyServerRpc(ulong playerId)
        {
            playersInLobby[playerId] = !playersInLobby[playerId];

            PropagateToClients();
            UpdateInterface();
        }

        [ClientRpc]
        private void UpdatePlayerClientRpc(ulong clientId, bool isReady)
        {
            if (IsServer) return;

            if (!playersInLobby.ContainsKey(clientId))
            {
                playersInLobby.Add(clientId, isReady);
            }
            else
            {
                playersInLobby[clientId] = isReady;
            }

            UpdateInterface();
        }

        [ClientRpc]
        private void RemovePlayerClientRpc(ulong clientId)
        {
            if (IsServer) return;

            if (playersInLobby.ContainsKey(clientId))
            {
                playersInLobby.Remove(clientId);
            }

            UpdateInterface();
        }
    }
}