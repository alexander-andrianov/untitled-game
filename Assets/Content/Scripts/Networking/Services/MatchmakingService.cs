using Content.Scripts.GameCore.Scenes.Authentication.Services;
using Content.Scripts.Networking.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Object = UnityEngine.Object;
using RelayService = Unity.Services.Relay.RelayService;

namespace Content.Scripts.GameCore.Services
{
    public static class MatchmakingService
    {
        private const int HeartbeatInterval = 15;
        private const int LobbyRefreshRate = 20;

        private static UnityTransport transport;
        private static Lobby currentLobby;
        private static CancellationTokenSource heartbeatSource;
        private static ILobbyEvents lobbyEvents;
        private static readonly Subject<List<Player>> lobbyPlayersChanged = new();
        private static readonly Subject<Unit> onLobbyLeft = new();
        private static readonly Subject<Unit> onLobbyDeleted = new();
        private static readonly Subject<Unit> onKicked = new();
        private static readonly Subject<LobbyEventConnectionState> onConnectionStateChanged = new();

        public static IObservable<List<Player>> LobbyPlayersChanged => lobbyPlayersChanged;
        public static IObservable<Unit> OnLobbyLeft => onLobbyLeft;
        public static IObservable<Unit> OnLobbyDeleted => onLobbyDeleted;
        public static IObservable<Unit> OnKicked => onKicked;
        public static IObservable<LobbyEventConnectionState> OnConnectionStateChanged => onConnectionStateChanged;

        private static UnityTransport Transport
        {
            get => transport != null ? transport : transport = Object.FindObjectOfType<UnityTransport>();
            set => transport = value;
        }

        public static async UniTask CreateLobbyWithAllocation(LobbyData data)
        {
            try
            {
                var relayService = RelayService.Instance;
                var allocation = await relayService.CreateAllocationAsync(Constants.MaxPlayers);
                var joinCode = await relayService.GetJoinCodeAsync(allocation.AllocationId);

                var options = new CreateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { Constants.JoinKey, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                    }
                };

                var lobbiesService = LobbyService.Instance;
                currentLobby = await lobbiesService.CreateLobbyAsync(data.Name, Constants.MaxPlayers, options);

                Transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, 
                    allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

                await SubscribeToLobbyEvents();
                Heartbeat();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create lobby: {e.Message}");
                throw;
            }
        }

        public static async UniTask<List<Lobby>> GatherLobbies()
        {
            try
            {
                var options = new QueryLobbiesOptions
                {
                    Count = Constants.MaxLobbies,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                        new QueryFilter(QueryFilter.FieldOptions.IsLocked, "0", QueryFilter.OpOptions.EQ)
                    }
                };

                var lobbiesService = LobbyService.Instance;
                var allLobbies = await lobbiesService.QueryLobbiesAsync(options);
                return allLobbies.Results;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to gather lobbies: {e.Message}");
                throw;
            }
        }

        public static async UniTask JoinLobbyWithAllocation(string lobbyId)
        {
            try
            {
                var lobbiesService = LobbyService.Instance;
                currentLobby = await lobbiesService.JoinLobbyByIdAsync(lobbyId);

                var relayService = RelayService.Instance;
                var allocation = await relayService.JoinAllocationAsync(currentLobby.Data[Constants.JoinKey].Value);

                Transport.SetClientRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData);

                await SubscribeToLobbyEvents();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to join lobby: {e.Message}");
                throw;
            }
        }

        public static async UniTask LeaveLobby()
        {
            heartbeatSource?.Cancel();

            if (currentLobby != null)
            {
                try
                {
                    if (lobbyEvents != null)
                    {
                        await lobbyEvents.UnsubscribeAsync();
                        lobbyEvents = null;
                    }

                    var lobbiesService = LobbyService.Instance;
                    if (currentLobby.HostId == Authentication.PlayerId)
                    {
                        await lobbiesService.DeleteLobbyAsync(currentLobby.Id);
                    }
                    else
                    {
                        await lobbiesService.RemovePlayerAsync(currentLobby.Id, Authentication.PlayerId);
                    }

                    currentLobby = null;
                    onLobbyLeft.OnNext(Unit.Default);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to leave lobby: {e.Message}");
                    throw;
                }
            }
        }

        public static async UniTask LockLobby()
        {
            try
            {
                var lobbiesService = LobbyService.Instance;
                await lobbiesService.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions { IsLocked = true });
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to lock lobby: {e.Message}");
                throw;
            }
        }

        public static Lobby GetCurrentLobby() => currentLobby;

        private static async UniTask SubscribeToLobbyEvents()
        {
            try
            {
                var callbacks = new LobbyEventCallbacks();
                callbacks.LobbyChanged += OnLobbyChanged;
                callbacks.KickedFromLobby += OnKickedFromLobby;
                callbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;

                var lobbiesService = LobbyService.Instance;
                lobbyEvents = await lobbiesService.SubscribeToLobbyEventsAsync(currentLobby.Id, callbacks);
            }
            catch (LobbyServiceException ex)
            {
                switch (ex.Reason)
                {
                    case LobbyExceptionReason.AlreadySubscribedToLobby:
                        Debug.LogWarning($"Already subscribed to lobby[{currentLobby.Id}]");
                        break;
                    case LobbyExceptionReason.LobbyEventServiceConnectionError:
                        Debug.LogError("Failed to connect to lobby events");
                        throw;
                    default:
                        throw;
                }
            }
        }

        private static void OnLobbyChanged(ILobbyChanges changes)
        {
            if (changes.LobbyDeleted)
            {
                currentLobby = null;
                onLobbyDeleted.OnNext(Unit.Default);
                return;
            }

            changes.ApplyToLobby(currentLobby);
            if (changes.PlayerJoined.Changed || changes.PlayerLeft.Changed)
            {
                lobbyPlayersChanged.OnNext(currentLobby.Players);
            }
        }

        private static void OnKickedFromLobby()
        {
            lobbyEvents = null;
            currentLobby = null;
            onKicked.OnNext(Unit.Default);
        }

        private static void OnLobbyEventConnectionStateChanged(LobbyEventConnectionState state)
        {
            onConnectionStateChanged.OnNext(state);
            
            switch (state)
            {
                case LobbyEventConnectionState.Unsubscribed:
                    Debug.Log("Unsubscribed from lobby events");
                    break;
                case LobbyEventConnectionState.Subscribing:
                    Debug.Log("Subscribing to lobby events");
                    break;
                case LobbyEventConnectionState.Subscribed:
                    Debug.Log("Successfully subscribed to lobby events");
                    break;
                case LobbyEventConnectionState.Unsynced:
                    Debug.LogWarning("Lobby events connection unsynced");
                    break;
                case LobbyEventConnectionState.Error:
                    Debug.LogError("Error in lobby events connection");
                    onLobbyLeft.OnNext(Unit.Default);
                    break;
            }
        }

        private static async void Heartbeat()
        {
            heartbeatSource = new CancellationTokenSource();

            while (!heartbeatSource.IsCancellationRequested && currentLobby != null)
            {
                try
                {
                    var lobbiesService = LobbyService.Instance;
                    await lobbiesService.SendHeartbeatPingAsync(currentLobby.Id);
                    await UniTask.Delay(HeartbeatInterval * 1000);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to send heartbeat: {e.Message}");
                    break;
                }
            }
        }
    }
}