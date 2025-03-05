using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Gamecore.Base.Structs;
using Content.Scripts.GameCore.Scenes.Game.Controllers;
using Content.Scripts.GameCore.Services;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Unity.Services.Lobbies;

namespace Content.Scripts.GameCore.Scenes.Game.Entry
{
    public class GameEntry : NetworkBehaviour
    {
        [SerializeField] private PlayerController playerPrefab;
        [SerializeField] private Transform playerParent;

        private List<Color> playerColors;
        private List<Vector3> playerSpawns;
        private readonly Dictionary<ulong, PlayerController> spawnedPlayers = new();

        public override async void OnDestroy()
        {
            base.OnDestroy();
            await MatchmakingService.LeaveLobby();

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        public override async void OnNetworkSpawn()
        {
            await Initialize();

            if (IsServer)
            {
                // Подписываемся на события подключения/отключения клиентов
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

                // Спавним всех существующих клиентов
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    SpawnPlayerServerRpc(clientId);
                }
            }
            else
            {
                // Клиент запрашивает спавн своего игрока
                RequestSpawnServerRpc();
            }
        }

        [ServerRpc]
        private void RequestSpawnServerRpc()
        {
            var clientId = NetworkManager.Singleton.LocalClientId;
            SpawnPlayerServerRpc(clientId);
        }

        private void OnClientConnectedCallback(ulong clientId)
        {
            if (!IsServer) return;
            SpawnPlayerServerRpc(clientId);
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            if (!IsServer) return;

            if (spawnedPlayers.TryGetValue(clientId, out var player))
            {
                spawnedPlayers.Remove(clientId);
                if (player != null)
                {
                    player.NetworkObject.Despawn();
                    Destroy(player.gameObject);
                }
            }
        }

        private async UniTask Initialize()
        {
            var playerParentY = playerParent.position.y;
            
            playerColors = new List<Color>()
            {
                Color.cyan,
                Color.green,
                Color.red,
                Color.yellow
            };

            playerSpawns = new List<Vector3>()
            {
                new(2f, playerParentY, 2f),
                new(-2f, playerParentY, 2f),
                new(-2f, playerParentY, -2f),
                new(2f, playerParentY, -2f)
            };

            var currentLobby = MatchmakingService.GetCurrentLobby();
            if (currentLobby != null)
            {
                try
                {
                    await ChatManager.Instance.InitializeAsync();
                    await ChatManager.Instance.JoinPositionalChannel(currentLobby.Id);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to initialize chat: {e.Message}");
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnPlayerServerRpc(ulong playerId)
        {
            if (spawnedPlayers.ContainsKey(playerId))
            {
                Debug.LogWarning($"Player {playerId} already spawned");
                return;
            }

            var connectedClients = NetworkManager.Singleton.ConnectedClientsIds.ToList();
            var spawnIndex = connectedClients.IndexOf(playerId);
            if (spawnIndex < 0 || spawnIndex >= playerSpawns.Count)
            {
                Debug.LogError($"Invalid spawn index for player {playerId}");
                return;
            }

            var playerInstance = Instantiate(
                playerPrefab,
                playerSpawns[spawnIndex],
                Quaternion.identity,
                playerParent
            );

            var networkObject = playerInstance.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(playerId);

            var colorIndex = spawnIndex;
            if (colorIndex >= 0 && colorIndex < playerColors.Count)
            {
                playerInstance.SetColor(playerColors[colorIndex]);
            }

            spawnedPlayers[playerId] = playerInstance;
            Debug.Log($"Spawned player {playerId} at position {playerSpawns[spawnIndex]}");
        }
    }
}