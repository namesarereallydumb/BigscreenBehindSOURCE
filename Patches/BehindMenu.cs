// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.Patches.BehindMenu
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using HarmonyLib;
using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.UI;
using Il2CppSimpleJSONBigscreen;
using Il2CppTMPro;
using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#nullable enable
namespace BigscreenBehind.Patches;

internal class BehindMenu
{
  public static TextMeshPro badgeNumber;
  public static int msgCount = 0;
  public static bool isCooldownActive = false;
  private static GameObject clonedBadge;
  private static ClientWebSocket socket;
  private static string lastRoom = (string) null;
  private static bool lastImAdmin = false;
  private static bool isConnected = false;
  private static bool shouldRun = true;
  private static string lastSocialId = (string) null;
  private static object _mainLoop;

  public static IEnumerator SetMenu()
  {
    while ((UnityEngine.Object) GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Bottom/ControlStrip(Clone)/Content/Btn (3)/") == (UnityEngine.Object) null)
      yield return (object) null;
    GameObject bt = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Bottom/ControlStrip(Clone)/Content/Btn (3)/");
    ((Behaviour) bt.GetComponent<BigUIButton>()).enabled = true;
    bt.GetComponent<BigUIButton>().SetPokeable(true);
    DynamicObjectEvent p = bt.GetComponent<BigUIButton>().OnPoked;
    Utils.ReplaceButtonEvent(p, new System.Action<Il2CppSystem.Object>(BehindMenu.OnButtonClicked));
    BigUIPage editroompage = ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GetPage((BigUIState) 38);
    ((Component) editroompage).transform.Find("Header/TextMeshPro (2)/").gameObject.GetComponent<TextMeshPro>().text = "Behind Settings";
    BigUIButtonVisualization vis = bt.GetComponent<BigUIButton>().Visualization;
    vis.SetSecondaryText("M");
    BigUIButton button5 = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Bottom/ControlStrip(Clone)/Content/Btn (5)/").gameObject.GetComponent<BigUIButton>();
    GameObject clonedButton = UnityEngine.Object.Instantiate<GameObject>(button5.Visualization.ButtonText.gameObject, ((Component) vis).transform);
    vis.buttonText = clonedButton.GetComponent<TextMeshPro>();
    clonedButton.transform.localPosition = new Vector3(0.005f, 3f / 500f, 0.0159f);
    clonedButton.GetComponent<TextMeshPro>().text = "\uF0E0";
    GameObject badge = ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GetPage((BigUIState) 200)).gameObject.GetComponent<NavMenu_Social>().socialBadge.gameObject;
    BehindMenu.clonedBadge = UnityEngine.Object.Instantiate<GameObject>(badge, ((Component) vis).transform);
    BehindMenu.clonedBadge.transform.localPosition = new Vector3(-11f / 625f, 0.02f, 0.016f);
    BehindMenu.badgeNumber = BehindMenu.clonedBadge.transform.Find("TextMeshPro (4)").gameObject.GetComponent<TextMeshPro>();
    BehindMenu.badgeNumber.text = "0";
    BehindMenu.StartStatusSocket();
  }

  private static void OnButtonClicked(Il2CppSystem.Object @object)
  {
    if (BehindMenu.isCooldownActive)
      return;
    MelonCoroutines.Start(BehindMenu.CoolDown());
    MelonCoroutines.Start(BehindMenu.GetLastMessage());
  }

  private static IEnumerator CoolDown()
  {
    BehindMenu.isCooldownActive = true;
    yield return (object) new WaitForSeconds(5f);
    BehindMenu.isCooldownActive = false;
  }

  public static void StartStatusSocket()
  {
    if (BehindMenu._mainLoop != null)
      return;
    BehindMenu._mainLoop = MelonCoroutines.Start(BehindMenu.MainWebSocketLoop());
  }

  public static void StopStatusSocket()
  {
    BehindMenu.shouldRun = false;
    if (BehindMenu.socket != null)
    {
      BehindMenu.socket.Abort();
      BehindMenu.socket.Dispose();
      BehindMenu.socket = (ClientWebSocket) null;
    }
    if (BehindMenu._mainLoop != null)
    {
      MelonCoroutines.Stop(BehindMenu._mainLoop);
      BehindMenu._mainLoop = (object) null;
    }
    BehindMenu.isConnected = false;
  }

