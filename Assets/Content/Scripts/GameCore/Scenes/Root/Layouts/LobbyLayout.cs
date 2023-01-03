using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Base;
using Content.Scripts.GameCore.Base.Interfaces;
using Content.Scripts.GameCore.Scenes.Root.Other;
using Content.Scripts.Networking.Data;
using TMPro;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Content.Scripts.GameCore.Scenes.Root.Layouts
{
    public class LobbyLayout : LayoutBase, ILayout
    {
        private readonly List<LobbyPlayerPanel> playerPanels = new();

        private readonly CompositeDisposable disposables = new();

        private readonly Subject<Unit> onReady = new();
        private readonly Subject<Unit> onPlay = new();
        
        [Header("TEXT")] [SerializeField] private TextMeshProUGUI lobbyTitle;

        [Header("BUTTONS")] [SerializeField] private Button readyButton;
        [SerializeField] private Button playButton;
        
        [SerializeField] private LobbyPlayerPanel playerPanelPrefab;
        [SerializeField] private Transform playerPanelParent;

        private Transform buttonsLayout;

        private bool everyoneReady;
        private bool ready;

        public IObservable<Unit> OnReady => onReady;
        public IObservable<Unit> OnPlay => onPlay;

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);

            readyButton.OnClickAsObservable().Subscribe(HandleReady).AddTo(disposables);
            playButton.OnClickAsObservable().Subscribe(HandlePlay).AddTo(disposables);
        }

        private void OnDestroy()
        {
            disposables?.Dispose();
        }

        public void UpdateLobbyData(LobbyData data)
        {
            lobbyTitle.text = data.Name;
        }

        public void UpdateLobby(Dictionary<ulong, bool> players)
        {
            var allActivePlayerIds = players.Keys;
            var toDestroy = playerPanels.Where(p => !allActivePlayerIds.Contains(p.PlayerId)).ToList();
            
            foreach (var panel in toDestroy)
            {
                playerPanels.Remove(panel);
                Destroy(panel.gameObject);
            }

            foreach (var player in players)
            {
                var currentPanel = playerPanels.FirstOrDefault(p => p.PlayerId == player.Key);

                if (currentPanel != null)
                {
                    currentPanel.UpdateReadyButton(player.Value);
                }
                else
                {
                    var panel = Instantiate(playerPanelPrefab, playerPanelParent);
                    
                    panel.Initialize(player.Key);
                    playerPanels.Add(panel);
                }
            }

            UpdateButtons(players);
        }

        private void UpdateButtons(Dictionary<ulong, bool> players)
        {
            var allPlayersReady = NetworkManager.Singleton.IsHost && players.All(p => p.Value);
            
            playButton.gameObject.SetActive(allPlayersReady);
            readyButton.gameObject.SetActive(!ready);
        }

        public async Task SetLayoutVisible(bool value)
        {
            try
            {
                if (value)
                {
                    await ShowLayout(buttonsLayout);
                }
                else
                {
                    await HideLayout(buttonsLayout);
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception);
            }
        }

        public void SetButtonsInteractable(bool value)
        {
            readyButton.interactable = value;
            playButton.interactable = value;
        }

        private void HandleReady(Unit unit)
        {
            onReady.OnNext(unit);
        }

        private void HandlePlay(Unit unit)
        {
            onPlay.OnNext(unit);
        }
    }
}