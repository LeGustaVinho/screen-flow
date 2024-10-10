using UnityEngine;
using UnityEngine.UI;

namespace LegendaryTools.Systems.ScreenFlow
{
    public class UIScreenFlowTrigger : MonoBehaviour
    {
        public ScreenFlowTriggerMode Mode = ScreenFlowTriggerMode.Trigger;
        public UIEntityBaseConfig UiEntity;
        public bool Enqueue;
        
        private Button button;

        public virtual void ProcessTrigger()
        {
#if SCREEN_FLOW_SINGLETON
            if (ScreenFlow.Instance != null)
            {
                switch (Mode)
                {
                    case ScreenFlowTriggerMode.Trigger:
                    {
                        ScreenFlow.Instance.SendTrigger(UiEntity, enqueue:Enqueue);
                        break;
                    }
                    case ScreenFlowTriggerMode.MoveBack:
                    {
                        ScreenFlow.Instance.MoveBack(enqueue:Enqueue);
                        break;
                    }
                    case ScreenFlowTriggerMode.ClosePopup:
                    {
                        ScreenFlow.Instance.CloseForegroundPopup(enqueue:Enqueue);
                        break;
                    }
                }
            }
#else
            Debug.LogWarning($"[{nameof(UIScreenFlowTrigger)}:{nameof(ProcessTrigger)}] Cannot be executed because ScreenFlow is not singleton, define SCREEN_FLOW_SINGLETON or override this function.");
#endif
        }

        private void Start()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(ProcessTrigger);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(ProcessTrigger);
            }
        }
    }
}
