// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.Patches.AutoJoinPatch
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using HarmonyLib;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.UI;
using MelonLoader;
using System.Collections;
using UnityEngine;

#nullable enable
namespace BigscreenBehind.Patches;

internal class AutoJoinPatch
{
  private static bool autoJoinInProgress;
  private static object? autoJoinCorutine;

  public static void JoinRoomAction(object obj)
  {
    if (AutoJoinPatch.autoJoinCorutine != null)
    {
      RoomPreviewPopup roomprevbutton = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Popups/Popup_RoomPreview(Clone)").GetComponent<RoomPreviewPopup>();
      ((BigUIPage) roomprevbutton).Hide();
      Utils.popupOptionsMessage("Auto Join is already in progress. Do you want to stop it?", (System.Action) (() =>
      {
        MelonCoroutines.Stop(AutoJoinPatch.autoJoinCorutine);
        GameObject gameObject = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Popups/Popup_RoomPreview(Clone)/Content/Footer/Btn_Cloned");
        gameObject.GetComponent<BigUIButton>().Visualization.SetColors(new Color(0.3115f, 0.8137f, 0.5283f, 1f), Color.black);
        gameObject.GetComponent<BigUIButton>().Visualization.SetupMeshes();
        AutoJoinPatch.autoJoinCorutine = (object) null;
        AutoJoinPatch.autoJoinInProgress = false;
        ((BigUIPage) roomprevbutton).Show();
      }), cancelButtonText: "No", cancelAction: (System.Action) (() => ((BigUIPage) roomprevbutton).Show()), headerText: "Auto Join in progress");
    }
    else
      AutoJoinPatch.autoJoinCorutine = MelonCoroutines.Start(AutoJoinPatch.AutoJoinRoomCoroutine());
  }

  private static IEnumerator AutoJoinRoomCoroutine()
  {
    GameObject button = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Popups/Popup_RoomPreview(Clone)/Content/Footer/Btn_Cloned");
    button.GetComponent<BigUIButton>().Visualization.SetColors(new Color(1f, 0.64f, 0.0f), Color.black);
    button.GetComponent<BigUIButton>().Visualization.SetupMeshes();
    string roomID = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Popups/Popup_RoomPreview(Clone)").GetComponent<RoomPreviewPopup>().roomId;
    bool isLobby = false;
    AutoJoinRoom fetcher = new AutoJoinRoom(Accounts.GetAccessToken(), CloudConfig.BIGSCREEN_API_KEY, CloudConfig.BIGSCREEN_CLOUD_API_URL, roomID);
    AutoJoinRoom.Room room = (AutoJoinRoom.Room) null;
    long statusCode = 200;
    bool isFull = true;
    try
    {
      while (isFull)
      {
        yield return (object) fetcher.GetRoomCoroutine((System.Action<AutoJoinRoom.Room>) (r => room = r), (System.Action<long>) (code => statusCode = code));
        if (statusCode == 500L)
          yield break;
        if (room != null)
        {
          int size = room.size;
          int? count = room.remoteUsers?.Count;
          int valueOrDefault = count.GetValueOrDefault();
          isFull = size <= valueOrDefault & count.HasValue;
        }
        else
          isFull = true;
        if (isFull)
          yield return (object) new WaitForSeconds(0.5f);
      }
      if (room != null && room.name == "Lobby" && room.description == "Bigscreen Lobby")
      {
        Api.JoinLobby();
      }
      else
      {
        JoinRoomOptions jro = new JoinRoomOptions();
        jro.RoomId = roomID;
        Api.JoinRoom(jro, Api.cloudConnectionCTS.Token);
        jro = (JoinRoomOptions) null;
      }
    }
    finally
    {
      button.GetComponent<BigUIButton>().Visualization.SetColors(new Color(0.3115f, 0.8137f, 0.5283f, 1f), Color.black);
      button.GetComponent<BigUIButton>().Visualization.SetupMeshes();
      AutoJoinPatch.autoJoinCorutine = (object) null;
      AutoJoinPatch.autoJoinInProgress = false;
    }
  }

  [HarmonyPatch(typeof (RoomPreviewPopup), "Show")]
  private static class RoomPreviewPopup_OnShowComplete
  {
    private static void Postfix(RoomPreviewPopup __instance)
    {
      Transform transform = ((Component) __instance).transform.Find("Content/Footer/Btn");
      if ((bool) (UnityEngine.Object) transform.parent.Find("Btn_Cloned"))
        return;
      GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(transform.gameObject, transform.parent);
      gameObject.name = "Btn_Cloned";
      gameObject.transform.localPosition = new Vector3(-0.1258f, 0.2514f, 0.0f);
      gameObject.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
      gameObject.transform.localScale = new Vector3(0.959f, 0.959f, 0.961f);
      gameObject.gameObject.GetComponent<BigUIButton>().Visualization.ButtonText.text = "AUTO JOIN (IF FULL)";
      gameObject.gameObject.GetComponent<BigUIButton>().Visualization.ButtonText.fontSize = 0.125f;
      gameObject.transform.localScale = new Vector3(0.959f, 0.959f, 0.961f);
      gameObject.gameObject.GetComponent<BigUIButton>().Visualization.SetColors(new Color(0.3115f, 0.8137f, 0.5283f, 1f), Color.black);
      gameObject.gameObject.GetComponent<BigUIButton>().Visualization.buttonText.color = Color.black;
      gameObject.gameObject.GetComponent<BigUIButton>().Visualization.SetupMeshes();
      Utils.ReplaceButtonEvent(gameObject.gameObject.GetComponent<BigUIButton>().OnPoked, new System.Action<Il2CppSystem.Object>(AutoJoinPatch.JoinRoomAction));
    }
  }
}
