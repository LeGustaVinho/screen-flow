using System;
using System.Collections;
using LegendaryTools.Systems.AssetProvider;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LegendaryTools.Systems.ScreenFlow
{
    [Serializable]
    public class ScreenBaseStringAssetLoadable : AssetLoadable<ScreenBase, string>
    {
    }

    [CreateAssetMenu(menuName = "Tools/ScreenFlow/ScreenConfig/ResourcesScreenConfig")]
    public class ResourcesScreenConfig : ScreenConfig
    {
        [Header("Loader")]
        public ScreenBaseStringAssetLoadable AssetLoadable;
        public override IAssetLoaderConfig AssetLoaderConfig => AssetLoadable;
    }
}