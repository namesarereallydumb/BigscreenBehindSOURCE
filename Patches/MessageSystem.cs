// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.Patches.MessageSystem
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using HarmonyLib;
using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.UI;
using Il2CppTMPro;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

#nullable enable
namespace BigscreenBehind.Patches;

internal class MessageSystem
{
  private static bool isBehindUser = true;
  public const string msgURL = "https://chat.bigscreenfriends.com/messages";
  private const int MAXIMUM_CHARS = 2000;

  private static void SendMessage(string socialId, string noteText)
  {
    MelonCoroutines.Start(MessageSystem.SendMessageCoroutine(((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel?.CurrentUser)?.Profile).SocialId, socialId, noteText));
  }

  private static IEnumerator SendMessageCoroutine(
    string? senderSocialId,
    string targetSocialId,
    string noteText)
  {
    UnityWebRequest www = UnityWebRequest.Get($"{"https://chat.bigscreenfriends.com/messages"}/send?sender={senderSocialId}&targetuser={targetSocialId}&text={noteText}");
    www.SetRequestHeader("Authorization", "Bearer " + Accounts.GetAccessToken());
    www.SendWebRequest();
    while (!www.isDone)
      yield return (object) null;
    if (!www.isHttpError)
    {
      if (www.result == UnityWebRequest.Result.Success)
      {
        string jsonData = www.downloadHandler.text;
        Utils.UiMessage("Message sent successfully.");
        jsonData = (string) null;
      }
      else
      {
        MelonLogger.Error("Error sending message: " + www.error);
        Utils.UiMessage("Failed to send message.\n" + www.error);
      }
      www.Dispose();
      yield return (object) null;
    }
  }

  [HarmonyPatch(typeof (UserProfile), "Show")]
  private static class UserProfile_Show
  {
    private static void Postfix(UserProfile __instance)
    {
      GameObject gameObject1 = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Bottom/ControlStrip(Clone)/Content/ToolbarRecenter/Btn (6)");
      Transform transform = ((Component) __instance).transform.Find("Content/Body");
      if ((bool) (UnityEngine.Object) transform.transform.Find("Btn_Message"))
        return;
      GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject1.gameObject, transform.transform);
      gameObject2.name = "Btn_Message";
      gameObject2.transform.localPosition = new Vector3(-0.26f, 0.117f, 0.0f);
      gameObject2.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
      gameObject2.transform.localScale = new Vector3(0.7f, 0.7f, 0.01f);
      gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetColors(new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f), new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f));
      Transform child = gameObject2.transform.GetChild(0);
      for (int index = 1; index < child.childCount; ++index)
        child.GetChild(index).gameObject.SetActive(false);
      child.transform.GetChild(0).GetComponent<TextMeshPro>().text = "\uF27A";
      gameObject2.transform.GetChild(1).localPosition += new Vector3(0.0f, 0.03f, 0.0f);
      gameObject2.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Send Message";
      gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetupMeshes();
      Utils.ReplaceButtonEvent(gameObject2.gameObject.GetComponent<BigUIButton>().OnPoked, new System.Action<Il2CppSystem.Object>(MessageSystem.UserProfile_Show.OpenUserNote));
    }

    private static void OpenUserNote(Il2CppSystem.Object @object)
    {
      UserProfile component = ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI)?.GetPage((BigUIState) 27))?.gameObject.GetComponent<UserProfile>();
      if (string.IsNullOrEmpty(((SocialProfile) component?.SocialProfile).SocialId))
        return;
      string socialId = ((SocialProfile) component.SocialProfile).SocialId;
      if (component.SocialProfile.SocialGraphType != 2)
        Utils.FloatingNotification("Not a friend", duration: 1f, icon: "f27a");
      else if (!MessageSystem.isBehindUser)
      {
        Utils.FloatingNotification("Not a Behind user", duration: 1f, icon: "f27a");
      }
      else
      {
        InputFieldPopup InputFieldPopupNote = ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI)?.GetPage((BigUIState) 85))?.gameObject.GetComponent<InputFieldPopup>();
        InputFieldPopupNote.PageTitle = "Send Message";
        InputFieldPopupNote.changeBtn.Visualization.SetButtonText("SAVE/SEND");
        InputFieldPopupNote.KeypadStyle = (KeypadStyle) 5;
        InputFieldPopupNote.InputFieldText = string.Empty;
        InputFieldPopupNote.ForbiddenChars = "";
        InputFieldPopupNote.InputField.lineType = (BigInputField.LineType) 2;
        ((Component) InputFieldPopupNote.InputField).transform.localScale = new Vector3(0.0015f, 1f / 400f, 0.0015f);
        InputFieldPopupNote.InputField.textComponent.fontSize = 14;
        InputFieldPopupNote.InputField.placeholder.TryCast<Text>().text = "Enter your message here...";
        InputFieldPopupNote.OnChangeConfirmed = (UnityAction<string>) (System.Action<string>) (noteText =>
        {
          if (string.IsNullOrEmpty(noteText))
          {
            ((BigUIPage) InputFieldPopupNote).EndState();
            Utils.UiMessage("Message cannot be empty.", stateOnDismissed: (BigUIState) 27);
          }
          else if (noteText.Length > 2000)
          {
            ((BigUIPage) InputFieldPopupNote).EndState();
            Utils.UiMessage($"Message is too long. Maximum {2000} characters.", stateOnDismissed: (BigUIState) 27);
          }
          else
          {
            MessageSystem.SendMessage(socialId, noteText);
            ((BigUIPage) InputFieldPopupNote).EndState();
          }
        });
        ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GoToPage((BigUIState) 85, false, true, false);
        ((BigUIPage) component).EndState();
      }
    }
  }

  [HarmonyPatch(typeof (UserProfile_List), "Show")]
  private static class UserProfile_List_Show
  {
    private static void Postfix(UserProfile_List __instance)
    {
      GameObject gameObject1 = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Bottom/ControlStrip(Clone)/Content/ToolbarRecenter/Btn (6)");
      Transform transform = ((Component) __instance).transform.Find("Content/Body");
      if ((bool) (UnityEngine.Object) transform.transform.Find("Btn_Message"))
        return;
      GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject1.gameObject, transform.transform);
      gameObject2.name = "Btn_Message";
      gameObject2.transform.localPosition = new Vector3(-0.295f, 0.16f, 0.0f);
      gameObject2.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
      gameObject2.transform.localScale = new Vector3(0.7f, 0.7f, 0.01f);
      gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetColors(new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f), new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f));
      Transform child = gameObject2.transform.GetChild(0);
      for (int index = 1; index < child.childCount; ++index)
        child.GetChild(index).gameObject.SetActive(false);
      child.transform.GetChild(0).GetComponent<TextMeshPro>().text = "\uF27A";
      gameObject2.transform.GetChild(1).localPosition += new Vector3(0.0f, 0.03f, 0.0f);
      gameObject2.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Send Message";
      gameObject2.transform.GetChild(1).localPosition += new Vector3(0.0f, 0.025f, 0.0f);
      gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetupMeshes();
      Utils.ReplaceButtonEvent(gameObject2.gameObject.GetComponent<BigUIButton>().OnPoked, new System.Action<Il2CppSystem.Object>(MessageSystem.UserProfile_List_Show.OpenUserNote));
    }

    private static void OpenUserNote(Il2CppSystem.Object @object)
    {
      UserProfile_List component = ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI)?.GetPage((BigUIState) 207))?.gameObject.GetComponent<UserProfile_List>();
      if (string.IsNullOrEmpty(((SocialProfile) component?.SocialProfile).SocialId))
        return;
      string socialId = ((SocialProfile) component.SocialProfile).SocialId;
      if (component.SocialProfile.SocialGraphType != 2)
        Utils.FloatingNotification("Not a friend", duration: 1f, icon: "f27a");
      else if (!MessageSystem.isBehindUser)
      {
        Utils.FloatingNotification("Not a Behind user", duration: 1f, icon: "f27a");
      }
      else
      {
        InputFieldPopup InputFieldPopupNote = ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI)?.GetPage((BigUIState) 85))?.gameObject.GetComponent<InputFieldPopup>();
        InputFieldPopupNote.PageTitle = "Send Message";
        InputFieldPopupNote.changeBtn.Visualization.SetButtonText("SAVE/SEND");
        InputFieldPopupNote.KeypadStyle = (KeypadStyle) 5;
        InputFieldPopupNote.InputFieldText = string.Empty;
        InputFieldPopupNote.ForbiddenChars = "";
        InputFieldPopupNote.InputField.lineType = (BigInputField.LineType) 2;
        ((Component) InputFieldPopupNote.InputField).transform.localScale = new Vector3(0.0015f, 1f / 400f, 0.0015f);
        InputFieldPopupNote.InputField.textComponent.fontSize = 14;
        InputFieldPopupNote.InputField.placeholder.TryCast<Text>().text = "Enter your message here...";
        InputFieldPopupNote.OnChangeConfirmed = (UnityAction<string>) (System.Action<string>) (noteText =>
        {
          if (string.IsNullOrEmpty(noteText))
          {
            ((BigUIPage) InputFieldPopupNote).EndState();
            Utils.UiMessage("Message cannot be empty.", stateOnDismissed: (BigUIState) 207);
          }
          else if (noteText.Length > 2000)
          {
            ((BigUIPage) InputFieldPopupNote).EndState();
            Utils.UiMessage($"Message is too long. Maximum {2000} characters.", stateOnDismissed: (BigUIState) 207);
          }
          else
          {
            MessageSystem.SendMessage(socialId, noteText);
            ((BigUIPage) InputFieldPopupNote).EndState();
          }
        });
        ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GoToPage((BigUIState) 85, false, true, false);
        ((BigUIPage) component).EndState();
      }
    }
  }
}
