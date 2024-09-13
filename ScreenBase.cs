using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace LegendaryTools.Systems.ScreenFlow
{
    public abstract class ScreenBase : MonoBehaviour
    {
        public BackKeyBehaviourOverride BackKeyBehaviourOverride = BackKeyBehaviourOverride.Inherit;

        public event Action<ScreenBase> OnHideRequest;
        public event Action<ScreenBase> OnHideCompleted;
        
        public abstract IEnumerator Show(System.Object args);

        public IEnumerator RequestHide(System.Object args)
        {
            OnHideRequest?.Invoke(this);
            yield return Hide(Hide(args));
            OnHideCompleted?.Invoke(this);
        }
        
        public abstract IEnumerator Hide(System.Object args);
    }
}