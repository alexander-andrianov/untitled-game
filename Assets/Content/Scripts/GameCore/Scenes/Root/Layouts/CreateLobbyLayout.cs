using Content.Scripts.GameCore.Base;
using Content.Scripts.Networking.Data;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Layouts
{
    public class CreateLobbyLayout : LayoutBase
    {
        [Header("TEXTS")]
        [SerializeField] private TextMeshProUGUI lobbyName;
        [Header("BUTTONS")]
        [SerializeField] private Button createButton;
        [SerializeField] private TMP_InputField inputField;

        private readonly CompositeDisposable disposables = new();
        
        private readonly Subject<LobbyData> onCreate = new();

        private Transform buttonsLayout;

        public IObservable<LobbyData> OnCreate => onCreate;

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
            createButton.interactable = value;
        }

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);

            createButton.OnClickAsObservable().Subscribe(_ => HandleCreate(default)).AddTo(disposables);
        }

        protected override void OnLayoutShowing()
        {
            Setup();
        }

        private void Setup()
        {
            inputField.text = string.Empty;

            inputField.Select();
        }

        private void HandleCreate(LobbyData data)
        {
            data.Name = lobbyName.text;

            onCreate.OnNext(data);
        }
    }
}