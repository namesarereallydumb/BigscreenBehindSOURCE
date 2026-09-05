// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.Mute
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using MelonLoader;
using UnityEngine;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("MuteEnvironmentAudio", "1.0.0", "Love")]
public class Mute : MelonMod
{
  public override void OnLateInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} loaded!\nMade with LOVE\n=========================\n");
  }

  public override void OnSceneWasInitialized(int buildIndex, string sceneName) => this.MuteAudio();

  private void MuteAudio()
  {
    Transform transform1 = GameObject.Find("SceneRoot")?.transform?.Find("AUDIO");
    Transform transform2 = GameObject.Find("SceneRoot")?.transform?.Find("Audio");
    if ((Object) transform1 != (Object) null)
    {
      transform1.gameObject.SetActive(false);
      this.LoggerInstance.Msg("Audio muted");
    }
    else if ((Object) transform2 != (Object) null)
    {
      transform2.gameObject.SetActive(false);
      this.LoggerInstance.Msg("Audio muted");
    }
    else
      this.LoggerInstance.Msg("No audio to mute.");
  }
}
