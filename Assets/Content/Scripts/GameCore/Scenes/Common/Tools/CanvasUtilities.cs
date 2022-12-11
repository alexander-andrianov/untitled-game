using System;
using System.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;

namespace Content.Scripts.GameCore.Scenes.Common.Tools
{
    /// <summary>
    ///     Handles the load and error screens
    /// </summary>
    public class CanvasUtilities : MonoBehaviour
    {
        public static CanvasUtilities Instance;

        [SerializeField] private CanvasGroup loader;
        [SerializeField] private float fadeTime;
        [SerializeField] private TMP_Text loaderText, errorText;

        private async void Awake()
        {
            Instance = this;
            
            DontDestroyOnLoad(gameObject);
            await Toggle(false, instant: true);
        }

        public async Task Toggle(bool on, string text = null, bool instant = false)
        {
            loaderText.text = text;
            loader.gameObject.SetActive(on);

            await loader.DOFade(on ? 1 : 0, instant ? 0 : fadeTime).AsyncWaitForCompletion();
        }

        public void ShowError(string error)
        {
            errorText.text = error;
            errorText.DOFade(1, fadeTime).OnComplete(() =>
            {
                errorText.DOFade(0, fadeTime).SetDelay(1);
            });
        }
    }

    public class SceneLoader : IDisposable
    {
        public async Task ShowLoader(string loaderText)
        {
            await CanvasUtilities.Instance.Toggle(true, loaderText);
        }
        
        public async Task HideLoader(string loaderText)
        {
            await CanvasUtilities.Instance.Toggle(false, loaderText);
        }

        public async void Dispose()
        {
            await CanvasUtilities.Instance.Toggle(false);
        }
    }
}