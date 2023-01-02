using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Content.Scripts.GameCore.Base;
using Content.Scripts.GameCore.Base.Interfaces;
using Content.Scripts.Networking.Data;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.GameCore.Scenes.Root.Layouts
{
    public class CreateLobbyLayout : LayoutBase, ILayout
    {
        [Header("TEXTS")] 
        [SerializeField] private TextMeshProUGUI lobbyName;
        [Header("BUTTONS")] 
        [SerializeField] private Button createButton;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly Subject<LobbyData> onCreate = new Subject<LobbyData>();

        private LobbyData currentLobbyData;
        private Transform buttonsLayout;

        public IObservable<LobbyData> OnCreate => onCreate;

        internal override void Initialize()
        {
            buttonsLayout = transform.GetChild(0);

            createButton.OnClickAsObservable().Subscribe(_ => HandleCreate(currentLobbyData)).AddTo(disposables);
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public async Task SetLayoutVisible(bool value)
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

        public void SetButtonsInteractable(bool value)
        {
            createButton.interactable = value;
        }

        private void HandleCreate(LobbyData data)
        {
            data.Name = lobbyName.text;
            
            onCreate.OnNext(data);
        }
    }
}