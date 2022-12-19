using System;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Base.Interfaces;
using Content.Scripts.GameCore.Data;
using Content.Scripts.GameCore.Scenes.Common.Enums;
using Content.Scripts.GameCore.Scenes.Common.Layouts;
using Content.Scripts.GameCore.Scenes.Common.Tools;
using Content.Scripts.GameCore.Scenes.Root.Layouts;
using Content.Scripts.GameCore.Services;
using Cysharp.Threading.Tasks;
using UniRx;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Content.Scripts.GameCore.Scenes.Root.View
{
    public class RootViewController : MonoBehaviour
    {
        private const string LoadingText = "Loading...";
        private const string LeavingLobbyText = "Leaving...";
        private const string AuthenticationSceneName = "Authentication";
        private const string GameSceneName = "Game";

        [Header("LAYOUTS")] [SerializeField] private StartLayout startLayout;
        [SerializeField] private PlaymodeLayout playmodeLayout;
        [SerializeField] private SettingsLayout settingsLayout;
        [SerializeField] private CreateLobbyLayout createLobbyLayout;
        [SerializeField] private FindLobbyLayout findLobbyLayout;
        [SerializeField] private LobbyLayout lobbyLayout;
        [SerializeField] private ReturnLayout returnLayout;

        [Header("CONTROLLERS")] 
        [SerializeField] private LobbyController lobbyController;
        [SerializeField] private FindLobbyController findLobbyController;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private ILayout currentLayout;

        private LayoutType currentLayoutType;

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public async Task Initialize()
        {
            InitializeLayouts();
            InitializeObservableListeners();

            currentLayout = startLayout;
            currentLayoutType = LayoutType.Start;

            await ShowLayoutView(currentLayout);
            await ShowLayoutView(returnLayout);
        }

        private void InitializeLayouts()
        {
            startLayout.Initialize();
            playmodeLayout.Initialize();
            settingsLayout.Initialize();
            createLobbyLayout.Initialize();
            lobbyLayout.Initialize();
            findLobbyLayout.Initialize();
            returnLayout.Initialize();
        }

        private void InitializeObservableListeners()
        {
            startLayout.OnPlay.Subscribe(_ => HandleSwitch(LayoutType.Playmode)).AddTo(disposables);
            startLayout.OnSettings.Subscribe(_ => HandleSwitch(LayoutType.Settings)).AddTo(disposables);
            startLayout.OnExit.Subscribe(HandleExit).AddTo(disposables);

            playmodeLayout.OnCreateLobby.Subscribe(_ => HandleSwitch(LayoutType.CreateLobby)).AddTo(disposables);
            playmodeLayout.OnFindLobby.Subscribe(_ => HandleSwitch(LayoutType.FindLobby)).AddTo(disposables);

            createLobbyLayout.OnCreate.Subscribe(HandleLobbyLayout).AddTo(disposables);
            lobbyLayout.OnCreate.Subscribe(HandleStartGame).AddTo(disposables);

            findLobbyController.OnConnect.Subscribe(HandleConnectToLobby).AddTo(disposables);

            returnLayout.OnBack.Subscribe(HandleReturn).AddTo(disposables);
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
            // TO REFACTOR
            if (currentLayout == lobbyLayout)
            {
                var loader = new SceneLoader();

                using (loader)
                {
                    try
                    {
                        await loader.ShowLoader(LeavingLobbyText);

                        lobbyController.ClearPlayers();
                        NetworkManager.Singleton.Shutdown();
                        await MatchmakingService.LeaveLobby();

                        await HideLayoutView(currentLayout);
                        await loader.HideLoader(LeavingLobbyText);

                        currentLayout = GetLayoutByType(layoutType);
                        currentLayoutType = layoutType;

                        await ShowLayoutView(currentLayout);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        CanvasUtilities.Instance.ShowError("Failed to return to previous layout");
                    }
                }

                return;
            }

            await HideLayoutView(currentLayout);

            currentLayout = GetLayoutByType(layoutType);
            currentLayoutType = layoutType;

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

        private async void HandleLobbyLayout(LobbyData data)
        {
            var loader = new SceneLoader();

            using (loader)
            {
                try
                {
                    await loader.ShowLoader(LoadingText);
                    await MatchmakingService.CreateLobbyWithAllocation(data);
                    await HideLayoutView(currentLayout);

                    lobbyLayout.UpdateLobbyData(data);
                    currentLayout = GetLayoutByType(LayoutType.Lobby);
                    currentLayoutType = LayoutType.Lobby;

                    await ShowLayoutView(currentLayout);

                    NetworkManager.Singleton.StartHost();
                    await loader.HideLoader(LoadingText);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    CanvasUtilities.Instance.ShowError("Failed creating lobby");
                }
            }
        }

        private async void HandleReturn(Unit unit)
        {
            if (currentLayoutType == LayoutType.Start)
            {
                var loader = new SceneLoader();

                using (loader)
                {
                    try
                    {
                        await loader.ShowLoader(LoadingText);
                        await SceneManager.LoadSceneAsync(AuthenticationSceneName);
                        await loader.HideLoader(LoadingText);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        CanvasUtilities.Instance.ShowError("Failed to return");
                    }
                }

                return;
            }

            await ReturnToPreviousLayout();
        }

        private async void HandleStartGame(Unit unit)
        {
            var loader = new SceneLoader();

            using (loader)
            {
                try
                {
                    await loader.ShowLoader(LoadingText);
                    await SceneManager.LoadSceneAsync(GameSceneName);
                    await loader.HideLoader(LoadingText);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    CanvasUtilities.Instance.ShowError("Failed to start the game");
                }
            }
        }

        private async void HandleConnectToLobby(Lobby lobby)
        {
            var loader = new SceneLoader();

            using (loader)
            {
                try
                {
                    await loader.ShowLoader(LoadingText);
                    await MatchmakingService.JoinLobbyWithAllocation(lobby.Id);

                    await HideLayoutView(currentLayout);

                    currentLayout = GetLayoutByType(LayoutType.Lobby);
                    currentLayoutType = LayoutType.Lobby;

                    await ShowLayoutView(currentLayout);

                    NetworkManager.Singleton.StartClient();
                    await loader.HideLoader(LoadingText);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    CanvasUtilities.Instance.ShowError("Failed joining lobby");
                }
            }
        }

        private void HandleExit(Unit unit)
        {
            startLayout.SetButtonsInteractable(false);
            Application.Quit();
        }

        private LayoutType GetPreviousLayoutTypeByCurrentType(LayoutType layoutType)
        {
            return layoutType switch
            {
                LayoutType.Start => default,
                LayoutType.Playmode => LayoutType.Start,
                LayoutType.CreateLobby => LayoutType.Playmode,
                LayoutType.FindLobby => LayoutType.Playmode,
                LayoutType.Lobby => LayoutType.CreateLobby,
                LayoutType.Settings => LayoutType.Start,
                _ => throw new ArgumentOutOfRangeException(nameof(layoutType), layoutType, null)
            };
        }

        private ILayout GetLayoutByType(LayoutType layoutType)
        {
            return layoutType switch
            {
                LayoutType.Playmode => playmodeLayout,
                LayoutType.CreateLobby => createLobbyLayout,
                LayoutType.FindLobby => findLobbyLayout,
                LayoutType.Start => startLayout,
                LayoutType.Lobby => lobbyLayout,
                LayoutType.Settings => settingsLayout,
                _ => throw new ArgumentOutOfRangeException(nameof(layoutType), layoutType, null)
            };
        }
    }
}