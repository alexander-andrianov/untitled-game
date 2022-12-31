using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Data;
using Content.Scripts.GameCore.Scenes.Authentication.Services;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Content.Scripts.GameCore.Services
{
    public static class MatchmakingService
    {
        private const int HeartbeatInterval = 15;
        private const int LobbyRefreshRate = 20; // Rate limits at 2

        private static UnityTransport transport;

        private static Lobby currentLobby;
        private static CancellationTokenSource heartbeatSource, updateLobbySource;

        public static Lobby GetCurrentLobby()
        {
            return currentLobby;
        }

        private static UnityTransport Transport
        {
            get => transport != null ? transport : transport = Object.FindObjectOfType<UnityTransport>();
            set => transport = value;
        }

        public static async Task CreateLobbyWithAllocation(LobbyData data)
        {
            // Create a relay allocation and generate a join code to share with the lobby
            var a = await RelayService.Instance.CreateAllocationAsync(Constants.MaxPlayers);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

            // Create a lobby, adding the relay join code to the lobby data
            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { Constants.JoinKey, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            };

            currentLobby = await Lobbies.Instance.CreateLobbyAsync(data.Name, Constants.MaxPlayers, options);

            Transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key,
                a.ConnectionData);

            Heartbeat();
            PeriodicallyRefreshLobby();
        }

        // Obviously you'd want to add customization to the query, but this
        // will suffice for this simple demo
        public static async Task<List<Lobby>> GatherLobbies()
        {
            var options = new QueryLobbiesOptions
            {
                Count = 15,

                Filters = new List<QueryFilter>
                {
                    new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    new(QueryFilter.FieldOptions.IsLocked, "0", QueryFilter.OpOptions.EQ)
                }
            };

            var allLobbies = await Lobbies.Instance.QueryLobbiesAsync(options);
            return allLobbies.Results;
        }

        private static async void Heartbeat()
        {
            heartbeatSource = new CancellationTokenSource();

            while (!heartbeatSource.IsCancellationRequested && currentLobby != null)
            {
                await Lobbies.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                await Task.Delay(HeartbeatInterval * 1000);
            }
        }

        public static async Task JoinLobbyWithAllocation(string lobbyId)
        {
            currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            var a = await RelayService.Instance.JoinAllocationAsync(currentLobby.Data[Constants.JoinKey].Value);

            Transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key,
                a.ConnectionData, a.HostConnectionData);

            PeriodicallyRefreshLobby();
        }

        private static async void PeriodicallyRefreshLobby()
        {
            updateLobbySource = new CancellationTokenSource();

            await Task.Delay(LobbyRefreshRate * 1000);

            while (!updateLobbySource.IsCancellationRequested && currentLobby != null)
            {
                currentLobby = await Lobbies.Instance.GetLobbyAsync(currentLobby.Id);

                await Task.Delay(LobbyRefreshRate * 1000);
            }
        }

        public static async Task LeaveLobby()
        {
            heartbeatSource?.Cancel();
            updateLobbySource?.Cancel();

            if (currentLobby != null)
                try
                {
                    if (currentLobby.HostId == Authentication.PlayerId)
                    {
                        await Lobbies.Instance.DeleteLobbyAsync(currentLobby.Id);
                    }
                    else
                    {
                        await Lobbies.Instance.RemovePlayerAsync(currentLobby.Id, Authentication.PlayerId);
                    }

                    currentLobby = null;
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
        }

        public static async Task LockLobby()
        {
            try
            {
                await Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions { IsLocked = true });
            }
            catch (Exception e)
            {
                Debug.Log($"Failed closing lobby: {e}");
            }
        }
    }
}