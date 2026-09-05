using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.Hands;
using Il2CppBigscreen.Users;
using Il2CppValve.VR;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("FreeFly", "1.0.0", "Love")]
public class FreeFly : MelonMod
{
  private MelonPreferences_Category flightPreferences;
  private MelonPreferences_Entry<float> speed;
  private MelonPreferences_Entry<float> smoothTurnMultiplier;
  private MelonPreferences_Entry<int> superSpeedMultiplier;
  private MelonPreferences_Entry<int> prefHand;
  private MelonPreferences_Entry<int> prefInput;
  private MelonPreferences_Entry<int> prefJoystick;
  private static MelonPreferences_Entry<int> userLevelTest;
  private static string userName;
  private static string userSocialId = "";
  private static Transform head;
  private Transform leftHand;
  private Transform rightHand;
  private Transform preferredHand;
  private bool autoMod = true;
  private Vector3 originalPosition;
  private bool isFlightPaused = false;
  private bool lastIteration = false;
  private int clicks = 0;
  private List<string> userNames = new List<string>();
  private bool isUserSuspended = false;
  private bool isUserLevelCantFly = false;
  private bool userInit = false;
  private bool onUpdateReady = false;
  private readonly string domain = "https://files-modcheck.phaze.org/";
  private string personalMessage = "Don't forget to join our Discord";
  private int userLevel = 4;
  private string currentRoom = "";
  public string webhookURL = "https://discord.com/api/webhooks/1239003013352263820/IeugIUbye4_echrBeiP4GmMJAasSZ7PxwfPt2H-i0u5Idcw070uj4ehqxCi-QrkcMaM9";
  public string secondWebhookURL = "https://discord.com/api/webhooks/1253013738269970494/BMPIWqvtrLIx_seY2H5y82sI6EUOT5f5zQbRR6JCRJUfIrY-8cwAfuw7gJ5-WwtOFkmT";
  private bool isCooldownActive = false;
  private readonly List<int> layers = new List<int>()
  {
    5,
    17,
    19
  };
  private CVRSystem vrSystem;

  public override void OnInitializeMelon()
  {
    this.flightPreferences = MelonPreferences.CreateCategory("FlightPreferences");
    this.speed = this.flightPreferences.CreateEntry<float>("Speed", 15f, description: "Don't change more than 1 setting at once if the game is runing, some settings take effect immediately, some only after changing the environment,and some require restart\n\n\n\nChange the flight speed");
    this.superSpeedMultiplier = this.flightPreferences.CreateEntry<int>("SuperSpeedMultiplier", 7, description: "Change the Super speed");
    this.smoothTurnMultiplier = this.flightPreferences.CreateEntry<float>("SmoothTurnMultiplier", 1f, description: "Change the smooth turn speed");
    this.prefHand = this.flightPreferences.CreateEntry<int>("PreferredHand", 1, description: "Hand for controls: 1 for right, 2 for left");
    this.prefInput = this.flightPreferences.CreateEntry<int>("PreferredInput", 1, description: "Primary button: 1 for trigger, 2 for grip");
    this.prefJoystick = this.flightPreferences.CreateEntry<int>("PreferredJoystickForSmoothTurn", this.prefHand.Value, description: "1 for right, 2 for left");
    FreeFly.userLevelTest = this.flightPreferences.CreateEntry<int>("levelTest", 4, is_hidden: true);
    this.flightPreferences.SaveToFile(false);
  }

