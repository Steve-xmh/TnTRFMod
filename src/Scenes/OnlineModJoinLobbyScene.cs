using TnTRFMod.Ui.Widgets;
using UnityEngine;
#if BEPINEX
#endif

#if MELONLOADER
using Il2CppSteamworks;
#endif

namespace TnTRFMod.Scenes;

public class OnlineModJoinLobbyScene : IScene
{
    public string SceneName => "OnlineRoomJoinLobby";

    public void Start()
    {
        var reopenInviteBtn = new ButtonUi
        {
            Text = "重新打开邀请页面",
            Position = new Vector2(64f, 128f),
            Size = new Vector2(200f, 64f)
        };
        // reopenInviteBtn.AddListener(() =>
        // {
        //     var id = ReopenInviteDialogPatch.PrevId;
        //     if (id.HasValue) SteamFriends.ActivateGameOverlayInviteDialog(id.Value);
        // });
    }

    public void Destroy()
    {
        // ReopenInviteDialogPatch.PrevId = null;
    }
}