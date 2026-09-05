// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.RoomModeration
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using HarmonyLib;
using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.UI;
using MelonLoader;
using MelonLoader.Utils;
using System.Collections;
using System.IO;
using UnityEngine.Networking;

#nullable enable
namespace BigscreenBehind;

internal class RoomModeration
{
  public static bool isMod;

  private static bool IsLocalUserAdmin()
  {
    return BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.IsLocalUserAdmin;
  }

  [HarmonyPatch(typeof (UserProfile), "ToggleAdminElements")]
  private static class UserProfile_Show
  {
    private static void Postfix(UserProfile __instance, bool __0)
    {
      if (!File.Exists(Path.Combine(MelonEnvironment.PluginsDirectory, "Debug.txt")) && !RoomModeration.isMod || __0)
        return;
      __instance.ToggleAdminElements(true);
    }
  }

  [HarmonyPatch(typeof (UserProfile), "DoKick")]
  private static class UserProfile_DoKick
  {
    private static void Postfix(UserProfile __instance)
    {
      if (string.IsNullOrEmpty(__instance.RemoteUserId) || RoomModeration.IsLocalUserAdmin())
        return;
      MelonCoroutines.Start(RoomModeration.UserProfile_DoKick.DoKick(__instance.RemoteUserId, BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.RoomId, ((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel.CurrentUser).Profile).SocialId));
    }

    private static IEnumerator DoKick(string sessionId, string currentRoom, string localID)
    {
      if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(currentRoom) || string.IsNullOrEmpty(localID))
      {
        MelonLogger.Error("KickUser: Invalid parameters provided.");
      }
      else
      {
        string url = $"{"https://chat.bigscreenfriends.com/messages"}/kickuser?room={currentRoom}&moderator={localID}&target={sessionId}";
        UnityWebRequest request = UnityWebRequest.Post(url, "");
        request.SetRequestHeader("Authorization", "Bearer " + Accounts.GetAccessToken());
        yield return (object) request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
          MelonLogger.Error($"KickUser request failed: {request.responseCode} {request.error}");
        else
          MelonLogger.Msg("KickUser response: " + request.downloadHandler.text);
      }
    }
  }
}
