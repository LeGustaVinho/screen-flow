using System;
using System.Collections.Generic;

namespace LegendaryTools.Systems.ScreenFlow
{
    public interface IScreenFlow
    {
        bool IsTransiting { get; }
        bool IsPreloading { get; }
        ScreenConfig CurrentScreenConfig { get; }
        IScreenBase CurrentScreenInstance { get; }
        PopupConfig CurrentPopupConfig { get; }
        IPopupBase CurrentPopupInstance { get; }
        List<IPopupBase> CurrentPopupInstancesStack { get; }
        
        event Action<ScreenConfig, ScreenConfig> OnScreenChange;
        event Action<PopupConfig, PopupConfig> OnPopupOpen;
        
        int PopupStackCount { get; }

        void SendTrigger(string name, System.Object args = null, bool enqueue = false, 
            Action<IScreenBase> requestedScreenOnShow = null, Action<IScreenBase> previousScreenOnHide = null);

        void SendTrigger(UIEntityBaseConfig uiEntity, System.Object args = null, bool enqueue = false, 
            Action<IScreenBase> requestedScreenOnShow = null, Action<IScreenBase> previousScreenOnHide = null);

        void MoveBack(System.Object args = null, bool enqueue = false, Action<IScreenBase> onShow = null, Action<IScreenBase> onHide = null);
        void CloseForegroundPopup(System.Object args = null, bool enqueue = false, Action<IScreenBase> onShow = null, Action<IScreenBase> onHide = null);
        void ClosePopup(IPopupBase popupBase, System.Object args = null, bool enqueue = false, Action<IScreenBase> onShow = null, Action<IScreenBase> onHide = null);
    }
}