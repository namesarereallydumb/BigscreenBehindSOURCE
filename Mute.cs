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
