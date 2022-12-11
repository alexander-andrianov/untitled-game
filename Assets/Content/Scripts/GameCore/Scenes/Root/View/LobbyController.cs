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

        [SerializeField] private LobbyLayout lobbyLayout;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Dictionary<ulong, bool> playersInLobby = new();

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

        private void InitializeObservableListeners()
        {
            lobbyLayout.OnReady.Subscribe(HandleReadyButton).AddTo(disposables);
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

        private void HandleReadyButton(Unit unit)
        {
            // updated for all players in lobby
            Debug.Log("Changing ready button state");
        }

        private void UpdateInterface()
        {
            // LobbyPlayersUpdated?.Invoke(playersInLobby);
        }

        [ClientRpc]
        private void RemovePlayerClientRpc(ulong clientId)
        {
            if (IsServer) return;
            if (playersInLobby.ContainsKey(clientId)) playersInLobby.Remove(clientId);

            UpdateInterface();
        }
    }
}