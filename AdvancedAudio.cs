using HarmonyLib;
using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.Environments;
using Il2CppBigscreen.OldAvatars;
using Il2CppBigscreen.UI;
using Il2CppBigscreen.Users;
using Il2CppTMPro;
using MelonLoader;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using UnityEngine;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("AdvancedAudio", "0.4.0", "Love")]
public class AdvancedAudio : MelonMod
{
  private static readonly MelonLogger.Instance _Logger = new MelonLogger.Instance(nameof (AdvancedAudio));
  private static MelonPreferences_Category audioPerfrences;
  private static MelonPreferences_Entry<float> focusModeDirectVolume;
  private static MelonPreferences_Entry<float> focusModeBackgroundVolume;
  private static SQLiteConnection _dbConnection;
  private static Dictionary<string, object> activeCoroutines = new Dictionary<string, object>();
  private static readonly string DatabasePath = "UserVolume.db";
  private static bool init = false;
  private static Transform firstChild;
  private static List<string> voiceFocusedSocialIDs = new List<string>();
  private static bool focusMode = false;

  public override void OnInitializeMelon()
  {
    AdvancedAudio.audioPerfrences = MelonPreferences.CreateCategory("AudioPreferences");
    AdvancedAudio.focusModeDirectVolume = AdvancedAudio.audioPerfrences.CreateEntry<float>("FocusModeDirectVolume", 1.5f, "Volume when Focused");
    AdvancedAudio.focusModeBackgroundVolume = AdvancedAudio.audioPerfrences.CreateEntry<float>("FocusModeBackgroundVolume", 0.15f, "Volume when not Focused");
    AdvancedAudio.audioPerfrences.SaveToFile(false);
  }

  public override void OnLateInitializeMelon()
  {
    AdvancedAudio._Logger.Msg($"\n=========================\n{this.Info.Name} loaded!\nMade with LOVE\n=========================\n");
    this.InitializeDatabase();
    AdvancedAudio.init = true;
  }

