using System.Collections.Generic;
using Content.Scripts.Gamecore.Base.Structs;
using Content.Scripts.GameCore.Scenes.Game.Controllers;
using Content.Scripts.GameCore.Services;
using Unity.Netcode;
using UnityEngine;
using VivoxUnity;

namespace Content.Scripts.GameCore.Scenes.Game.Entry
{
    public class GameEntry : NetworkBehaviour
    {
        [SerializeField] private PlayerController playerPrefab;
        [SerializeField] private Transform playerParent;

        private List<Color> playerColors;
        private List<Vector3> playerSpawns;

        public override async void OnDestroy()
        {
            base.OnDestroy();
            await MatchmakingService.LeaveLobby();

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        public override void OnNetworkSpawn()
        {
            Initialize();
            SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        private void Initialize()
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
            
            ChatManager.Instance.LeaveChannel();
            
            var channelName = MatchmakingService.GetCurrentLobby().Id;
            var channelSettings = new ChannelSettings
            {
                Name = channelName,
                Type = ChannelType.Positional,
                ConnectAudio = true,
                ConnectText = false,
                SwitchToThisChannel = true
            };

            var channel3DProperties = new Channel3DProperties(10, 5, 5, AudioFadeModel.InverseByDistance);

            ChatManager.Instance.JoinPositionalChannel(channelSettings, channel3DProperties);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnPlayerServerRpc(ulong playerId)
        {
            var player = Instantiate(playerPrefab, playerSpawns[0], Quaternion.identity, playerParent);
            var teamColor = playerColors[0];

            playerSpawns.RemoveAt(0);
            playerColors.RemoveAt(0);

            player.PlayerLight.color = teamColor;
            // player.GetComponent<NetworkObject>().Spawn();
            player.NetworkObject.SpawnWithOwnership(playerId);
        }

        // [ServerRpc(RequireOwnership = false)]
        // private void SetTeamServerRpc(ulong playerId)
        // {
        //     var teamColor = playerColors[0];
        //     playerColors.RemoveAt(0);
        //     
        // }
    }
}