using Content.Scripts.GameCore.Scenes.Common.Tools;
using Content.Scripts.GameCore.Scenes.Root.Layouts;
using Content.Scripts.GameCore.Services;
using System;
using System.Collections.Generic;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
// using VivoxUnity;

namespace Content.Scripts.GameCore.Scenes.Root.Views
{
    public class LobbyController : NetworkBehaviour
    {
        private const string GameSceneName = "Game";
        private const string LoadingGameText = "Loading game...";
        private const int ReconnectAttempts = 3;
        private const int ReconnectDelay = 2000; // 2 seconds

        private readonly CompositeDisposable disposables = new();
        private readonly Dictionary<ulong, bool> playersInLobby = new();
        private int currentReconnectAttempt = 0;

        private readonly Subject<Unit> onLobbyLeft = new();
        private readonly Subject<Unit> onLobbyDeleted = new();
        private readonly Subject<Unit> onKicked = new();
        private readonly Subject<LobbyEventConnectionState> onConnectionStateChanged = new();

        public IObservable<Unit> OnLobbyLeft => onLobbyLeft;
        public IObservable<Unit> OnLobbyDeleted => onLobbyDeleted;
        public IObservable<Unit> OnKicked => onKicked;
        public IObservable<LobbyEventConnectionState> OnConnectionStateChanged => onConnectionStateChanged;

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

            if (NetworkManager.Singleton != null)
            {
                NetworkObject.DestroyWithScene = true;
            }
        }

        public override async void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                playersInLobby.Add(NetworkManager.Singleton.LocalClientId, false);
                UpdateInterface();
            }

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            
            // Скрываем loading panel после успешного подключения
            await CanvasUtilities.Instance.Toggle(false);
        }

        private void InitializeObservableListeners()
        {
            lobbyLayout.OnReady.Subscribe(HandleReadyButton).AddTo(disposables);
            lobbyLayout.OnPlay.Subscribe(HandlePlayButton).AddTo(disposables);
            lobbyLayout.OnSwitchMicro.Subscribe(HandleSwitchMicroButton).AddTo(disposables);

            // Подписываемся на события лобби
            MatchmakingService.OnLobbyLeft.Subscribe(_ => HandleLobbyLeft()).AddTo(disposables);
            MatchmakingService.OnLobbyDeleted.Subscribe(_ => HandleLobbyLeft()).AddTo(disposables);
            MatchmakingService.OnKicked.Subscribe(_ => HandleLobbyLeft()).AddTo(disposables);
            MatchmakingService.OnConnectionStateChanged.Subscribe(HandleConnectionStateChanged).AddTo(disposables);
        }

        private void HandleConnectionStateChanged(LobbyEventConnectionState state)
        {
            switch (state)
            {
                case LobbyEventConnectionState.Error:
                    Debug.LogError("Lost connection to lobby");
                    HandleLobbyLeft();
                    break;
            }
        }

        private async void HandleLobbyLeft()
        {
            try
            {
                ClearPlayers();
                DisableAllListeners();

                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.Shutdown();
                }
                await MatchmakingService.LeaveLobby();
            }
            catch (Exception e)
            {
                CanvasUtilities.Instance.ShowError(e, "Failed leaving lobby");
            }
        }

        public void ClearPlayers()
        {
            playersInLobby.Clear();
            UpdateInterface();
        }

        private void OnClientConnectedCallback(ulong playerId)
        {
            if (!IsServer) return;

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
                if (playersInLobby.ContainsKey(playerId))
                {
                    playersInLobby.Remove(playerId);
                }

                RemovePlayerClientRpc(playerId);
                UpdateInterface();
            }
            else
            {
                HandleLobbyLeft();
            }
        }

        private void PropagateToClients()
        {
            var playersCopy = new Dictionary<ulong, bool>(playersInLobby);
            foreach (var player in playersCopy)
            {
                UpdatePlayerClientRpc(player.Key, player.Value);
            }
        }

        private void HandleReadyButton(Unit unit)
        {
            SetReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        private async void HandlePlayButton(Unit unit)
        {
            try
            {
                await CanvasUtilities.Instance.Toggle(true, LoadingGameText);
                await MatchmakingService.LockLobby();
                
                NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
                NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            }
            catch (Exception e)
            {
                CanvasUtilities.Instance.ShowError(e, "Failed to start the game");
            }
        }
        
        private async void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
            await CanvasUtilities.Instance.Toggle(false, LoadingGameText);
        }

        private void HandleSwitchMicroButton(Unit unit)
        {
            if (ChatManager.Instance.IsTransmitting)
            {
                ChatManager.Instance.MuteMyself();
            }
            else
            {
                ChatManager.Instance.UnmuteMyself();
            }
            
            lobbyLayout.UpdateMicroSwitchButtonState(!ChatManager.Instance.IsTransmitting);
        }

        private void UpdateInterface()
        {
            lobbyLayout.UpdateLobby(playersInLobby);
        }

        private void DisableAllListeners()
        {
            if (!IsServer) return;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetReadyServerRpc(ulong playerId)
        {
            if (!playersInLobby.ContainsKey(playerId)) return;
            
            playersInLobby[playerId] = !playersInLobby[playerId];

            PropagateToClients();
            UpdateInterface();
        }

        [ClientRpc]
        private void UpdatePlayerClientRpc(ulong clientId, bool isReady)
        {
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
            if (playersInLobby.ContainsKey(clientId))
            {
                playersInLobby.Remove(clientId);
            }
        }
    }
}