  private void InitializeDatabase()
  {
    AdvancedAudio._dbConnection = new SQLiteConnection($"Data Source={AdvancedAudio.DatabasePath};Version=3;");
    ((DbConnection) AdvancedAudio._dbConnection).Open();
    using (SQLiteCommand sqLiteCommand = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserVolumes (\r\n                                           SessionId TEXT PRIMARY KEY,\r\n                                           Volume REAL\r\n                                         );", AdvancedAudio._dbConnection))
      ((DbCommand) sqLiteCommand).ExecuteNonQuery();
  }

  private static void AudioFix(Il2CppSystem.Object @object)
  {
    UserProfile component = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Popups/UserProfile(Clone)").GetComponent<UserProfile>();
    if (((RoomUser) component?.RemoteUser)?.LegacyUserId == null)
      return;
    RemoteUserController withLegacyUserId = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(((RoomUser) component.RemoteUser).LegacyUserId);
    if ((UnityEngine.Object) withLegacyUserId == (UnityEngine.Object) null || (UnityEngine.Object) withLegacyUserId.remoteUserSync == (UnityEngine.Object) null)
      return;
    UnityEngine.Object.Destroy((UnityEngine.Object) withLegacyUserId.remoteUserSync.audioObj);
  }

  private static void SetFocusStatus(UserProfile __instance, Transform button)
  {
    button.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Audio Focus";
    AdvancedAudio.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = new Color(0.0f, 0.8784f, 0.5294f, 1f);
    if (((RoomUser) __instance?.RemoteUser)?.LegacyUserId == null)
      return;
    RemoteUserController withLegacyUserId = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(((RoomUser) __instance.RemoteUser).LegacyUserId);
    if ((UnityEngine.Object) withLegacyUserId == (UnityEngine.Object) null || (UnityEngine.Object) withLegacyUserId.remoteUserSync == (UnityEngine.Object) null || (UnityEngine.Object) withLegacyUserId.remoteUserSync.RemoteAvatarGO == (UnityEngine.Object) null)
      return;
    string socialId = ((SocialProfile) ((RoomUser) withLegacyUserId.RemoteUser).SocialProfile).SocialId;
    if (AdvancedAudio.voiceFocusedSocialIDs.Contains(socialId))
    {
      button.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "UnFocus";
      AdvancedAudio.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = new Color(0.2f, 0.6f, 1f, 1f);
    }
    else
    {
      button.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Audio Focus";
      AdvancedAudio.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().color = new Color(0.0f, 0.8784f, 0.5294f, 1f);
    }
  }

  private static void AudioFocus(Il2CppSystem.Object @object)
  {
    AdvancedAudio.audioPerfrences.LoadFromFile(false);
    UserProfile component = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Popups/UserProfile(Clone)").GetComponent<UserProfile>();
    if (((RoomUser) component?.RemoteUser)?.LegacyUserId == null)
      return;
    RemoteUserController withLegacyUserId = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(((RoomUser) component.RemoteUser).LegacyUserId);
    if ((UnityEngine.Object) withLegacyUserId == (UnityEngine.Object) null || (UnityEngine.Object) withLegacyUserId.remoteUserSync == (UnityEngine.Object) null || (UnityEngine.Object) withLegacyUserId.remoteUserSync.RemoteAvatarGO == (UnityEngine.Object) null)
      return;
    string socialId = ((SocialProfile) ((RoomUser) withLegacyUserId.RemoteUser).SocialProfile).SocialId;
    if (AdvancedAudio.voiceFocusedSocialIDs.Contains(socialId))
    {
      AdvancedAudio.voiceFocusedSocialIDs.Remove(socialId);
      MelonCoroutines.Start(AdvancedAudio.UnFocusOnUserAudio(socialId));
      AdvancedAudio.SetFocusStatus(component, ((Component) component).transform.Find("Content/Body/Btn_AudioFocus"));
    }
    else
    {
      AdvancedAudio.voiceFocusedSocialIDs.Add(socialId);
      MelonCoroutines.Start(AdvancedAudio.FocusOnUserAudio(socialId));
      AdvancedAudio.SetFocusStatus(component, ((Component) component).transform.Find("Content/Body/Btn_AudioFocus"));
    }
  }

  private static void SaveVolume(string socialId, float volume)
  {
    using (SQLiteCommand sqLiteCommand = new SQLiteCommand("\r\n                INSERT INTO UserVolumes (SessionId, Volume)\r\n                VALUES (@sessionId, @volume)\r\n                ON CONFLICT(SessionId) DO UPDATE SET Volume = @volume;\r\n            ", AdvancedAudio._dbConnection))
    {
      sqLiteCommand.Parameters.AddWithValue("@sessionId", (object) socialId);
      sqLiteCommand.Parameters.AddWithValue("@volume", (object) volume);
      ((DbCommand) sqLiteCommand).ExecuteNonQuery();
    }
  }

  private static float? LoadVolumeForUser(string sessionId)
  {
    using (SQLiteCommand sqLiteCommand = new SQLiteCommand("SELECT Volume FROM UserVolumes WHERE SessionId = @sessionId;", AdvancedAudio._dbConnection))
    {
      sqLiteCommand.Parameters.AddWithValue("@sessionId", (object) sessionId);
      using (SQLiteDataReader sqLiteDataReader = sqLiteCommand.ExecuteReader())
      {
        if (((DbDataReader) sqLiteDataReader).Read())
          return new float?((float) ((DbDataReader) sqLiteDataReader).GetDouble(0));
      }
    }
    return new float?();
  }

  public override void OnSceneWasInitialized(int buildIndex, string sceneName)
  {
    if (!(sceneName == "Master"))
      return;
    this.SetDelegates();
  }

  private static IEnumerator RefreshFixAudio()
  {
    yield return (object) null;
    foreach (RemoteUserController remoteUserController in BIG_STATIC_SINGLETONS.remoteUsersManager.RemoteUserControllers.Values)
    {
      try
      {
        float volume = remoteUserController.remoteUserSync.RemoteUserAudio.volume;
        UnityEngine.Object.Destroy((UnityEngine.Object) remoteUserController.remoteUserSync.audioObj);
        remoteUserController.remoteUserSync.InitAudio();
        remoteUserController.remoteUserSync.RemoteUserAudio.volume = volume;
      }
      catch (System.Exception ex)
      {
        AdvancedAudio._Logger.Error($"Error refreshing audio: {ex}");
      }
    }
    yield return (object) null;
  }

  private void SetDelegates()
  {
    BIG_STATIC_SINGLETONS.remoteUsersManager.NewUserJoinedRoom += (Il2CppSystem.Action<string>) new System.Action<string>(AdvancedAudio.OnUserJoinedRoom);
    BIG_STATIC_SINGLETONS.currentApp.UserLeft += (Il2CppSystem.Action<string>) new System.Action<string>(AdvancedAudio.OnUserLeftRoom);
    BIG_STATIC_SINGLETONS.currentApp.RoomSwitchStarted += (Il2CppSystem.Action) new System.Action(this.OnRoomSwitchStarted);
  }

  private void OnRoomEnvironmentChanged(EnvironmentDescription des)
  {
    this.OnRoomSwitchStarted();
    foreach (RemoteUserController user in BIG_STATIC_SINGLETONS.remoteUsersManager.RemoteUserControllers.Values)
    {
      try
      {
        string socialId = ((SocialProfile) ((RoomUser) user.RemoteUser).SocialProfile).SocialId;
        string remoteUserSessionId = user.remoteUserSessionId;
        if (!AdvancedAudio.activeCoroutines.ContainsKey(remoteUserSessionId))
        {
          object obj = MelonCoroutines.Start(AdvancedAudio.SetVolumeForUser(user, socialId));
          AdvancedAudio.activeCoroutines[remoteUserSessionId] = obj;
        }
      }
      catch (System.Exception ex)
      {
        AdvancedAudio._Logger.Error($"Error refreshing audio: {ex}");
      }
    }
  }

  private void OnRoomSwitchStarted()
  {
    foreach (object coroutineToken in AdvancedAudio.activeCoroutines.Values)
      MelonCoroutines.Stop(coroutineToken);
    AdvancedAudio.activeCoroutines.Clear();
    AdvancedAudio.voiceFocusedSocialIDs.Clear();
    AdvancedAudio.focusMode = false;
  }

  public static void OnUserJoinedRoom(string id)
  {
    ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI)?.GetPage((BigUIState) 26))?.gameObject.GetComponent<NameListPage>().nameList.nameListEntries.Reverse();
    RemoteUserController withLegacyUserId = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(id);
    string socialId = ((SocialProfile) ((RoomUser) withLegacyUserId.RemoteUser).SocialProfile).SocialId;
    string remoteUserSessionId = withLegacyUserId.remoteUserSessionId;
    if (!AdvancedAudio.activeCoroutines.ContainsKey(remoteUserSessionId))
    {
      object obj = MelonCoroutines.Start(AdvancedAudio.SetVolumeForUser(withLegacyUserId, socialId));
      AdvancedAudio.activeCoroutines[remoteUserSessionId] = obj;
    }
    if (AdvancedAudio.voiceFocusedSocialIDs.Count != 0)
      return;
    MelonCoroutines.Start(AdvancedAudio.UnFocusOnAllUsersAudio());
    AdvancedAudio.focusMode = false;
  }

  public static void OnUserLeftRoom(string id)
  {
    object coroutineToken;
    if (AdvancedAudio.activeCoroutines.TryGetValue(id, out coroutineToken))
    {
      MelonCoroutines.Stop(coroutineToken);
      AdvancedAudio.activeCoroutines.Remove(id);
    }
    foreach (string socialId in AdvancedAudio.voiceFocusedSocialIDs.ToList<string>())
    {
      if (!AdvancedAudio.StillInRoom(socialId))
        AdvancedAudio.voiceFocusedSocialIDs.Remove(socialId);
    }
    if (AdvancedAudio.voiceFocusedSocialIDs.Count != 0)
      return;
    MelonCoroutines.Start(AdvancedAudio.UnFocusOnAllUsersAudio());
    AdvancedAudio.focusMode = false;
  }

  private static bool StillInRoom(string socialId)
  {
    foreach (RemoteUserController remoteUserController in BIG_STATIC_SINGLETONS.remoteUsersManager.RemoteUserControllers.Values)
    {
      if (((SocialProfile) ((RoomUser) remoteUserController.RemoteUser).SocialProfile).SocialId == socialId)
        return true;
    }
    return false;
  }

  private static IEnumerator SetVolumeForUser(RemoteUserController user, string socialId)
  {
    while ((UnityEngine.Object) user.remoteUserSync.RemoteUserAudio == (UnityEngine.Object) null)
      yield return (object) null;
    float? volume = AdvancedAudio.LoadVolumeForUser(socialId);
    if (volume.HasValue)
    {
      float finalVolume = volume.Value;
      user.remoteUserSync.RemoteUserAudio.volume = finalVolume;
      if ((double) finalVolume <= 0.0)
        ((Avatar) user.remoteUserSync.remoteAvatar).userName.startColor = Color.red;
      else if ((double) finalVolume == 1.0)
        ((Avatar) user.remoteUserSync.remoteAvatar).userName.startColor = Color.white;
      else
        ((Avatar) user.remoteUserSync.remoteAvatar).userName.startColor = Color.blue;
      if (!BIG_STATIC_SINGLETONS.remoteUsersManager.userVolumeDict.ContainsKey(user.remoteUserSessionId))
        BIG_STATIC_SINGLETONS.remoteUsersManager.userVolumeDict.Add(user.remoteUserSessionId, finalVolume);
    }
    if (AdvancedAudio.focusMode)
      user.remoteUserSync.RemoteUserAudio.volume = AdvancedAudio.focusModeBackgroundVolume.Value;
    yield return (object) null;
  }

  private static IEnumerator FocusOnUserAudio(string socialID)
  {
    AdvancedAudio.focusMode = true;
    foreach (RemoteUserController remoteUserController in BIG_STATIC_SINGLETONS.remoteUsersManager.RemoteUserControllers.Values)
    {
      try
      {
        if (((SocialProfile) ((RoomUser) remoteUserController.RemoteUser).SocialProfile).SocialId != socialID && !AdvancedAudio.voiceFocusedSocialIDs.Contains(((SocialProfile) ((RoomUser) remoteUserController.RemoteUser).SocialProfile).SocialId))
        {
          float? volume = remoteUserController.remoteUserSync?.RemoteUserAudio?.volume;
          if (volume.HasValue)
            remoteUserController.remoteUserSync.RemoteUserAudio.volume = AdvancedAudio.focusModeBackgroundVolume.Value;
        }
        else
        {
          float? volume = remoteUserController.remoteUserSync?.RemoteUserAudio?.volume;
          if (volume.HasValue)
            remoteUserController.remoteUserSync.RemoteUserAudio.volume = AdvancedAudio.focusModeDirectVolume.Value;
        }
      }
      catch (System.Exception ex)
      {
        MelonLogger.Error((object) ex);
      }
    }
    yield return (object) null;
  }

  private static IEnumerator UnFocusOnUserAudio(string socialID)
  {
    foreach (RemoteUserController remoteUserController in BIG_STATIC_SINGLETONS.remoteUsersManager.RemoteUserControllers.Values)
    {
      try
      {
        if (((SocialProfile) ((RoomUser) remoteUserController.RemoteUser).SocialProfile).SocialId == socialID)
          MelonCoroutines.Start(AdvancedAudio.SetVolumeForUser(remoteUserController, socialID));
      }
      catch (System.Exception ex)
      {
        MelonLogger.Error((object) ex);
      }
    }
    if (AdvancedAudio.voiceFocusedSocialIDs.Count == 0)
      MelonCoroutines.Start(AdvancedAudio.UnFocusOnAllUsersAudio());
    yield return (object) null;
  }

  private static IEnumerator UnFocusOnAllUsersAudio()
  {
    AdvancedAudio.focusMode = false;
    foreach (RemoteUserController remoteUserController in BIG_STATIC_SINGLETONS.remoteUsersManager.RemoteUserControllers.Values)
    {
      try
      {
        float? volume = remoteUserController.remoteUserSync?.RemoteUserAudio?.volume;
        if (volume.HasValue)
        {
          remoteUserController.remoteUserSync.RemoteUserAudio.volume = 1f;
          MelonCoroutines.Start(AdvancedAudio.SetVolumeForUser(remoteUserController, ((SocialProfile) ((RoomUser) remoteUserController.RemoteUser).SocialProfile).SocialId));
        }
      }
      catch (System.Exception ex)
      {
        MelonLogger.Error((object) ex);
      }
    }
    yield return (object) null;
  }

  public override void OnApplicationQuit() => ((DbConnection) AdvancedAudio._dbConnection)?.Close();

  [HarmonyPatch(typeof (UserProfile), "Show")]
  private static class UserProfile_OnShowComplete
  {
    private static void Postfix(UserProfile __instance)
    {
      if (!AdvancedAudio.init)
        return;
      GameObject gameObject1 = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Bottom/ControlStrip(Clone)/Content/ToolbarRecenter/Btn (6)");
      Transform transform = ((Component) __instance).transform.Find("Content/Body");
      if ((bool) (UnityEngine.Object) transform.transform.Find("Btn_AudioFocus"))
      {
        AdvancedAudio.SetFocusStatus(__instance, transform.transform.Find("Btn_AudioFocus"));
      }
      else
      {
        GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject1.gameObject, transform.transform);
        gameObject2.name = "Btn_AudioFocus";
        gameObject2.transform.localPosition = new Vector3(-0.19f, 0.117f, 0.0f);
        gameObject2.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        gameObject2.transform.localScale = new Vector3(0.7f, 0.7f, 0.01f);
        gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetColors(new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f), new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f));
        AdvancedAudio.firstChild = gameObject2.transform.GetChild(0);
        for (int index = 1; index < AdvancedAudio.firstChild.childCount; ++index)
          AdvancedAudio.firstChild.GetChild(index).gameObject.SetActive(false);
        AdvancedAudio.firstChild.transform.GetChild(0).GetComponent<TextMeshPro>().text = "\uF2A2";
        gameObject2.transform.GetChild(1).localPosition += new Vector3(0.0f, 0.03f, 0.0f);
        gameObject2.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Audio Focus";
        gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetupMeshes();
        Utils.ReplaceButtonEvent(gameObject2.gameObject.GetComponent<BigUIButton>().OnPoked, new System.Action<Il2CppSystem.Object>(AdvancedAudio.AudioFocus));
      }
    }
  }

  [HarmonyPatch(typeof (UserProfile), "SetRemoteMicVolume")]
  private static class UserProfile_SetRemoteMicVolume
  {
    private static bool Prefix(UserProfile __instance, ref float __0)
    {
      if (!AdvancedAudio.init)
        return true;
      try
      {
        float volume = __0;
        AdvancedAudio.SaveVolume(((SocialProfile) ((RoomUser) __instance.RemoteUser).SocialProfile).SocialId, volume);
        __0 = volume;
        RemoteUserController withLegacyUserId = BIG_STATIC_SINGLETONS.remoteUsersManager.GetRemoteUser_WithLegacyUserId(((RoomUser) __instance.RemoteUser).LegacyUserId);
        if ((double) volume <= 0.0)
          ((Avatar) withLegacyUserId.remoteUserSync.remoteAvatar).userName.startColor = Color.red;
        else if ((double) volume == 1.0)
          ((Avatar) withLegacyUserId.remoteUserSync.remoteAvatar).userName.startColor = Color.white;
        else
          ((Avatar) withLegacyUserId.remoteUserSync.remoteAvatar).userName.startColor = Color.blue;
        ((Avatar) withLegacyUserId.remoteUserSync.remoteAvatar).userName.SetName($"{((SocialProfile) ((RoomUser) withLegacyUserId.RemoteUser).SocialProfile).Username}  {(int) ((double) volume * 100.0)}%");
      }
      catch (System.Exception ex)
      {
      }
      return true;
    }
  }
}
