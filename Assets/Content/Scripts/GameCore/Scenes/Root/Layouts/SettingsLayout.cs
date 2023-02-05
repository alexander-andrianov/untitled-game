using Content.Scripts.GameCore.Base;
using System;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Layouts
{
    public class SettingsLayout : LayoutBase
    {
        [Header("BUTTONS")]
        [SerializeField] private Button audioButton;
        [SerializeField] private Button videoButton;
        [SerializeField] private Button controlButton;
        [SerializeField] private Button languageButton;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Subject<Unit> onAudio = new Subject<Unit>();
        private readonly Subject<Unit> onVideo = new Subject<Unit>();
        private readonly Subject<Unit> onControl = new Subject<Unit>();
        private readonly Subject<Unit> onLanguage = new Subject<Unit>();

        private Transform buttonsLayout;

        public IObservable<Unit> OnAudio => onAudio;
        public IObservable<Unit> OnVideo => onVideo;
        public IObservable<Unit> OnControl => onControl;
        public IObservable<Unit> OnLanguage => onLanguage;

        private void OnDisable()
        {
            disposables.Clear();
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public override async Task SetLayoutVisible(bool value)
        {
            SetButtonsInteractable(value);

            if (value)
            {
                await ShowLayout(buttonsLayout);
            }
            else
            {
                await HideLayout(buttonsLayout);
            }
        }

        public override void SetButtonsInteractable(bool value)
        {
            audioButton.interactable = value;
            videoButton.interactable = value;
            controlButton.interactable = value;
            languageButton.interactable = value;
        }

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);

            audioButton.OnClickAsObservable().Subscribe(HandleAudio).AddTo(disposables);
            videoButton.OnClickAsObservable().Subscribe(HandleVideo).AddTo(disposables);
            controlButton.OnClickAsObservable().Subscribe(HandleControl).AddTo(disposables);
            languageButton.OnClickAsObservable().Subscribe(HandleLanguage).AddTo(disposables);
        }

        protected override void OnLayoutShowing()
        {
            audioButton.Select();
        }

        private void HandleAudio(Unit unit)
        {
            onAudio.OnNext(unit);
        }

        private void HandleVideo(Unit unit)
        {
            onVideo.OnNext(unit);
        }

        private void HandleControl(Unit unit)
        {
            onControl.OnNext(unit);
        }

        private void HandleLanguage(Unit unit)
        {
            onLanguage.OnNext(unit);
        }
    }
}