  private static IEnumerator MainWebSocketLoop()
  {
    while (string.IsNullOrEmpty(((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel.CurrentUser).Profile).SocialId))
      yield return (object) null;
    BehindMenu.shouldRun = true;
    BehindMenu.lastSocialId = ((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel.CurrentUser).Profile).SocialId;
    while (BehindMenu.shouldRun)
    {
      if (((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel.CurrentUser).Profile).SocialId != BehindMenu.lastSocialId)
      {
        MelonLogger.Msg("User changed, reconnecting WebSocket.");
        BehindMenu.StopStatusSocket();
        yield return (object) new WaitForSecondsRealtime(1f);
        BehindMenu.StartStatusSocket();
        break;
      }
      BehindMenu.socket = new ClientWebSocket();
      BehindMenu.socket.Options.SetRequestHeader("Authorization", "Bearer " + Accounts.GetAccessToken());
      System.Uri uri = new System.Uri("ws://184.72.34.141/ws/getstatus");
      Task connectTask = BehindMenu.socket.ConnectAsync(uri, CancellationToken.None);
      while (!connectTask.IsCompleted)
        yield return (object) null;
      if (BehindMenu.socket.State != WebSocketState.Open)
      {
        MelonLogger.Error("WebSocket failed to connect.");
        yield return (object) new WaitForSecondsRealtime(3f);
      }
      else
      {
        BehindMenu.isConnected = true;
        object send = MelonCoroutines.Start(BehindMenu.SendRoomUpdatesCoroutine());
        object recv = MelonCoroutines.Start(BehindMenu.ReceiveLoopCoroutine());
        while (BehindMenu.socket.State == WebSocketState.Open && BehindMenu.shouldRun && !(((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel.CurrentUser).Profile).SocialId != BehindMenu.lastSocialId))
          yield return (object) null;
        MelonLogger.Msg("WebSocket disconnected or shutting down.");
        BehindMenu.socket.Abort();
        BehindMenu.socket.Dispose();
        BehindMenu.socket = (ClientWebSocket) null;
        BehindMenu.isConnected = false;
        MelonCoroutines.Stop(send);
        MelonCoroutines.Stop(recv);
        yield return (object) new WaitForSecondsRealtime(3f);
        uri = (System.Uri) null;
        connectTask = (Task) null;
        send = (object) null;
        recv = (object) null;
      }
    }
  }

