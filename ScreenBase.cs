using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace LegendaryTools.Systems.ScreenFlow
{
    public enum BackKeyBehaviourOverride
    {
        /// <summary>
        /// Setting are inherit from ScreenConfig
        /// </summary>
        Inherit,

        /// <summary>
        /// Back key not allowed, does nothing
        /// </summary>
        NotAllowed,

        /// <summary>
        /// Back key causes a Move Back on flow
        /// </summary>
        ScreenMoveBack,

        /// <summary>
        /// Back key closes the first popup in the stack 
        /// </summary>
        CloseFirstPopup,
    }

    public abstract class ScreenBase : MonoBehaviour
    {
        public BackKeyBehaviourOverride BackKeyBehaviourOverride = BackKeyBehaviourOverride.Inherit;

        public abstract Task Show(System.Object args);

        public abstract Task Hide(System.Object args);
    }
}