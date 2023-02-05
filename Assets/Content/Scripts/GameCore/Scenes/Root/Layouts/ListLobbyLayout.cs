using Content.Scripts.GameCore.Base;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Layouts
{
    public class ListLobbyLayout : LayoutBase
    {
        [Header("BUTTONS")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button createLobbyButton;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Subject<Unit> onRefresh = new Subject<Unit>();
        private readonly Subject<Unit> onCreateLobby = new Subject<Unit>();

        private Transform buttonsLayout;

        public IObservable<Unit> OnRefresh => onRefresh;
        public IObservable<Unit> OnCreateLobby => onCreateLobby;

        private void OnDestroy()
        {
            disposables.Dispose();
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
            refreshButton.interactable = value;
        }

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);

            refreshButton.OnClickAsObservable().Subscribe(HandleRefresh).AddTo(disposables);
            createLobbyButton.OnClickAsObservable().Subscribe(HandleLobbyCreation).AddTo(disposables);
        }

        protected override void OnLayoutShowing()
        {
            refreshButton.Select();
        }

        private void HandleRefresh(Unit unit)
        {
            onRefresh.OnNext(unit);
        }

        private void HandleLobbyCreation(Unit unit)
        {
            onCreateLobby.OnNext(unit);
        }
    }
}