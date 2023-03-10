using Assets.Content.Scripts.Extensions.Actions;
using Content.Scripts.GameCore.Base;
using System;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Common.Layouts
{
    public class NavigationLayout : LayoutBase
    {
        [Header("BUTTONS")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button logoutButton;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Subject<Unit> onBack = new();

        private Transform buttonsLayout;
        private PlayerInput playerInput;

        public IObservable<Unit> OnBack => onBack;

        private void OnEnable()
        {
            playerInput?.Enable();
        }

        private void OnDisable()
        {
            playerInput?.Disable();
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public override async Task SetLayoutVisible(bool value)
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

        public override void SetButtonsInteractable(bool value)
        {
            backButton.interactable = value;
        }

        internal override void Initialize()
        {
            playerInput = new PlayerInput();
            buttonsLayout = transform.GetChild(0);

            backButton.OnClickAsObservable().Subscribe(HandleBack).AddTo(disposables);
            logoutButton.OnClickAsObservable().Subscribe(HandleBack).AddTo(disposables);

            playerInput.UI.Cancel.ToObservable().Subscribe(_ => HandleBack(default)).AddTo(disposables);
        }

        private void HandleBack(Unit unit)
        {
            onBack.OnNext(unit);
        }
    }
}