  private static IEnumerator SendRoomUpdatesCoroutine()
  {
    while (BehindMenu.shouldRun && BehindMenu.socket != null && BehindMenu.socket.State == WebSocketState.Open)
    {
      if (string.IsNullOrEmpty(((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel.CurrentUser).Profile).SocialId))
      {
        MelonLogger.Msg("No user social ID, aborting socket.");
        BehindMenu.socket.Abort();
        break;
      }
      string room = BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.RoomId;
      bool imAdmin = BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.IsLocalUserAdmin;
      BehindMenu.lastRoom = room;
      BehindMenu.lastImAdmin = imAdmin;
      string json = $"{{\"Room\":\"{room}\",\"ImAdmin\":{imAdmin.ToString().ToLower()}}}";
      byte[] buffer = Encoding.UTF8.GetBytes(json);
      Task sendTask = BehindMenu.socket.SendAsync(new System.ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
      while (!sendTask.IsCompleted)
        yield return (object) null;
      json = (string) null;
      buffer = (byte[]) null;
      sendTask = (Task) null;
      yield return (object) new WaitForSecondsRealtime(2f);
      room = (string) null;
    }
  }

  private static IEnumerator ReceiveLoopCoroutine()
  {
    byte[] buffer = new byte[4096 /*0x1000*/];
    while (BehindMenu.shouldRun && BehindMenu.socket != null && BehindMenu.socket.State == WebSocketState.Open)
    {
      Task<WebSocketReceiveResult> receiveTask = BehindMenu.socket.ReceiveAsync(new System.ArraySegment<byte>(buffer), CancellationToken.None);
      while (!receiveTask.IsCompleted)
        yield return (object) null;
      if (receiveTask.Result.MessageType == WebSocketMessageType.Close)
      {
        MelonLogger.Msg("WebSocket closed by server.");
        break;
      }
      string json = Encoding.UTF8.GetString(buffer, 0, receiveTask.Result.Count);
      BehindMenu.StatusUpdate status = (BehindMenu.StatusUpdate) null;
      try
      {
        status = JsonConvert.DeserializeObject<BehindMenu.StatusUpdate>(json);
      }
      catch (System.Exception ex)
      {
        MelonLogger.Error("Error parsing WebSocket status: " + ex.Message);
        continue;
      }
      if (status != null)
      {
        if (status.MessageCount > BehindMenu.msgCount)
          Utils.FloatingNotification("You have new messages!", duration: 4f, icon: "f27a");
        BehindMenu.msgCount = status.MessageCount;
        BehindMenu.badgeNumber.text = BehindMenu.msgCount.ToString();
        BehindMenu.clonedBadge.SetActive(BehindMenu.msgCount > 0);
        RoomModeration.isMod = status.IsMod;
        if (BehindMenu.lastImAdmin && !string.IsNullOrEmpty(status.KickTarget))
        {
          MelonLogger.Msg($"Kicking user {status.KickTarget} from room {BehindMenu.lastRoom}");
          RemoteUser remoteUser = new RemoteUser();
          ((RoomUser) remoteUser).UserSessionId = status.KickTarget;
          Api.KickUserFromRoom(remoteUser, Api.cloudConnectionCTS.Token);
        }
      }
      receiveTask = (Task<WebSocketReceiveResult>) null;
      json = (string) null;
      status = (BehindMenu.StatusUpdate) null;
    }
  }

  private static IEnumerator GetLastMessage()
  {
    string socialID = ((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel?.CurrentUser)?.Profile).SocialId;
    UnityWebRequest www = UnityWebRequest.Get("https://chat.bigscreenfriends.com/messages/get?socialid=" + socialID);
    www.SetRequestHeader("Authorization", "Bearer " + Accounts.GetAccessToken());
    www.SendWebRequest();
    while (!www.isDone)
      yield return (object) null;
    if (!www.isHttpError)
    {
      if (www.result == UnityWebRequest.Result.Success)
      {
        string jsonData = www.downloadHandler.text;
        if (jsonData == "No messages")
        {
          Utils.UiMessage("No messages found.");
          yield break;
        }
        JSONNode json = JSON.Parse(jsonData);
        if (JSONNode.op_Inequality(json, (Il2CppSystem.Object) null))
        {
          string sender = JSONNode.op_Implicit(json["sender"]);
          string text = JSONNode.op_Implicit(json["text"]);
          UserProfileFetcher fetcher = new UserProfileFetcher(Accounts.GetAccessToken(), CloudConfig.BIGSCREEN_API_KEY, CloudConfig.BIGSCREEN_CLOUD_API_URL, sender);
          UserProfileFetcher.UserProfile profile = (UserProfileFetcher.UserProfile) null;
          yield return (object) fetcher.GetUserProfileCoroutine((System.Action<UserProfileFetcher.UserProfile>) (p => profile = p));
          FriendInvitationPopup popup = ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GetPage((BigUIState) 208 /*0xD0*/))?.gameObject.GetComponent<FriendInvitationPopup>();
          int startIndex = 0;
          string fullText = "sent you a message: " + text;
          List<string> pages = new List<string>();
          int currentPage = 0;
          RemoteSocialProfile socialprofile = new RemoteSocialProfile();
          ((SocialProfile) socialprofile).Username = profile != null ? profile.Username : "";
          ((SocialProfile) socialprofile).SocialId = profile != null ? profile.SocialId : sender;
          popup.SocialProfile = socialprofile;
          popup.SetupPopupActions((Il2CppSystem.Action) new System.Action(ScrollDown), (Il2CppSystem.Action) new System.Action(ScrollUp));
          TextMeshPro textTMPro = popup.messageBody;
          textTMPro.fontSize = 0.26f;
          textTMPro.enableWordWrapping = true;
          textTMPro.overflowMode = TextOverflowModes.Overflow;
          SplitMessageIntoPages(fullText, 200);
          UpdatePopupText();
          fullText = (string) null;
          socialprofile = (RemoteSocialProfile) null;
          textTMPro = (TextMeshPro) null;
          sender = (string) null;
          text = (string) null;
          fetcher = (UserProfileFetcher) null;

          void UpdatePopupText()
          {
            if (currentPage >= 0 && currentPage < pages.Count)
              popup.SetPopupText(profile != null ? profile.Username : "User", pages[currentPage], "SCROLL DOWN", "SCROLL UP");
            ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GoToPage((BigUIState) 208 /*0xD0*/, false, true, false);
          }

          void SplitMessageIntoPages(string message, int maxCharsPerPage)
          {
            pages.Clear();
            string[] strArray = message.Split(' ');
            string str1 = "";
            foreach (string str2 in strArray)
            {
              if (str1.Length + str2.Length + 1 <= maxCharsPerPage)
              {
                str1 = str1 + (str1.Length > 0 ? " " : "") + str2;
              }
              else
              {
                pages.Add(str1);
                str1 = str2;
              }
            }
            if (string.IsNullOrWhiteSpace(str1))
              return;
            pages.Add(str1);
          }

          void ScrollDown()
          {
            if (currentPage < pages.Count - 1)
            {
              ++currentPage;
              UpdatePopupText();
            }
            else
              ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GoToPage((BigUIState) 208 /*0xD0*/, false, true, false);
          }

          void ScrollUp()
          {
            if (currentPage > 0)
            {
              --currentPage;
              UpdatePopupText();
            }
            else
              ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GoToPage((BigUIState) 208 /*0xD0*/, false, true, false);
          }
        }
        else
          Utils.UiMessage("Failed to parse message.");
        json = (JSONNode) null;
        jsonData = (string) null;
      }
      else
      {
        MelonLogger.Error("Error getting messages: " + www.error);
        Utils.UiMessage("Failed to get messages.\n" + www.error);
      }
      www.Dispose();
      yield return (object) null;
    }
  }

  [HarmonyPatch(typeof (BigUI), "BackgroundClicked")]
  private static class BigUI_EndState
  {
    private static bool Prefix(BigUI __instance) => __instance.currentScreenState != 208 /*0xD0*/;
  }

  [Serializable]
  public class StatusUpdate
  {
    public int MessageCount;
    public bool IsMod;
    public string KickTarget;
  }
}
