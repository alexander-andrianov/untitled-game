using System;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Base;
using Content.Scripts.GameCore.Base.Interfaces;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Layouts
{
    public class PlaymodeLayout : LayoutBase, ILayout
    {
        [Header("BUTTONS")]
        [SerializeField]
        private Button createLobbyButton;

        [SerializeField] 
        private Button findLobbyButton;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Subject<Unit> onCreateLobby = new Subject<Unit>();
        private readonly Subject<Unit> onFindLobby = new Subject<Unit>();

        private Transform buttonsLayout;

        public IObservable<Unit> OnCreateLobby => onCreateLobby;
        public IObservable<Unit> OnFindLobby => onFindLobby;

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);
            
            createLobbyButton.OnClickAsObservable().Subscribe(HandleLobbyCreation).AddTo(disposables);
            findLobbyButton.OnClickAsObservable().Subscribe(HandleLobbyFinding).AddTo(disposables);
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public async Task SetLayoutVisible(bool value)
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
        
        public void SetButtonsInteractable(bool value) {
            createLobbyButton.interactable = value;
            findLobbyButton.interactable = value;
        }

        private void HandleLobbyCreation(Unit unit)
        {
            onCreateLobby.OnNext(unit);
        }

        private void HandleLobbyFinding(Unit unit)
        {
            onFindLobby.OnNext(unit);
        }
    }
}