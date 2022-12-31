using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Scenes.Root.Other;
using Content.Scripts.GameCore.Scenes.Root.Layouts;
using Content.Scripts.GameCore.Services;
using UniRx;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Content.Scripts.GameCore.Scenes.Root.View
{
    public class FindLobbyController : MonoBehaviour
    {
        private readonly List<LobbyPanel> currentLobbySpawns = new();
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        
        [SerializeField] private FindLobbyLayout findLobbyLayout;
        [SerializeField] private LobbyPanel lobbyPanelPrefab;
        [SerializeField] private Transform panelsParent;
        [SerializeField] private GameObject noLobbies;
        
        private readonly Subject<Lobby> onConnect = new Subject<Lobby>();
        
        public IObservable<Lobby> OnConnect => onConnect;
        
        private async void OnEnable()
        {
            InitializeListeners();
            
            foreach (Transform child in panelsParent) Destroy(child.gameObject);
            
            currentLobbySpawns.Clear();
            await FetchLobbies();
        }
        
        private void InitializeListeners()
        {
            findLobbyLayout.OnRefresh.Subscribe(HandleRefresh).AddTo(disposables);
        }
        
        private void HandleConnect(Lobby lobby)
        {
            onConnect.OnNext(lobby);
        }

        private async void HandleRefresh(Unit unit)
        {
            await FetchLobbies();
        }

        private async Task FetchLobbies() {
            try {
                // Grab all current lobbies
                var allLobbies = await MatchmakingService.GatherLobbies();

                // Destroy all the current lobby panels which don't exist anymore.
                // Exclude our own homes as it'll show for a brief moment after closing the room
                var lobbyIds = allLobbies.Where(l => l.HostId != Authentication.Services.Authentication.PlayerId).Select(l => l.Id);
                var notActive = currentLobbySpawns.Where(l => !lobbyIds.Contains(l.Lobby.Id)).ToList();

                foreach (var panel in notActive) {
                    Destroy(panel.gameObject);
                    currentLobbySpawns.Remove(panel);
                }

                // Update or spawn the remaining active lobbies
                foreach (var lobby in allLobbies) {
                    var current = currentLobbySpawns.FirstOrDefault(p => p.Lobby.Id == lobby.Id);
                    if (current != null) {
                        current.UpdateDetails(lobby);
                    }
                    else {
                        var panel = Instantiate(lobbyPanelPrefab, panelsParent);
                        panel.Initialize(lobby);
                        panel.Button.OnClickAsObservable().Subscribe(_ => HandleConnect(lobby)).AddTo(disposables);
                        currentLobbySpawns.Add(panel);
                    }
                }

                noLobbies.SetActive(!currentLobbySpawns.Any());
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }
    }
}
