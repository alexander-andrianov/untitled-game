using Content.Scripts.GameCore.Base;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Layouts
{
    public class StartLayout : LayoutBase
    {
        [Header("BUTTONS")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Subject<Unit> onPlay = new Subject<Unit>();
        private readonly Subject<Unit> onSettings = new Subject<Unit>();
        private readonly Subject<Unit> onExit = new Subject<Unit>();

        private Transform buttonsLayout;

        public IObservable<Unit> OnPlay => onPlay;
        public IObservable<Unit> OnSettings => onSettings;
        public IObservable<Unit> OnExit => onExit;

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
            playButton.interactable = value;
            settingsButton.interactable = value;
            exitButton.interactable = value;
        }

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);

            playButton.OnClickAsObservable().Subscribe(HandlePlay).AddTo(disposables);
            settingsButton.OnClickAsObservable().Subscribe(HandleSettings).AddTo(disposables);
            exitButton.OnClickAsObservable().Subscribe(HandleExit).AddTo(disposables);
        }

        protected override void OnLayoutShowing()
        {
            playButton.Select();
        }

        private void HandlePlay(Unit unit)
        {
            onPlay.OnNext(unit);
        }

        private void HandleSettings(Unit unit)
        {
            onSettings.OnNext(unit);
        }

        private void HandleExit(Unit unit)
        {
            onExit.OnNext(unit);
        }
    }
}