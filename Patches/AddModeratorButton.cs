// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.Patches.AddModeratorButton
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using HarmonyLib;
using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.UI;
using Il2CppBigscreen.Users;
using Il2CppSimpleJSONBigscreen;
using Il2CppTMPro;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

#nullable enable
namespace BigscreenBehind.Patches;

internal class AddModeratorButton
{
  public static bool init = true;
  private static bool IsMod = false;
  private static Transform firstChild;

  private static bool IsUserMod(string socialId)
  {
    string roomId = BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.RoomId;
    if (string.IsNullOrEmpty(roomId))
      return false;
    UnityWebRequest unityWebRequest = UnityWebRequest.Get($"{"https://chat.bigscreenfriends.com/messages"}/ismod?room={roomId}&socialid={socialId}");
    unityWebRequest.SendWebRequest();
    do
      ;
    while (!unityWebRequest.isDone);
    if (unityWebRequest.result != UnityWebRequest.Result.Success)
    {
      MelonLogger.Error("Failed to check mod status: " + unityWebRequest.error);
      return false;
    }
    try
    {
      return JSON.Parse(unityWebRequest.downloadHandler.text)["isMod"].AsBool;
    }
    catch
    {
      MelonLogger.Error("Failed to parse ismod response");
      return false;
    }
  }

  [HarmonyPatch(typeof (UserProfile), "Show")]
  private static class UserProfile_OnShowComplete
  {
    private static GameObject clonedButton;

    private static void Postfix(UserProfile __instance)
    {
      if (!AddModeratorButton.init)
        return;
      if (!BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.IsLocalUserAdmin && (UnityEngine.Object) AddModeratorButton.UserProfile_OnShowComplete.clonedButton != (UnityEngine.Object) null)
      {
        AddModeratorButton.UserProfile_OnShowComplete.clonedButton.SetActive(false);
      }
      else
      {
        GameObject gameObject = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Bottom/ControlStrip(Clone)/Content/ToolbarRecenter/Btn (6)");
        Transform transform = ((Component) __instance).transform.Find("Content/Body");
        if ((UnityEngine.Object) AddModeratorButton.UserProfile_OnShowComplete.clonedButton != (UnityEngine.Object) null)
          MelonCoroutines.Start(AddModeratorButton.UserProfile_OnShowComplete.SetModStatus(__instance, AddModeratorButton.UserProfile_OnShowComplete.clonedButton.transform));
        if (!((UnityEngine.Object) transform.transform.Find("Btn_AddMod") == (UnityEngine.Object) null))
          return;
        AddModeratorButton.UserProfile_OnShowComplete.clonedButton = UnityEngine.Object.Instantiate<GameObject>(gameObject.gameObject, transform.transform);
        AddModeratorButton.UserProfile_OnShowComplete.clonedButton.name = "Btn_AddMod";
        AddModeratorButton.UserProfile_OnShowComplete.clonedButton.transform.localPosition = new Vector3(0.13f, 0.207f, 0.0f);
        AddModeratorButton.UserProfile_OnShowComplete.clonedButton.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        AddModeratorButton.UserProfile_OnShowComplete.clonedButton.transform.localScale = new Vector3(0.7f, 0.7f, 0.01f);
        AddModeratorButton.UserProfile_OnShowComplete.clonedButton.gameObject.GetComponent<BigUIButton>().Visualization.SetColors(new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f), new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f));
        AddModeratorButton.firstChild = AddModeratorButton.UserProfile_OnShowComplete.clonedButton.transform.GetChild(0);
        for (int index = 1; index < AddModeratorButton.firstChild.childCount; ++index)
          AddModeratorButton.firstChild.GetChild(index).gameObject.SetActive(false);
        AddModeratorButton.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().text = "\uF521";
        AddModeratorButton.UserProfile_OnShowComplete.clonedButton.transform.GetChild(1).localPosition += new Vector3(0.0f, 0.03f, 0.0f);
        AddModeratorButton.UserProfile_OnShowComplete.clonedButton.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Add Mod";
        AddModeratorButton.UserProfile_OnShowComplete.clonedButton.gameObject.GetComponent<BigUIButton>().Visualization.SetupMeshes();
        Utils.ReplaceButtonEvent(AddModeratorButton.UserProfile_OnShowComplete.clonedButton.gameObject.GetComponent<BigUIButton>().OnPoked, new System.Action<Il2CppSystem.Object>(AddModeratorButton.UserProfile_OnShowComplete.ModUserAction));
        AddModeratorButton.UserProfile_OnShowComplete.clonedButton.gameObject.SetActive(false);
      }
    }

