using System;
using System.Collections;
using UnityEngine;

namespace LegendaryTools.Systems.ScreenFlow
{
    public abstract class ScreenBaseT<T, TDataShow, TDataHide> : ScreenBase
        where T : class
    {
        public event Action<T> OnHideRequestT;
        public event Action<T> OnHideCompletedT;
        public event Action<T> OnDestroyedT;
        
        public override IEnumerator Show(object args)
        {
            if (args is TDataShow typedData)
            {
                yield return Show(typedData);
            }
            else
            {
                Debug.LogError($"[ScreenBaseT:Show] TypeMissMatch: Args is type {args.GetType()}, but was expected {typeof(TDataShow)}, Show() will not be called");
            }
        }
        
        public override IEnumerator Hide(object args)
        {
            if (args is TDataHide typedData)
            {
                yield return Hide(typedData);
            }
            else
            {
                Debug.LogError($"[ScreenBaseT:Hide] TypeMissMatch: Args is type {args.GetType()}, but was expected {typeof(TDataHide)}, Hide() will not be called");
            }
        }

        public override IEnumerator RequestHide(object args)
        {
            RaiseOnHideRequest(this);
            OnHideRequestT?.Invoke(this as T);
            yield return Hide(args);
            RaiseOnHideCompleted(this);
            OnHideCompletedT?.Invoke(this as T);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnDestroyedT?.Invoke(this as T);
        }

        public abstract IEnumerator Show(TDataShow args);
        public abstract IEnumerator Hide(TDataHide args);
    }
}