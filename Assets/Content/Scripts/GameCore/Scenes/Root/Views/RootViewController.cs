using Content.Scripts.GameCore.Base.Interfaces;
using Content.Scripts.GameCore.Scenes.Common.Enums;
using Content.Scripts.GameCore.Scenes.Common.Layouts;
using Content.Scripts.GameCore.Scenes.Common.Tools;
using Content.Scripts.GameCore.Scenes.Root.Layouts;
using Content.Scripts.GameCore.Scenes.Root.View;
using Content.Scripts.GameCore.Services;
using Content.Scripts.Networking.Data;
using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using UniRx;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Content.Scripts.GameCore.Scenes.Root.Views
{
    public class RootViewController : MonoBehaviour
    {
        private const string LoadingText = "Loading...";
        private const string LeavingLobbyText = "Leaving...";
        private const string AuthenticationSceneName = "Authentication";

        [Header("LAYOUTS")][SerializeField] private StartLayout startLayout;
        [SerializeField] private SettingsLayout settingsLayout;
        [SerializeField] private ListLobbyLayout listLobbyLayout;
        [SerializeField] private CreateLobbyLayout createLobbyLayout;
        [SerializeField] private LobbyLayout lobbyLayout;
        [SerializeField] private NavigationLayout navigationLayout;

        [Header("CONTROLLERS")]
        [SerializeField] private LobbyController lobbyController;
        [SerializeField] private FindLobbyController findLobbyController;
        [SerializeField] private NavigationController navigationController;

        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly Subject<LayoutType> layoutChange = new();

        private ILayout currentLayout;
        private LayoutType currentLayoutType;

        public IObservable<LayoutType> LayoutChange => layoutChange;

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public async Task Initialize()
        {
            InitializeLayouts();
            InitializeControllers();
            InitializeObservableListeners();

            currentLayout = startLayout;
            currentLayoutType = LayoutType.Start;

            await ShowLayoutView(currentLayout);
            await ShowLayoutView(navigationLayout);
        }

        private void InitializeLayouts()
        {
            startLayout.Initialize();
            settingsLayout.Initialize();
            lobbyLayout.Initialize();
            listLobbyLayout.Initialize();
            navigationLayout.Initialize();
            createLobbyLayout.Initialize();
        }

        private void InitializeControllers()
        {
            navigationController.Initialize();
            
            // Subscribe to lobby events
            lobbyController.OnLobbyLeft
                .Subscribe(_ => ReturnToDefalut())
                .AddTo(disposables);
                
            lobbyController.OnLobbyDeleted
                .Subscribe(_ => ReturnToDefalut())
                .AddTo(disposables);
                
            lobbyController.OnKicked
                .Subscribe(_ => ReturnToDefalut())
                .AddTo(disposables);
                
            lobbyController.OnConnectionStateChanged
                .Where(state => state == Unity.Services.Lobbies.LobbyEventConnectionState.Error)
                .Subscribe(_ => ReturnToDefalut())
                .AddTo(disposables);
        }

        private void InitializeObservableListeners()
        {
            startLayout.OnPlay.Subscribe(_ => HandleSwitch(LayoutType.ListLobby)).AddTo(disposables);
            startLayout.OnSettings.Subscribe(_ => HandleSwitch(LayoutType.Settings)).AddTo(disposables);
            startLayout.OnExit.Subscribe(HandleExit).AddTo(disposables);

            findLobbyController.OnConnect.Subscribe(HandleConnectToLobby).AddTo(disposables);

            listLobbyLayout.OnCreateLobby.Subscribe(_ => HandleSwitch(LayoutType.CreateLobby)).AddTo(disposables);

            createLobbyLayout.OnCreate.Subscribe(HandleCreateLobby).AddTo(disposables);

            navigationLayout.OnBack.Subscribe(HandleReturn).AddTo(disposables);
        }

        private async Task ShowLayoutView(ILayout layout)
        {
            await layout.SetLayoutVisible(true);
        }

        private async Task HideLayoutView(ILayout layout)
        {
            await layout.SetLayoutVisible(false);
        }

        private async Task SwitchLayout(LayoutType layoutType)
        {
            if (currentLayout == (ILayout)lobbyLayout)
            {
                try
                {
                    await CanvasUtilities.Instance.Toggle(true, LoadingText);

                    lobbyController.ClearPlayers();
                    NetworkManager.Singleton.Shutdown();
                    await MatchmakingService.LeaveLobby();

                    await HideLayoutView(currentLayout);

                    await CanvasUtilities.Instance.Toggle(false, LoadingText);

                    currentLayout = GetLayoutByType(layoutType);
                    currentLayoutType = layoutType;

                    layoutChange.OnNext(layoutType);

                    await ShowLayoutView(currentLayout);
                }
                catch (Exception e)
                {
                    CanvasUtilities.Instance.ShowError(e, "Failed to return to previous layout");
                    ReturnToDefalut();
                }

                return;
            }

            await HideLayoutView(currentLayout);

            currentLayout = GetLayoutByType(layoutType);
            currentLayoutType = layoutType;

            layoutChange.OnNext(layoutType);

            await ShowLayoutView(currentLayout);
        }

        private async Task ReturnToPreviousLayout()
        {
            var targetLayout = GetPreviousLayoutTypeByCurrentType(currentLayoutType);
            await SwitchLayout(targetLayout);
        }

        private async void HandleSwitch(LayoutType nextLayoutType)
        {
            await SwitchLayout(nextLayoutType);
        }

        private async void HandleCreateLobby(LobbyData data)
        {
            try
            {
                await CanvasUtilities.Instance.Toggle(true, LoadingText);
                await MatchmakingService.CreateLobbyWithAllocation(data);
                await HideLayoutView(currentLayout);

                lobbyLayout.UpdateLobbyData(data);
                currentLayout = GetLayoutByType(LayoutType.Lobby);
                currentLayoutType = LayoutType.Lobby;

                await ShowLayoutView(currentLayout);

                NetworkManager.Singleton.StartHost();
            }
            catch (Exception e)
            {
                CanvasUtilities.Instance.ShowError(e, "Failed creating lobby");
                ReturnToDefalut();
            }
        }

        private async void HandleReturn(Unit unit)
        {
            await ReturnToPreviousLayout();
        }

        private async void HandleConnectToLobby(Lobby lobby)
        {
            try
            {
                await CanvasUtilities.Instance.Toggle(true, LoadingText);
                await MatchmakingService.JoinLobbyWithAllocation(lobby.Id);

                await HideLayoutView(currentLayout);

                currentLayout = GetLayoutByType(LayoutType.Lobby);
                currentLayoutType = LayoutType.Lobby;

                await CanvasUtilities.Instance.Toggle(false, LoadingText);

                NetworkManager.Singleton.StartClient();

                await ShowLayoutView(currentLayout);
            }
            catch (Exception e)
            {
                CanvasUtilities.Instance.ShowError(e, "Failed joining lobby");
                ReturnToDefalut();
            }
        }

        private void HandleExit(Unit unit)
        {
            startLayout.SetButtonsInteractable(false);

            #if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
            #else
                Application.Quit();
            #endif
        }

        private async void ReturnToDefalut()
        {
            await HideLayoutView(currentLayout);
            await CanvasUtilities.Instance.Toggle(false, LoadingText);

            currentLayout = GetLayoutByType(LayoutType.Start);
            currentLayoutType = LayoutType.Start;

            layoutChange.OnNext(LayoutType.Start);

            await ShowLayoutView(currentLayout);
        }

        private LayoutType GetPreviousLayoutTypeByCurrentType(LayoutType layoutType)
        {
            return layoutType switch
            {
                LayoutType.Settings => LayoutType.Start,
                LayoutType.ListLobby => LayoutType.Start,
                LayoutType.CreateLobby => LayoutType.ListLobby,
                LayoutType.Lobby => LayoutType.ListLobby,
                _ => LayoutType.Start
            };
        }

        private ILayout GetLayoutByType(LayoutType layoutType)
        {
            return layoutType switch
            {
                LayoutType.Start => startLayout,
                LayoutType.Settings => settingsLayout,
                LayoutType.ListLobby => listLobbyLayout,
                LayoutType.CreateLobby => createLobbyLayout,
                LayoutType.Lobby => lobbyLayout,
                _ => startLayout
            };
        }
    }
}