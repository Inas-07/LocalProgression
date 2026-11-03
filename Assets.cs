using GTFO.API;
using UnityEngine;

namespace LocalProgression
{
    internal static class Assets
    {
        internal static GameObject NoBoosterIcon { get; private set; } = null!;

        internal static void Init()
        {
            NoBoosterIcon = AssetAPI.GetLoadedAsset<GameObject>("Assets/Misc/CM_ExpeditionSectorIcon.prefab");
        }
    }
}
