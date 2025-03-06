using HarmonyLib;
using TnTRFMod.Utils;
#if BEPINEX
using Steamworks;
#endif

#if MELONLOADER
using Il2CppSteamworks;
#endif

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class ReopenInviteDialogPatch
{
    public static CSteamID? PrevId;

    [HarmonyPatch(typeof(SteamFriends))]
    [HarmonyPatch(nameof(SteamFriends.ActivateGameOverlayInviteDialog))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void SteamFriends_ActivateGameOverlayInviteDialog_Prefix(CSteamID steamIDLobby)
    {
        Logger.Info($"Current Steam Lobby ID: {steamIDLobby}");
        PrevId = steamIDLobby;
    }
}