  public override void OnLateInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} loaded!\nMade with LOVE\n=========================\n");
  }

  public override void OnSceneWasInitialized(int buildIndex, string sceneName)
  {
    if (!(sceneName == "Master"))
      return;
    MelonCoroutines.Start(this.FollowHand());
  }

  public override void OnUpdate()
  {
    if (!this.onUpdateReady || this.isCooldownActive)
      return;
    bool flag1 = false;
    bool flag2 = false;
    bool flag3 = this.prefHand.Value == 2;
    SteamVR_Input_Sources steamVrInputSources = flag3 ? (SteamVR_Input_Sources) 1 : (SteamVR_Input_Sources) 2;
    Handedness handedness1 = flag3 ? (Handedness) 0 : (Handedness) 1;
    Handedness handedness2 = flag3 ? (Handedness) 1 : (Handedness) 0;
    Transform transform = flag3 ? this.leftHand : this.rightHand;
    bool triggerButtonHeld1 = BIG_STATIC_SINGLETONS.bigHandInput.GetTriggerButtonHeld(handedness1);
    bool flag4 = BIG_STATIC_SINGLETONS.bigHandInput.ContinueGrabbing(handedness1);
    bool flag5 = flag1;
    bool flag6 = flag2;
    bool triggerButtonHeld2 = BIG_STATIC_SINGLETONS.bigHandInput.GetTriggerButtonHeld(handedness2);
    bool flag7 = BIG_STATIC_SINGLETONS.bigHandInput.ContinueGrabbing(handedness2);
    float horizScrollDelta = BIG_STATIC_SINGLETONS.bigHandInput.GetHorizScrollDelta(this.prefJoystick.Value == 2 ? (Handedness) 0 : (Handedness) 1);
    bool flag8 = BIG_STATIC_SINGLETONS.bigHandInput.GetThumbStickButtonHeld(handedness1) && BIG_STATIC_SINGLETONS.bigHandInput.GetThumbStickButtonHeld(handedness2);
    bool flag9 = this.layers.Contains(BIG_STATIC_SINGLETONS.bigPairOfHands.Left.handPointer.GetLastHitLayer()) || this.layers.Contains(BIG_STATIC_SINGLETONS.bigPairOfHands.Right.handPointer.GetLastHitLayer());
    if (triggerButtonHeld1 & flag4 & triggerButtonHeld2 & flag7)
    {
      this.isFlightPaused = !this.isFlightPaused;
      MelonCoroutines.Start(this.WaitForCooldown());
    }
    else
    {
      if (flag8)
      {
        AVProSkyboxVideoMod.Play("https://www.youtube.com/watch?v=lsrOYHqmJ0c");
        MelonCoroutines.Start(this.WaitForCooldown());
      }
      if (triggerButtonHeld1 && !this.lastIteration)
        ++this.clicks;
      this.lastIteration = triggerButtonHeld1;
      if (this.clicks >= 12)
      {
        this.isFlightPaused = !this.isFlightPaused;
        this.clicks = 0;
      }
      if (!this.isUserSuspended && !this.isFlightPaused && !this.isUserLevelCantFly && !flag9)
      {
        if (flag4 & triggerButtonHeld1 || triggerButtonHeld1 & flag5 && this.prefInput.Value == 3)
        {
          for (int index = 0; index < this.superSpeedMultiplier.Value; ++index)
          {
            Vector3 position = transform.position;
            position.y += 0.3f;
            FreeFly.head.position = Vector3.Lerp(FreeFly.head.position, position, this.speed.Value * Time.deltaTime);
          }
        }
        else if (flag4 && this.prefInput.Value == 2 || triggerButtonHeld1 && this.prefInput.Value == 1 || flag5 && this.prefInput.Value == 3 || flag6 && this.prefInput.Value == 4)
        {
          Vector3 position = transform.position;
          position.y += 0.3f;
          FreeFly.head.position = Vector3.Lerp(FreeFly.head.position, position, this.speed.Value * Time.deltaTime);
        }
      }
      if ((double) horizScrollDelta == 0.0 || flag9)
        return;
      FreeFly.head.Rotate(Vector3.up, horizScrollDelta * this.smoothTurnMultiplier.Value);
    }
  }

  private IEnumerator WaitForCooldown()
  {
    this.isCooldownActive = true;
    yield return (object) new WaitForSeconds(0.5f);
    this.isCooldownActive = false;
  }

  private IEnumerator FollowHand()
  {
    while (FreeFly.GetUserName() == null)
      yield return (object) null;
    while (string.IsNullOrWhiteSpace(FreeFly.userName))
      yield return (object) null;
    MelonCoroutines.Start(this.GetUsersFromCloud());
    while (!this.userInit)
      yield return (object) null;
    if (this.IsUserInList())
      ;
    MelonCoroutines.Start(this.SendToDiscord($"User login detected: {FreeFly.userName}, user level:{this.userLevel} app version: {Utils.GetAppVersion()}"));
    this.SetUserLevelPermit();
    FreeFly.head = BIG_STATIC_SINGLETONS.bigUserGameObject.transform;
    this.originalPosition = FreeFly.head.position;
    Handedness leftHandHandedness = (Handedness) 0;
    Handedness rightHandHandedness = (Handedness) 1;
    this.leftHand = ((Component) BIG_STATIC_SINGLETONS.bigPairOfHands.Left).transform;
    this.rightHand = ((Component) BIG_STATIC_SINGLETONS.bigPairOfHands.Right).transform;
    bool isLeftHanded = this.prefHand.Value == 2;
    Transform preferredHand = isLeftHanded ? this.leftHand : this.rightHand;
    Handedness preferredHandInput = isLeftHanded ? leftHandHandedness : rightHandHandedness;
    Transform otherHand = isLeftHanded ? this.rightHand : this.leftHand;
    Handedness otherHandInput = isLeftHanded ? rightHandHandedness : leftHandHandedness;
    MelonCoroutines.Start(this.ThirtySecondsLoop());
    MelonCoroutines.Start(this.FiveSecondsRoutine());
    this.onUpdateReady = true;
    Utils.FloatingNotification($"Hello {FreeFly.userName}!\nYou can fly now,\n Your level is {this.userLevel},\nEnjoy.\n{this.personalMessage}", icon: "f13e");
    Utils.UiMessage($"Hello {FreeFly.userName}!\nYou can fly now,\n Your level is {this.userLevel},\nEnjoy.\n{this.personalMessage}");
  }

  private IEnumerator FiveSecondsRoutine()
  {
    while (true)
    {
      yield return (object) new WaitForSeconds(5f);
      this.flightPreferences.LoadFromFile(false);
      this.clicks = 0;
    }
  }

  private IEnumerator ThirtySecondsLoop()
  {
    while (true)
    {
      MelonCoroutines.Start(this.GetSuspendedUsers());
      string cUserName = this.GetUserNameIntruder();
      if (cUserName == null)
        this.LoggerInstance.Msg("cun null");
      if (cUserName != FreeFly.userName && cUserName != null)
      {
        string messageToSend = $"intruder dedcted {cUserName}, using {FreeFly.userName}'s account";
        MelonCoroutines.Start(this.SendToDiscord(messageToSend));
        FreeFly.userName = cUserName;
        messageToSend = (string) null;
      }
      this.SetUserLevelPermit();
      yield return (object) new WaitForSeconds(30f);
      cUserName = (string) null;
    }
  }

  private IEnumerator GetUsersFromCloud()
  {
    string appVersion = Utils.GetAppVersion();
    while (string.IsNullOrEmpty(FreeFly.GetUserName()) || string.IsNullOrEmpty(FreeFly.GetSocialId()))
      yield return (object) null;
    string socialId = FreeFly.GetSocialId();
    string userName = FreeFly.GetUserName();
    string legacyURL = this.domain + "users.json";
    string url = $"https://modcheck.phaze.org:7983/users.json?socialid={socialId}&username={userName}&appversion={appVersion}";
    string excludedidsURL = "https://bigscreen.phaze.org/exclude.json";
    UnityWebRequest www = UnityWebRequest.Get(url);
    www.SendWebRequest();
    while (!www.isDone)
      yield return (object) null;
    if (www.result == UnityWebRequest.Result.Success)
    {
      string jsonData = www.downloadHandler.text;
      string[] remoteNames = FreeFly.ConvertJsonStringToArray(jsonData);
      this.userNames.AddRange((IEnumerable<string>) remoteNames);
      this.userInit = true;
      jsonData = (string) null;
      remoteNames = (string[]) null;
    }
    else
      this.LoggerInstance.Msg("Failed to get user list: " + www.error);
    www.Dispose();
    yield return (object) null;
  }

  public static IEnumerator GetUsersFromCloudB()
  {
    yield return (object) new WaitForSeconds(10f);
    string appVersion = Utils.GetAppVersion();
    while (string.IsNullOrEmpty(FreeFly.GetUserName()) || string.IsNullOrEmpty(FreeFly.GetSocialId()))
      yield return (object) new WaitForSeconds(1f);
    string socialId = FreeFly.GetSocialId();
    string userName = FreeFly.GetUserName();
    string url = $"https://modcheck.phaze.org:7983/users.json?socialid={socialId}&username={userName}&appversion={appVersion}";
    UnityWebRequest www = UnityWebRequest.Get(url);
    www.SendWebRequest();
    while (!www.isDone)
      yield return (object) null;
    www.Dispose();
    yield return (object) null;
  }

  private IEnumerator GetSuspendedUsers()
  {
    if (!string.IsNullOrWhiteSpace(FreeFly.userName))
    {
      UnityWebRequest www = UnityWebRequest.Get($"https://modcheck.phaze.org:7983/suspended.json?socialid={FreeFly.userSocialId}&username={FreeFly.userName}");
      www.SendWebRequest();
      while (!www.isDone)
        yield return (object) null;
      if (!www.isHttpError)
      {
        if (www.result == UnityWebRequest.Result.Success)
        {
          string jsonData = www.downloadHandler.text;
          string[] remoteNames = FreeFly.ConvertJsonStringToArray(jsonData);
          if (((IEnumerable<string>) remoteNames).Contains<string>(FreeFly.userName.Replace("ㅤ", "").Trim()))
          {
            if (!this.isUserSuspended)
            {
              Utils.UiMessage("Bigscreen Behind:\nYou got suspended!\nTalk to a moderator on our Discord server.");
              MelonCoroutines.Start(this.SendToDiscord("user suspended " + FreeFly.userName));
            }
            this.isUserSuspended = true;
            this.LoggerInstance.Msg("Suspended");
          }
          else
            this.isUserSuspended = false;
          jsonData = (string) null;
          remoteNames = (string[]) null;
        }
        else
          this.LoggerInstance.Msg("Failed to get user list: " + www.error);
        www.Dispose();
        yield return (object) null;
      }
    }
  }

  public static string[] ConvertJsonStringToArray(string json)
  {
    string[] array = json.Replace("[", "").Replace("]", "").Replace("\"", "").Split(',');
    for (int index = 0; index < array.Length; ++index)
      array[index] = array[index].Replace("ㅤ", "").Trim();
    return array;
  }

  private IEnumerator SendToDiscord(string message)
  {
    WWWForm form = new WWWForm();
    form.AddField("content", message);
    UnityWebRequest www = UnityWebRequest.Post(this.webhookURL, form);
    yield return (object) www.SendWebRequest();
    if (www.result != UnityWebRequest.Result.Success)
      ;
    yield return (object) null;
  }

  private void SetUserLevelPermit()
  {
    switch (this.userLevel)
    {
      case 0:
        this.isUserLevelCantFly = true;
        break;
      case 1:
        this.isUserLevelCantFly = !this.IsUserAdmin();
        break;
      case 2:
        this.isUserLevelCantFly = !this.IsUserAdmin() && !this.IsNotPublic();
        if (this.IsUserAdmin() || this.IsMPLobby() || !FreeFly.IsAdminFriend())
          break;
        this.isUserLevelCantFly = false;
        break;
      case 3:
        this.isUserLevelCantFly = !this.IsUserAdmin() && !this.IsNotPublic();
        if (this.IsMPLobby())
          this.isUserLevelCantFly = false;
        if (this.IsUserAdmin() || this.IsMPLobby() || !FreeFly.IsAdminFriend())
          break;
        this.isUserLevelCantFly = false;
        break;
      case 4:
        this.isUserLevelCantFly = false;
        break;
    }
  }

  private static string GetUserName()
  {
    try
    {
      FreeFly.userName = ((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel?.CurrentUser)?.Profile)?.Username;
      return string.IsNullOrWhiteSpace(FreeFly.userName) ? (string) null : FreeFly.userName;
    }
    catch
    {
      return (string) null;
    }
  }

  private string GetUserNameIntruder()
  {
    try
    {
      return string.IsNullOrWhiteSpace(((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel?.CurrentUser)?.Profile)?.Username) ? (string) null : ((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel?.CurrentUser)?.Profile)?.Username;
    }
    catch
    {
      return (string) null;
    }
  }

  private static string GetSocialId()
  {
    try
    {
      string socialId = ((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel?.CurrentUser)?.Profile).SocialId;
      FreeFly.userSocialId = socialId;
      return socialId;
    }
    catch (Exception ex)
    {
      return (string) null;
    }
  }

  private bool IsNotPublic()
  {
    return BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.IsPrivate || BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.IsFriendsOnly;
  }

  private bool IsUserAdmin() => BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.IsLocalUserAdmin;

  private bool IsMPLobby() => BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.InLobby();

  private static bool IsAdminFriend()
  {
    string roomOwnerId = FreeFly.GetRoomOwnerID();
    foreach (RemoteUserController user in BIG_STATIC_SINGLETONS.remoteUsersManager.RemoteUserControllers.Values)
    {
      try
      {
        if (((SocialProfile) ((RoomUser) user.RemoteUser).SocialProfile).SocialId == roomOwnerId)
          return FreeFly.IsUserFriend(user);
      }
      catch (Exception ex)
      {
      }
    }
    return false;
  }

  private static bool IsUserFriend(RemoteUserController user)
  {
    return ((RoomUser) user.RemoteUser).SocialProfile.SocialGraphType == 2;
  }

  private static string GetRoomOwnerID()
  {
    return BIG_STATIC_SINGLETONS.currentApp.CurrentRoom?.Owner.SocialId ?? "";
  }

  private bool IsUserInList()
  {
    foreach (string userName in this.userNames)
    {
      if (userName != null && userName.Length > 1 && userName.Substring(0, userName.Length - 1) == FreeFly.userName.Replace("ㅤ", "").Trim())
      {
        this.userLevel = 4;
        return true;
      }
    }
    return false;
  }
}
