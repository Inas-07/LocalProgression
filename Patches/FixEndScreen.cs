using HarmonyLib;

namespace LocalProgression.Patches
{
    [HarmonyPatch]
    internal class FixEndScreen
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.UpdateDiscordDetails))]
        private static bool Pre_UpdateDiscordDetails()
        {
            return false;
        }
    }
}
