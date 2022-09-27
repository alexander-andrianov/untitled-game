using System.Threading.Tasks;
using Content.Scripts.GameCore.Base.Interfaces;
using DG.Tweening;
using UniRx;
using UnityEngine;

namespace Content.Scripts.GameCore.Base
{
    public abstract class LayoutBase : MonoBehaviour
    {
        private const float HideLayoutDuration = 0.2f;
        private const float ShowLayoutDuration = 0.3f;
        
        private readonly Vector3 minScale = new Vector3(0.01f, 0.01f, 0.01f);
        private readonly Vector3 defaultScale = Vector3.one;
        
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private void OnDisable()
        {
            disposables.Clear();
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        private void ChangeScale(Transform targetTransform, Vector3 value)
        {
            targetTransform.localScale = value;
        }

        private void SetLayoutVisible(bool value)
        {
            gameObject.SetActive(value);
        }
        
        internal async Task HideLayout(Transform childTransform)
        {
            await childTransform.DOScale(minScale, HideLayoutDuration).SetEase(Ease.InExpo).OnComplete(() =>
            {
                SetLayoutVisible(false);
                ChangeScale(childTransform, defaultScale);
            }).AsyncWaitForCompletion();
        }

        internal async Task ShowLayout(Transform targetTransform)
        {
            ChangeScale(targetTransform, minScale);
            SetLayoutVisible(true);
            
            await targetTransform.DOScale(defaultScale, ShowLayoutDuration).SetEase(Ease.OutExpo).AsyncWaitForCompletion();
        }
        
        internal abstract void Initialize();
    }
}
