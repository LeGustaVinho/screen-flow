using System.Collections;
using LegendaryTools.Systems.AssetProvider;
using UnityEngine;

namespace LegendaryTools.Systems.ScreenFlow
{
    public abstract class UIEntityBaseConfig : ScriptableObject
    {
        public AnimationType AnimationType = AnimationType.NoAnimation;

        public abstract IAssetLoaderConfig AssetLoaderConfig { get; }
    }
}