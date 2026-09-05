// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.OutlineTracker
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using HarmonyLib;
using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.UI;
using Il2CppBigscreen.Users;
using Il2CppBigscreen.Utils;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTMPro;
using MelonLoader;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("OutlineTracker", "0.5.0", "Love")]
internal class OutlineTracker : MelonMod
{
  public static List<string> excludedSocialIDs = new List<string>();
  private static bool init = false;
  private static Transform firstChild;

  public override void OnLateInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} Mod loaded!\nMade with LOVE\n=========================\n");
    MelonCoroutines.Start(this.GetExcludedUsersFromCloud());
    OutlineTracker.init = true;
  }

  private IEnumerator GetExcludedUsersFromCloud()
  {
    string excludedidsURL = "https://bigscreen.phaze.org/exclude.json";
    UnityWebRequest wwwx = UnityWebRequest.Get(excludedidsURL);
    wwwx.SendWebRequest();
    while (!wwwx.isDone)
      yield return (object) null;
    if (wwwx.result == UnityWebRequest.Result.Success)
    {
      string jsonData = wwwx.downloadHandler.text;
      string[] remoteIDS = OutlineTracker.ConvertJsonStringToArray(jsonData);
      OutlineTracker.excludedSocialIDs.AddRange((IEnumerable<string>) remoteIDS);
      jsonData = (string) null;
      remoteIDS = (string[]) null;
    }
    else
      this.LoggerInstance.Msg("Failed to get excluded ids: " + wwwx.error);
    wwwx.Dispose();
    yield return (object) null;
  }

  public static string[] ConvertJsonStringToArray(string json)
  {
    string[] array = json.Replace("[", "").Replace("]", "").Replace("\"", "").Split(',');
    for (int index = 0; index < array.Length; ++index)
      array[index] = array[index].Replace("ㅤ", "").Trim();
    return array;
  }

  private static void OutlineObject(GameObject gameObject)
  {
    Transform transform = gameObject.transform.Find(nameof (OutlineObject));
    if ((UnityEngine.Object) transform != (UnityEngine.Object) null)
      transform.gameObject.SetActive(true);
    if ((UnityEngine.Object) gameObject.GetComponent<Outline>() != (UnityEngine.Object) null)
    {
      gameObject.GetComponent<Outline>().OnEnable();
    }
    else
    {
      Outline outline = gameObject.AddComponent<Outline>();
      outline.renderers = (Il2CppReferenceArray<Renderer>) gameObject.transform.GetComponentsInChildren<Renderer>();
      outline.outlineColor = new Color(0.98f, 0.42f, 0.65f);
      outline.OutlineMode = (Outline.Mode) 3;
      outline.UpdateMaterialProperties();
      outline.OnEnable();
    }
  }

  private static void DisableOutline(GameObject gameObject)
  {
    if ((UnityEngine.Object) gameObject.GetComponent<Outline>() != (UnityEngine.Object) null)
      gameObject.GetComponent<Outline>().OnDisable();
    Transform transform = gameObject.transform.Find("OutlineObject");
    if (!((UnityEngine.Object) transform != (UnityEngine.Object) null))
      return;
    transform.gameObject.SetActive(false);
  }

  [HarmonyPatch(typeof (UserProfile), "Show")]
  private static class UserProfile_OnShowComplete
  {
    private static void Postfix(UserProfile __instance)
    {
      if (!OutlineTracker.init)
        return;
      GameObject gameObject1 = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Bottom/ControlStrip(Clone)/Content/ToolbarRecenter/Btn (6)");
      Transform transform = ((Component) __instance).transform.Find("Content/Body");
      if ((UnityEngine.Object) transform.transform.Find("Btn_Highlight") != (UnityEngine.Object) null)
      {
        Transform button = transform.transform.Find("Btn_Highlight");
        OutlineTracker.UserProfile_OnShowComplete.SetHiglightStatus(__instance, button);
      }
      if ((UnityEngine.Object) transform.transform.Find("Btn_Highlight") == (UnityEngine.Object) null)
      {
        GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject1.gameObject, transform.transform);
        gameObject2.name = "Btn_Highlight";
        gameObject2.transform.localPosition = new Vector3(-0.155f, 0.117f, 0.0f);
        gameObject2.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        gameObject2.transform.localScale = new Vector3(0.7f, 0.7f, 0.01f);
        gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetColors(new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f), new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f));
        OutlineTracker.firstChild = gameObject2.transform.GetChild(0);
        for (int index = 1; index < OutlineTracker.firstChild.childCount; ++index)
          OutlineTracker.firstChild.GetChild(index).gameObject.SetActive(false);
        OutlineTracker.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().text = "\uF06E";
        gameObject2.transform.GetChild(1).localPosition += new Vector3(0.0f, 0.03f, 0.0f);
        gameObject2.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Highlight";
        gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetupMeshes();
        BigscreenBehind.Utils.ReplaceButtonEvent(gameObject2.gameObject.GetComponent<BigUIButton>().OnPoked, new System.Action<Il2CppSystem.Object>(OutlineTracker.UserProfile_OnShowComplete.HighlightUser));
      }
      if (OutlineTracker.excludedSocialIDs.Contains(((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel?.CurrentUser)?.Profile).SocialId) || ((RoomUser) __instance?.RemoteUser)?.LegacyUserId == null)
        return;
      RemoteUserController withLegacyUserId = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(((RoomUser) __instance.RemoteUser).LegacyUserId);
      if ((UnityEngine.Object) withLegacyUserId == (UnityEngine.Object) null || (UnityEngine.Object) withLegacyUserId.remoteUserSync == (UnityEngine.Object) null)
        return;
      GameObject remoteAvatarGo = withLegacyUserId.remoteUserSync.RemoteAvatarGO;
      if ((UnityEngine.Object) remoteAvatarGo == (UnityEngine.Object) null || !((UnityEngine.Object) remoteAvatarGo.transform.Find("OutlineObject") == (UnityEngine.Object) null))
        return;
      GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
      primitive.name = "OutlineObject";
      primitive.transform.SetParent(remoteAvatarGo.transform);
      primitive.transform.localPosition = Vector3.zero;
      primitive.transform.localRotation = Quaternion.identity;
      primitive.transform.localScale = new Vector3(1.5f, 1.4f, 1.5f);
      Material material = new Material(Shader.Find("Standard"));
      material.color = new Color(1f, 1f, 1f, 0.2f);
      material.SetFloat("_Mode", 3f);
      material.SetInt("_SrcBlend", 5);
      material.SetInt("_DstBlend", 10);
      material.SetInt("_ZWrite", 0);
      material.DisableKeyword("_ALPHATEST_ON");
      material.EnableKeyword("_ALPHABLEND_ON");
      material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
      material.renderQueue = 3000;
      primitive.GetComponent<Renderer>().material = material;
      primitive.gameObject.SetActive(false);
    }

    private static void SetHiglightStatus(UserProfile __instance, Transform button)
    {
      button.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Highlight";
      OutlineTracker.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = new Color(0.0f, 0.8784f, 0.5294f, 1f);
      if (((RoomUser) __instance?.RemoteUser)?.LegacyUserId == null)
        return;
      RemoteUserController withLegacyUserId = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(((RoomUser) __instance.RemoteUser).LegacyUserId);
      if ((UnityEngine.Object) withLegacyUserId == (UnityEngine.Object) null || (UnityEngine.Object) withLegacyUserId.remoteUserSync == (UnityEngine.Object) null)
        return;
      GameObject remoteAvatarGo = withLegacyUserId.remoteUserSync.RemoteAvatarGO;
      if ((UnityEngine.Object) remoteAvatarGo == (UnityEngine.Object) null || !((UnityEngine.Object) remoteAvatarGo.transform.Find("OutlineObject") != (UnityEngine.Object) null))
        return;
      if (remoteAvatarGo.transform.Find("OutlineObject").gameObject.active)
      {
        button.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Remove Highlight";
        OutlineTracker.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = new Color(0.98f, 0.42f, 0.65f);
      }
      else
      {
        button.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Highlight";
        OutlineTracker.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = new Color(0.0f, 0.8784f, 0.5294f, 1f);
      }
    }

    private static void HighlightUser(Il2CppSystem.Object @object)
    {
      UserProfile component = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Popups/UserProfile(Clone)").GetComponent<UserProfile>();
      if (((RoomUser) component?.RemoteUser)?.LegacyUserId == null)
        return;
      RemoteUserController withLegacyUserId = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(((RoomUser) component.RemoteUser).LegacyUserId);
      if ((UnityEngine.Object) withLegacyUserId == (UnityEngine.Object) null || (UnityEngine.Object) withLegacyUserId.remoteUserSync == (UnityEngine.Object) null)
        return;
      GameObject remoteAvatarGo = withLegacyUserId.remoteUserSync.RemoteAvatarGO;
      if ((UnityEngine.Object) remoteAvatarGo == (UnityEngine.Object) null)
        return;
      string socialId = ((SocialProfile) ((RoomUser) withLegacyUserId.RemoteUser).SocialProfile).SocialId;
      if (OutlineTracker.excludedSocialIDs.Contains(socialId) || OutlineTracker.excludedSocialIDs.Contains(((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel?.CurrentUser)?.Profile).SocialId) || OutlineTracker.excludedSocialIDs.Count < 1)
      {
        BigscreenBehind.Utils.FloatingNotification("Nope", duration: 1f, icon: "f06e");
      }
      else
      {
        if (!((UnityEngine.Object) remoteAvatarGo.transform.Find("OutlineObject") != (UnityEngine.Object) null))
          return;
        if (!remoteAvatarGo.transform.Find("OutlineObject").gameObject.active)
        {
          OutlineTracker.OutlineObject(remoteAvatarGo);
          ((Component) component).transform.Find("Content/Body/Btn_Highlight").transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Remove Highlight";
          OutlineTracker.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = new Color(0.98f, 0.42f, 0.65f, 1f);
        }
        else
        {
          OutlineTracker.DisableOutline(remoteAvatarGo);
          ((Component) component).transform.Find("Content/Body/Btn_Highlight").transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Highlight";
          OutlineTracker.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = new Color(0.0f, 0.8784f, 0.5294f, 1f);
        }
      }
    }
  }
}
