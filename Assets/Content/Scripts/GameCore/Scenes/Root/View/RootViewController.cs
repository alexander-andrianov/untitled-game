using System;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Base.Interfaces;
using Content.Scripts.GameCore.Scenes.Common.Enums;
using Content.Scripts.GameCore.Scenes.Common.Layouts;
using Content.Scripts.GameCore.Scenes.Common.Tools;
using Content.Scripts.GameCore.Scenes.Root.Layouts;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Content.Scripts.GameCore.Scenes.Root.View
{
    public class RootViewController : MonoBehaviour
    {
        private const string LoadingText = "Loading...";
        private const string AuthenticationSceneName = "Authentication";

        [Header("LAYOUTS")] [SerializeField] private StartLayout startLayout;

        [SerializeField] private PlaymodeLayout playmodeLayout;

        [SerializeField] private SettingsLayout settingsLayout;

        [SerializeField] private CreateLobbyLayout createLobbyLayout;

        [SerializeField] private FindLobbyLayout findLobbyLayout;

        [SerializeField] private ReturnLayout returnLayout;

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
            returnLayout.Initialize();
        }

        private void InitializeObservableListeners()
        {
            startLayout.OnPlay.Subscribe(HandlePlay).AddTo(disposables);
            startLayout.OnSettings.Subscribe(HandleSettings).AddTo(disposables);
            startLayout.OnExit.Subscribe(HandleExit).AddTo(disposables);

            playmodeLayout.OnCreateLobby.Subscribe(HandleCreateLobby).AddTo(disposables);
            playmodeLayout.OnFindLobby.Subscribe(HandleFindLobby).AddTo(disposables);

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

        private async void HandlePlay(Unit unit)
        {
            await SwitchLayout(LayoutType.Playmode);
        }

        private async void HandleSettings(Unit unit)
        {
            await SwitchLayout(LayoutType.Settings);
        }

        private async void HandleCreateLobby(Unit unit)
        {
            await SwitchLayout(LayoutType.CreateLobby);
        }

        private async void HandleFindLobby(Unit unit)
        {
            await SwitchLayout(LayoutType.FindLobby);
        }

        private async void HandleReturn(Unit unit)
        {
            if (currentLayoutType == LayoutType.Start)
            {
                using var loader = new SceneLoader();

                await loader.ShowLoader(LoadingText);
                await SceneManager.LoadSceneAsync(AuthenticationSceneName);
                await loader.HideLoader(LoadingText);

                return;
            }

            await ReturnToPreviousLayout();
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
                LayoutType.Settings => settingsLayout,
                _ => throw new ArgumentOutOfRangeException(nameof(layoutType), layoutType, null)
            };
        }
    }
}