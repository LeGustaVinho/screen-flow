using UnityEngine;

namespace LegendaryTools.Systems.ScreenFlow
{
    public abstract class ScreenConfig : UIEntityBaseConfig
    {
        [Header("Popups")] public bool AllowPopups;
        public bool AllowStackablePopups;

        [Header("Behaviour")] public bool CanMoveBackFromHere;
        public bool CanMoveBackToHere;
        public BackKeyBehaviour BackKeyBehaviour = BackKeyBehaviour.ScreenMoveBack;
        public PopupsBehaviourOnScreenTransition PopupBehaviourOnScreenTransition = PopupsBehaviourOnScreenTransition.HideFirstThenTransit;
    }
}