using Content.Scripts.GameCore.Base;
using Content.Scripts.GameCore.Scenes.Root.Other;
using Content.Scripts.Networking.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Layouts
{
    public class LobbyLayout : LayoutBase
    {
        private readonly List<LobbyPlayerPanel> playerPanels = new();

        private readonly CompositeDisposable disposables = new();

        private readonly Subject<Unit> onReady = new();
        private readonly Subject<Unit> onPlay = new();
        private readonly Subject<Unit> onSwitchMicro = new();

        [Header("ART")]
        [SerializeField] private Sprite microOn;
        [SerializeField] private Sprite microOff;

        [Header("TEXT")]
        [SerializeField] private TextMeshProUGUI lobbyTitle;

        [Header("BUTTONS")]
        [SerializeField] private Button readyButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button switchMicroButton;

        [Header("OTHER")]
        [SerializeField] private LobbyPlayerPanel playerPanelPrefab;
        [SerializeField] private Transform playerPanelParent;
        [SerializeField] private Image microSwitchImage;


        private Transform buttonsLayout;

        private bool ready;

        public IObservable<Unit> OnReady => onReady;
        public IObservable<Unit> OnPlay => onPlay;
        public IObservable<Unit> OnSwitchMicro => onSwitchMicro;

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

        public void UpdateMicroSwitchButtonState(bool enabled)
        {
            microSwitchImage.sprite = enabled ? microOn : microOff;
        }

        public override async Task SetLayoutVisible(bool value)
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

        public override void SetButtonsInteractable(bool value)
        {
            readyButton.interactable = value;
            playButton.interactable = value;
            switchMicroButton.interactable = value;
        }

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);

            UpdateMicroSwitchButtonState(true);

            readyButton.OnClickAsObservable().Subscribe(HandleReady).AddTo(disposables);
            playButton.OnClickAsObservable().Subscribe(HandlePlay).AddTo(disposables);
            switchMicroButton.OnClickAsObservable().Subscribe(HandleSwitchMicro).AddTo(disposables);
        }

        protected override void OnLayoutShowing()
        {
            readyButton.Select();
        }

        private void UpdateButtons(Dictionary<ulong, bool> players)
        {
            var allPlayersReady = NetworkManager.Singleton.IsHost && players.All(p => p.Value);

            playButton.gameObject.SetActive(allPlayersReady);
            readyButton.gameObject.SetActive(!ready);
        }

        private void HandleReady(Unit unit)
        {
            onReady.OnNext(unit);
        }

        private void HandlePlay(Unit unit)
        {
            onPlay.OnNext(unit);
        }

        private void HandleSwitchMicro(Unit unit)
        {
            onSwitchMicro.OnNext(unit);
        }
    }
}