using Content.Scripts.GameCore.Scenes.Common.Tools;
using Content.Scripts.GameCore.Scenes.Root.Layouts;
using Content.Scripts.GameCore.Scenes.Root.Other;
using Content.Scripts.GameCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Content.Scripts.GameCore.Scenes.Root.View
{
    public class FindLobbyController : MonoBehaviour
    {
        private readonly List<LobbyPanel> currentLobbySpawns = new();
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [SerializeField] private ListLobbyLayout listLobbyLayout;
        [SerializeField] private LobbyPanel lobbyPanelPrefab;
        [SerializeField] private Transform panelsParent;
        [SerializeField] private GameObject noLobbies;

        private readonly Subject<Lobby> onConnect = new();

        public IObservable<Lobby> OnConnect => onConnect;

        private void OnEnable()
        {
            InitializeListeners();

            foreach (Transform child in panelsParent) 
            {
                Destroy(child.gameObject);
            }

            currentLobbySpawns.Clear();
            FetchLobbies().Forget();
        }

        private void OnDestroy()
        {
            disposables?.Dispose();
        }

        private void InitializeListeners()
        {
            listLobbyLayout.OnRefresh.Subscribe(HandleRefresh).AddTo(disposables);
        }

        private void HandleConnect(Lobby lobby)
        {
            onConnect.OnNext(lobby);
        }

        private void HandleRefresh(Unit unit)
        {
            FetchLobbies().Forget();
        }

        private async UniTask FetchLobbies()
        {
            try
            {
                var allLobbies = await MatchmakingService.GatherLobbies();

                // Destroy all the current lobby panels which don't exist anymore
                var lobbyIds = allLobbies.Where(l => l.HostId != Authentication.Services.Authentication.PlayerId)
                    .Select(l => l.Id)
                    .ToHashSet();

                var notActive = currentLobbySpawns.Where(l => !lobbyIds.Contains(l.Lobby.Id)).ToList();

                foreach (var panel in notActive)
                {
                    Destroy(panel.gameObject);
                    currentLobbySpawns.Remove(panel);
                }

                // Update or spawn the remaining active lobbies
                foreach (var lobby in allLobbies)
                {
                    if (lobby.HostId == Authentication.Services.Authentication.PlayerId)
                        continue;

                    var current = currentLobbySpawns.FirstOrDefault(p => p.Lobby.Id == lobby.Id);
                    if (current != null)
                    {
                        current.UpdateDetails(lobby);
                    }
                    else
                    {
                        var panel = Instantiate(lobbyPanelPrefab, panelsParent);
                        panel.Initialize(lobby);
                        panel.Button.OnClickAsObservable().Subscribe(_ => HandleConnect(lobby)).AddTo(disposables);
                        currentLobbySpawns.Add(panel);
                    }
                }

                noLobbies.SetActive(!currentLobbySpawns.Any());
            }
            catch (Exception e)
            {
                CanvasUtilities.Instance.ShowError(e, "Can't fetch lobbies");
            }
        }
    }
}
