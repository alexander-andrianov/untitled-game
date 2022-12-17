using System;
using System.Collections.Generic;
using Content.Scripts.GameCore.Scenes.Common.Tools;
using Content.Scripts.GameCore.Scenes.Root.Layouts;
using Content.Scripts.GameCore.Services;
using UniRx;
using Unity.Netcode;
using UnityEngine;

namespace Content.Scripts.GameCore.Scenes.Root.View
{
    public class LobbyController : NetworkBehaviour
    {
        private const string LeavingLobbyText = "Leaving...";

        private readonly CompositeDisposable disposables = new();
        private readonly Dictionary<ulong, bool> playersInLobby = new();

        [SerializeField] private LobbyLayout lobbyLayout;

        private void OnDisable()
        {
            OnLobbyLeft();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            disposables.Dispose();

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
        }

        private void Start()
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

        private void InitializeObservableListeners()
        {
            lobbyLayout.OnReady.Subscribe(HandleReadyButton).AddTo(disposables);
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
            var loader = new SceneLoader();

            using (loader)
            {
                try
                {
                    await loader.ShowLoader(LeavingLobbyText);

                    playersInLobby.Clear();
                    NetworkManager.Singleton.Shutdown();
                    await MatchmakingService.LeaveLobby();

                    await loader.HideLoader(LeavingLobbyText);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    CanvasUtilities.Instance.ShowError("Failed creating lobby");
                }
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
            Debug.Log("Changing ready button state");
        }

        private void UpdateInterface()
        {
            // LobbyPlayersUpdated?.Invoke(playersInLobby);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetReadyServerRpc(ulong playerId)
        {
            playersInLobby[playerId] = true;

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