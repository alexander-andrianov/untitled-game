using System;
using UniRx;
using UnityEngine.InputSystem;

namespace Assets.Content.Scripts.Extensions.Actions
{
    public static class InputActionExtensions
    {
        public static IObservable<InputAction.CallbackContext> ToObservable(this InputAction action) =>
            Observable.FromEvent<InputAction.CallbackContext>(
                h => action.performed += h,
                h => action.performed -= h
        );
    }
}
