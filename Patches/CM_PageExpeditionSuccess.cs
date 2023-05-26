//using CellMenu;
//using HarmonyLib;
//using LocalProgression.Component;

//namespace LocalProgression.Patches
//{
//    [HarmonyPatch]
//    internal class Patches_CM_PageExpeditionSuccess
//    {
//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(CM_PageExpeditionSuccess), nameof(CM_PageExpeditionSuccess.Setup))]
//        private static void Post_CM_PageExpeditionSuccess_Setup(CM_PageExpeditionSuccess __instance)
//        {
//            __instance.gameObject.AddComponent<ExpeditionSuccess_NoBooster>().Setup();
//        }
//    }
//}
