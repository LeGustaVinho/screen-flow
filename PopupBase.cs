using System;

namespace LegendaryTools.Systems.ScreenFlow
{
    public abstract class PopupBase : ScreenBase, IPopupBase
    {
        private IScreenBase parentScreen;
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.HideInEditorMode]
#endif
        IScreenBase IPopupBase.ParentScreen
        {
            get => parentScreen;
            set => parentScreen = value;
        }

        public event Action<IPopupBase> OnClosePopupRequest;
        public event Action<IPopupBase> OnGoneToBackground;
        
        void IPopupBase.GoToBackground(System.Object args)
        {
            OnGoToBackground(args);
            OnGoneToBackground?.Invoke(this);
        }

        public abstract void OnGoToBackground(System.Object args);

        public virtual void ClosePopup()
        {
            OnClosePopupRequest?.Invoke(this);
        }
    }
}