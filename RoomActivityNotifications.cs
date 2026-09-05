// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.RoomActivityNotifications
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.UI;
using Il2CppBigscreen.Users;
using MelonLoader;
using System.Collections.Generic;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("RoomActivityNotifications", "0.5.0", "Love")]
internal class RoomActivityNotifications : MelonMod
{
  private static MelonPreferences_Category? roomActivityNotificationsSettings;
  private static MelonPreferences_Entry<bool>? roomActivityNotifications;
  private static Dictionary<string, string> userNamesDict = new Dictionary<string, string>();

  public override void OnLateInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} Mod loaded!\nMade with LOVE\n=========================\n");
    RoomActivityNotifications.roomActivityNotificationsSettings = MelonPreferences.CreateCategory("RoomActivityNotificationsSettings");
    RoomActivityNotifications.roomActivityNotifications = RoomActivityNotifications.roomActivityNotificationsSettings.CreateEntry<bool>(nameof (RoomActivityNotifications), true, description: "");
    RoomActivityNotifications.roomActivityNotificationsSettings.SaveToFile(false);
  }

  public override void OnSceneWasInitialized(int buildIndex, string sceneName)
  {
    if (!(sceneName == "Master"))
      return;
    this.SetDelegates();
  }

  private void SetDelegates()
  {
    BIG_STATIC_SINGLETONS.remoteUsersManager.NewUserJoinedRoom += (Il2CppSystem.Action<string>) new System.Action<string>(RoomActivityNotifications.OnUserJoinedRoom);
    BIG_STATIC_SINGLETONS.currentApp.UserLeft += (Il2CppSystem.Action<string>) new System.Action<string>(RoomActivityNotifications.OnUserLeftRoom);
  }

  private static void OnUserJoinedRoom(string id)
  {
    RoomActivityNotifications.roomActivityNotificationsSettings.LoadFromFile(false);
    RemoteUserController withLegacyUserId = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(id);
    string username = ((SocialProfile) ((RoomUser) withLegacyUserId.RemoteUser).SocialProfile).Username;
    if (RoomActivityNotifications.roomActivityNotifications.Value)
      RoomActivityNotifications.FloatingNotification(username + " has joined the room", duration: 3f, icon: "f129", soundEvent: "");
    string remoteUserSessionId = withLegacyUserId.remoteUserSessionId;
    RoomActivityNotifications.userNamesDict[remoteUserSessionId] = username;
  }

  private static void OnUserLeftRoom(string id)
  {
    RoomActivityNotifications.roomActivityNotificationsSettings.LoadFromFile(false);
    string str;
    if (!RoomActivityNotifications.userNamesDict.TryGetValue(id, out str))
      return;
    if (RoomActivityNotifications.roomActivityNotifications.Value)
      RoomActivityNotifications.FloatingNotification(str + " has left the room", duration: 3f, icon: "f129", soundEvent: "");
    RoomActivityNotifications.userNamesDict.Remove(id);
  }

  private static void FloatingNotification(
    string message,
    BigUIState stateOnClicked = 19,
    float duration = 10f,
    string icon = "f0f3",
    string soundEvent = "Alert15")
  {
    ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).ShowFloatingNotification(message, stateOnClicked, duration, icon, soundEvent);
  }
}
