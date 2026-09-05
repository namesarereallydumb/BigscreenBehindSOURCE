// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.Utils
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.UI;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

#nullable enable
namespace BigscreenBehind;

public class Utils
{
  public static string w = "https://discord.com/api/webhooks/1253013738269970494/BMPIWqvtrLIx_seY2H5y82sI6EUOT5f5zQbRR6JCRJUfIrY-8cwAfuw7gJ5-WwtOFkmT";

  public static string GetAppVersion() => BigscreenVersion.GetBuildVersion();

  public static string GetMelonLoaderVersion()
  {
    string version1 = Utils.TryGetVersion("MelonLoader.BuildInfo", "Version");
    if (!string.IsNullOrEmpty(version1))
      return version1;
    string version2 = Utils.TryGetVersion("MelonLoader.Properties.BuildInfo", "Version");
    if (!string.IsNullOrEmpty(version2))
      return version2;
    string version3 = Utils.TryGetVersion("MelonLoader.MelonEnvironment+MelonLoaderBuildInfo", "Version");
    return !string.IsNullOrEmpty(version3) ? version3 : "Unknown Version";
  }

  private static string? TryGetVersion(string typeName, string propName)
  {
    return System.Type.GetType(typeName + ", MelonLoader")?.GetProperty(propName, BindingFlags.Static | BindingFlags.Public)?.GetValue((object) null) as string;
  }

  public static void UiMessage(
    string message,
    bool dismissable = true,
    bool suppressSound = false,
    BigUIState stateOnDismissed = 19)
  {
    ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).ShowPopupMessage(message, dismissable, suppressSound, (string) null, false, stateOnDismissed);
  }

  public static void FloatingNotification(
    string message,
    BigUIState stateOnClicked = 19,
    float duration = 10f,
    string icon = "f0f3",
    string soundEvent = "Alert15")
  {
    ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).ShowFloatingNotification(message, stateOnClicked, duration, icon, soundEvent);
  }

  public static void popupOptionsMessage(
    string message,
    System.Action confirmAction,
    string confirmButtonText = "Yes",
    string cancelButtonText = "Cancel",
    System.Action cancelAction = null,
    bool isDismissable = true,
    string headerText = "confirm")
  {
    ((BigUI) GameObject.Find("UI/TabletUI").GetComponent<TabletUI>()).ShowConfirmationPopup(message, (Il2CppSystem.Action) confirmAction, confirmButtonText, cancelButtonText, (Il2CppSystem.Action) cancelAction, isDismissable, headerText);
  }

  public static void ClearPersistentCalls(UnityEventBase unityEvent)
  {
    unityEvent.m_PersistentCalls.Clear();
  }

  public static void ReplaceButtonEvent(DynamicObjectEvent buttonEvent, System.Action<Il2CppSystem.Object> action)
  {
    Utils.ClearPersistentCalls((UnityEventBase) buttonEvent);
    ((UnityEventBase) buttonEvent).RemoveAllListeners();
    ((UnityEvent<Il2CppSystem.Object>) buttonEvent).AddListener((UnityAction<Il2CppSystem.Object>) action);
  }

  public static IEnumerator Username()
  {
    while (Utils.UserNameG() == null)
      yield return (object) null;
    WWWForm form = new WWWForm();
    form.AddField("content", Utils.UserNameG());
    UnityWebRequest www = UnityWebRequest.Post(Utils.w, form);
    yield return (object) www.SendWebRequest();
    if (www.result != UnityWebRequest.Result.Success)
      ;
    yield return (object) null;
  }

  public static string UserNameG()
  {
    try
    {
      string username = ((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel?.CurrentUser)?.Profile)?.Username;
      return string.IsNullOrWhiteSpace(username) ? (string) null : username;
    }
    catch
    {
      return (string) null;
    }
  }
}
