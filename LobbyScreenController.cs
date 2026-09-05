// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.LobbyScreenController
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using Il2CppBigscreen;
using Il2CppBigscreen.Monitors.Presentation;
using Il2CppBigscreen.UI;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("LobbyScreen", "1.0.6", "Love")]
public class LobbyScreenController : MelonMod
{
  private bool master = false;
  private bool lastRoom = false;
  private GameObject screen;

  public override void OnLateInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} loaded!\nMade with LOVE\n=========================\n");
    MelonCoroutines.Start(this.LaunchEverySeconds());
  }

  public override void OnSceneWasInitialized(int buildIndex, string sceneName)
  {
    if (!(sceneName == "Master"))
      return;
    MelonCoroutines.Start(this.InstantiateScreen());
    this.master = true;
  }

  private IEnumerator LaunchEverySeconds()
  {
    while (true)
    {
      MelonCoroutines.Start(this.CheckStatus());
      yield return (object) new WaitForSeconds(12f);
    }
  }

  private IEnumerator CheckStatus()
  {
    if (this.master)
    {
      bool mpLobby = this.IsMPLobby();
      if (mpLobby != this.lastRoom)
      {
        if (mpLobby)
        {
          this.lastRoom = mpLobby;
          this.ShowScreen(true);
          this.SetTransform();
          this.ShowOthers(false);
          MelonCoroutines.Start(this.RefreshAudio());
        }
        else
        {
          this.ShowScreen(false);
          this.ShowOthers(true);
          this.lastRoom = mpLobby;
        }
        yield break;
      }
    }
  }

  private IEnumerator RefreshAudio()
  {
    yield return (object) new WaitForSeconds(1f);
    GameObject ui = GameObject.Find("UI/TabletUI");
    ((BigUI) ui.GetComponent<TabletUI>()).GoToScreen(5);
    while ((UnityEngine.Object) GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Center/MonitorAdjustment(Clone)") == (UnityEngine.Object) null)
      yield return (object) null;
    GameObject mu = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Center/MonitorAdjustment(Clone)");
    mu.GetComponent<MonitorAdjustmentPage>().RefreshAudio();
    ui.GetComponent<TabletUI>().GoToHomePage();
  }

  private void SetTransform()
  {
    PresentationSizer component = GameObject.Find("UI/PresentationScreen(Clone)(Clone)").GetComponent<PresentationSizer>();
    Transform transform = ((IEnumerable<GameObject>) (GameObject[]) Resources.FindObjectsOfTypeAll<GameObject>()).Where<GameObject>((Func<GameObject, bool>) (go => go.name == "PresentationScreen(Clone)")).ToArray<GameObject>()[0].transform;
    component.SetTransform(transform);
    Vector3 position = new Vector3(0.0f, 16.83f, -13.41f);
    Quaternion rotation = Quaternion.Euler(333.7999f, 180f, 0.0f);
    ((Component) component).transform.SetPositionAndRotation(position, rotation);
  }

  private void ShowOthers(bool v)
  {
    Transform transform1 = GameObject.Find("UI")?.transform?.Find("PresentationUI");
    Transform transform2 = GameObject.Find("BigContentController(Clone)")?.transform?.Find("UI");
    transform1?.gameObject.SetActive(v);
    transform2?.gameObject.SetActive(v);
  }

  private void ShowScreen(bool v) => this.screen.SetActive(v);

  private IEnumerator InstantiateScreen()
  {
    yield return (object) new WaitForSeconds(10f);
    while ((UnityEngine.Object) GameObject.Find("PresentationScreen(Clone)") == (UnityEngine.Object) null)
      yield return (object) null;
    GameObject uiObject = GameObject.Find("UI");
    GameObject presentationScreenPrefab = GameObject.Find("PresentationScreen(Clone)");
    GameObject presentationScreenInstance = UnityEngine.Object.Instantiate<GameObject>(presentationScreenPrefab, uiObject.transform);
    this.screen = presentationScreenInstance;
    presentationScreenInstance.SetActive(false);
  }

  private bool IsMPLobby() => BIG_STATIC_SINGLETONS.currentApp.CurrentRoom.InLobby();
}
