using MelonLoader;
using System.Collections.Generic;
using UnityEngine;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("LobbyPropsRemover", "1.0.0", "Love")]
public class LobbyPropsRemover : MelonMod
{
  public override void OnLateInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} loaded!\nMade with LOVE\n=========================\n");
  }

  public override void OnSceneWasLoaded(int buildIndex, string sceneName)
  {
    if (!(sceneName == "GrandLobby"))
      return;
    this.ApplyMod();
  }

  private void ApplyMod()
  {
    Transform transform1 = GameObject.Find("SceneRoot")?.transform?.Find("Props");
    List<Transform> transformList = new List<Transform>();
    for (int index = 0; index < 10; ++index)
    {
      string str;
      if (index != 0)
        str = $"Pedestal ({index})";
      else
        str = "Pedestal";
      string name = str;
      Transform transform2 = GameObject.Find(name)?.transform;
      if ((Object) transform2 != (Object) null)
        transformList.Add(transform2);
      else
        this.LoggerInstance.Msg($"Pedestal {name} not found in the scene!");
    }
    foreach (Component component in transformList)
      component.gameObject.SetActive(false);
    if ((Object) transform1 != (Object) null)
      Object.Destroy((Object) transform1.gameObject);
    else
      this.LoggerInstance.Msg("Object not found in the scene!");
  }
}
