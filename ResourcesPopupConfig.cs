using System;
using System.Collections;
using LegendaryTools.Systems.AssetProvider;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LegendaryTools.Systems.ScreenFlow
{
    [Serializable]
    public class PopupBaseStringAssetLoadable : AssetLoadable<PopupBase, string>
    {
    }
    
    [CreateAssetMenu(menuName = "Tools/ScreenFlow/PopupConfig/ResourcesPopupConfig")]
    public class ResourcesPopupConfig : PopupConfig
    {
        [Header("Loader")]
        public PopupBaseStringAssetLoadable AssetLoadable;
        public override IAssetLoaderConfig AssetLoaderConfig => AssetLoadable;
    }
}