    private static IEnumerator SetModStatus(UserProfile __instance, Transform button)
    {
      button.gameObject.SetActive(false);
      AddModeratorButton.IsMod = false;
      button.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Add Mod";
      AddModeratorButton.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = Color.white;
      if (((RoomUser) __instance?.RemoteUser)?.LegacyUserId != null)
      {
        RemoteUserController user = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(((RoomUser) __instance.RemoteUser).LegacyUserId);
        if (!((UnityEngine.Object) user == (UnityEngine.Object) null) && !((UnityEngine.Object) user.remoteUserSync == (UnityEngine.Object) null))
        {
          GameObject avatarGO = user.remoteUserSync.RemoteAvatarGO;
          if (!((UnityEngine.Object) avatarGO == (UnityEngine.Object) null))
          {
            string currentRoom = BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.RoomId;
            if (!string.IsNullOrEmpty(currentRoom))
            {
              string url = $"{"https://chat.bigscreenfriends.com/messages"}/ismod?room={currentRoom}&socialid={((SocialProfile) ((RoomUser) user.RemoteUser).SocialProfile).SocialId}";
              UnityWebRequest www = UnityWebRequest.Get(url);
              www.SendWebRequest();
              while (!www.isDone)
                yield return (object) null;
              if (www.result != UnityWebRequest.Result.Success)
              {
                MelonLogger.Error("Failed to check mod status: " + www.error);
              }
              else
              {
                try
                {
                  JSONNode response = JSON.Parse(www.downloadHandler.text);
                  AddModeratorButton.IsMod = response["isMod"].AsBool;
                  response = (JSONNode) null;
                }
                catch
                {
                  MelonLogger.Error("Failed to parse ismod response");
                  yield break;
                }
                if (AddModeratorButton.IsMod)
                {
                  button.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Remove Mod";
                  AddModeratorButton.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = new Color(1f, 0.843f, 0.0f, 1f);
                }
                else
                {
                  button.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Add Mod";
                  AddModeratorButton.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = Color.white;
                }
                button.gameObject.SetActive(true);
              }
            }
          }
        }
      }
    }

    private static void ModUserAction(Il2CppSystem.Object @object)
    {
      if (!BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.IsLocalUserAdmin)
        return;
      UserProfile component = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Popups/UserProfile(Clone)").GetComponent<UserProfile>();
      if (((RoomUser) component?.RemoteUser)?.LegacyUserId == null)
        return;
      RemoteUserController withLegacyUserId = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(((RoomUser) component.RemoteUser).LegacyUserId);
      if ((UnityEngine.Object) withLegacyUserId == (UnityEngine.Object) null || (UnityEngine.Object) withLegacyUserId.remoteUserSync == (UnityEngine.Object) null || (UnityEngine.Object) withLegacyUserId.remoteUserSync.RemoteAvatarGO == (UnityEngine.Object) null)
        return;
      string username = ((SocialProfile) ((RoomUser) withLegacyUserId.RemoteUser).SocialProfile).Username;
      string socialId = ((SocialProfile) ((RoomUser) withLegacyUserId.RemoteUser).SocialProfile).SocialId;
      if (!AddModeratorButton.IsMod)
      {
        MelonCoroutines.Start(AddModeratorButton.UserProfile_OnShowComplete.ToggleMod(username, socialId, true, component));
        ((Component) component).transform.Find("Content/Body/Btn_AddMod").transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Remove Mod";
        AddModeratorButton.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = new Color(1f, 0.843f, 0.0f, 1f);
      }
      else
      {
        MelonCoroutines.Start(AddModeratorButton.UserProfile_OnShowComplete.ToggleMod(username, socialId, false, component));
        ((Component) component).transform.Find("Content/Body/Btn_AddMod").transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Add Mod";
        AddModeratorButton.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = Color.white;
      }
    }

    private static IEnumerator ToggleMod(
      string username,
      string socialId,
      bool makeMod,
      UserProfile __instance)
    {
      if (BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.IsLocalUserAdmin)
      {
        string currentRoom = BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.RoomId;
        if (!string.IsNullOrEmpty(socialId) && !string.IsNullOrEmpty(currentRoom))
        {
          string endpoint = makeMod ? "setmod" : "removemod";
          string url = $"{"https://chat.bigscreenfriends.com/messages"}/{endpoint}?room={currentRoom}&socialid={socialId}";
          UnityWebRequest www = UnityWebRequest.Post(url, "");
          www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
          www.SetRequestHeader("Authorization", "Bearer " + Accounts.GetAccessToken());
          yield return (object) www.SendWebRequest();
          if (www.result != UnityWebRequest.Result.Success)
            Utils.FloatingNotification($"Failed to {(makeMod ? "promote" : "demote")} mod: {www.error}", duration: 4f, icon: "f521");
          else
            Utils.FloatingNotification($"{(makeMod ? "Promoted" : "Demoted")} mod: {username}", duration: 3f, icon: "f521");
          MelonCoroutines.Start(AddModeratorButton.UserProfile_OnShowComplete.SetModStatus(__instance, AddModeratorButton.UserProfile_OnShowComplete.clonedButton.transform));
        }
      }
    }
  }
}
