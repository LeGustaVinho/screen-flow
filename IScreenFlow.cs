using System;
using System.Collections.Generic;

namespace LegendaryTools.Systems.ScreenFlow
{
    public interface IScreenFlow
    {
        bool IsTransiting { get; }
        bool IsPreloading { get; }
        ScreenConfig CurrentScreenConfig { get; }
        ScreenBase CurrentScreenInstance { get; }
        PopupConfig CurrentPopupConfig { get; }
        PopupBase CurrentPopupInstance { get; }
        List<PopupBase> CurrentPopupInstancesStack { get; }
        
        int PopupStackCount { get; }

        void SendTrigger(string name, System.Object args = null, bool enqueue = false, 
            Action<ScreenBase> requestedScreenOnShow = null, Action<ScreenBase> previousScreenOnHide = null);

        void SendTrigger(UIEntityBaseConfig uiEntity, System.Object args = null, bool enqueue = false, 
            Action<ScreenBase> requestedScreenOnShow = null, Action<ScreenBase> previousScreenOnHide = null);

        void MoveBack(System.Object args = null, bool enqueue = false, Action<ScreenBase> onShow = null, Action<ScreenBase> onHide = null);
        void CloseForegroundPopup(System.Object args = null, bool enqueue = false, Action<ScreenBase> onShow = null, Action<ScreenBase> onHide = null);
        void ClosePopup(PopupBase popupBase, System.Object args = null, bool enqueue = false, Action<ScreenBase> onShow = null, Action<ScreenBase> onHide = null);
    }
}