// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.Patches.Teleport
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using HarmonyLib;
using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.UI;
using Il2CppBigscreen.Users;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

#nullable enable
namespace BigscreenBehind.Patches;

internal class Teleport
{
  private bool isDebug;

  private static IEnumerator TeleportToUserCoroutine()
  {
    bool debugexist = File.Exists(Path.Combine(MelonEnvironment.PluginsDirectory, "Debug.txt"));
    bool isDebug = (UnityEngine.Object) GameObject.Find("DebugUser(Clone)") != (UnityEngine.Object) null;
    Transform head = isDebug ? BIG_STATIC_SINGLETONS.bigMainCameraLOL?.transform?.parent?.parent : BIG_STATIC_SINGLETONS.bigMainCameraLOL?.transform?.parent?.parent?.parent;
    UserProfile userProfilePopup = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Popups/UserProfile(Clone)")?.GetComponent<UserProfile>();
    RemoteUserController user = (RemoteUserController) null;
    if (((RoomUser) userProfilePopup?.RemoteUser)?.LegacyUserId != null)
      user = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(((RoomUser) userProfilePopup.RemoteUser).LegacyUserId);
    if ((UnityEngine.Object) user?.remoteUserSync?.RemoteAvatarGO != (UnityEngine.Object) null)
    {
      if (!debugexist)
      {
        string socialId = ((SocialProfile) ((RoomUser) user.RemoteUser).SocialProfile).SocialId;
        bool? isOnline = new bool?();
        yield return MelonCoroutines.Start(Teleport.CheckUserOnlineCoroutine(socialId, (System.Action<bool>) (result => isOnline = new bool?(result))));
        if (((RoomUser) user.RemoteUser).SocialProfile.SocialGraphType != 2)
        {
          BigscreenBehind.Utils.FloatingNotification("Not a friend", duration: 1f, icon: "f259");
          yield break;
        }
        if (!isOnline.GetValueOrDefault())
        {
          BigscreenBehind.Utils.FloatingNotification("Not a Behind user", duration: 1f, icon: "f259");
          yield break;
        }
        MelonCoroutines.Start(Teleport.MuteFor3Seconds());
        socialId = (string) null;
      }
      ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GetPage((BigUIState) 23)).gameObject.GetComponent<ControlStrip>().RecenterHMD();
      GameObject avatarGO = user.remoteUserSync.RemoteAvatarGO;
      Vector3 spawnPosition = avatarGO.transform.position + avatarGO.transform.forward * 2.5f;
      MelonCoroutines.Start(Teleport.MoveHeadOverTime(head, spawnPosition, avatarGO.transform.position, 1f));
      avatarGO = (GameObject) null;
    }
    else
      MelonLogger.Error("User or LegacyUserId is null.");
  }

  private static IEnumerator MoveHeadOverTime(
    Transform head,
    Vector3 targetPosition,
    Vector3 lookAtPosition,
    float duration)
  {
    Vector3 startPosition = head.position;
    float elapsed = 0.0f;
    while ((double) elapsed < (double) duration)
    {
      float t = elapsed / duration;
      head.position = Vector3.Lerp(startPosition, targetPosition, t);
      Vector3 directionToAvatar = new Vector3(lookAtPosition.x - head.position.x, 0.0f, lookAtPosition.z - head.position.z).normalized;
      if (directionToAvatar != Vector3.zero)
      {
        Quaternion lookRotation = Quaternion.LookRotation(directionToAvatar);
        head.rotation = Quaternion.Slerp(head.rotation, lookRotation, Time.deltaTime * 10f);
      }
      elapsed += Time.deltaTime;
      yield return (object) null;
    }
    head.position = targetPosition;
    Vector3 finalDirection = new Vector3(lookAtPosition.x - targetPosition.x, 0.0f, lookAtPosition.z - targetPosition.z).normalized;
    if (finalDirection != Vector3.zero)
      head.rotation = Quaternion.LookRotation(finalDirection);
  }

  private static IEnumerator MuteFor3Seconds()
  {
    bool currentMuteState = BIG_STATIC_SINGLETONS.blockingController.AllMuted;
    MainCameraControlMod.muteInProgress = true;
    BIG_STATIC_SINGLETONS.blockingController?.ToggleMuteAll(true);
    yield return (object) new WaitForSeconds(3f);
    BIG_STATIC_SINGLETONS.blockingController?.ToggleMuteAll(currentMuteState);
    MainCameraControlMod.muteInProgress = false;
  }

  private static void TeleportToUser(object obj)
  {
    MelonCoroutines.Start(Teleport.TeleportToUserCoroutine());
  }

  private static IEnumerator CheckUserOnlineCoroutine(string socialId, System.Action<bool> callback)
  {
    string url = "https://modcheck.phaze.org:7983/checkonline.php?socialid=" + socialId;
    UnityWebRequest www = UnityWebRequest.Get(url);
    yield return (object) www.SendWebRequest();
    if (www.result == UnityWebRequest.Result.Success)
    {
      string data = www.downloadHandler.text;
      callback(data == "[\"online\"]");
      data = (string) null;
    }
    else
      callback(false);
    www.Dispose();
  }

  [HarmonyPatch(typeof (UserProfile), "Show")]
  private static class UserProfile_Show
  {
    private static void Postfix(UserProfile __instance)
    {
      GameObject gameObject1 = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Bottom/ControlStrip(Clone)/Content/ToolbarRecenter/Btn (6)");
      Transform transform = ((Component) __instance).transform.Find("Content/Body");
      if ((bool) (UnityEngine.Object) transform.transform.Find("Btn_Cloned"))
        return;
      GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject1.gameObject, transform.transform);
      gameObject2.name = "Btn_Cloned";
      gameObject2.transform.localPosition = new Vector3(-0.12f, 0.117f, 0.0f);
      gameObject2.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
      gameObject2.transform.localScale = new Vector3(0.7f, 0.7f, 0.01f);
      gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetColors(new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f), new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f));
      Transform child = gameObject2.transform.GetChild(0);
      for (int index = 1; index < child.childCount; ++index)
        child.GetChild(index).gameObject.SetActive(false);
      child.transform.GetChild(0).GetComponent<TextMeshPro>().text = "\uF259";
      gameObject2.transform.GetChild(1).localPosition += new Vector3(0.0f, 0.03f, 0.0f);
      gameObject2.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = nameof (Teleport);
      gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetupMeshes();
      BigscreenBehind.Utils.ReplaceButtonEvent(gameObject2.gameObject.GetComponent<BigUIButton>().OnPoked, new System.Action<Il2CppSystem.Object>(Teleport.TeleportToUser));
    }
  }
}
