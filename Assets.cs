using GTFO.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LocalProgression
{
    internal static class Assets
    {
        internal static GameObject NoBoosterIcon { get; private set; }

        internal static void Init()
        {
            NoBoosterIcon = AssetAPI.GetLoadedAsset<GameObject>("Assets/Misc/CM_ExpeditionSectorIcon.prefab");

        }
    }
}
