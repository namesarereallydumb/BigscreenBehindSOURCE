using Il2Cpp;
using Il2CppBigscreen;
using MelonLoader;
using System.Collections;
using UnityEngine;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("DisableSnapTurn", "1.0.0", "Love")]
public class DisableSnapTurn : MelonMod
{
  public override void OnLateInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} loaded!\nMade with LOVE\n=========================\n");
  }

  public override void OnSceneWasInitialized(int buildIndex, string sceneName)
  {
    if (!(sceneName == "Master"))
      return;
    MelonCoroutines.Start(this.DisableSnapTurnCoroutine());
  }

  private IEnumerator DisableSnapTurnCoroutine()
  {
    while ((Object) BIG_STATIC_SINGLETONS.bigUserGameObject == (Object) null)
      yield return (object) null;
    ((Behaviour) BIG_STATIC_SINGLETONS.bigUserGameObject.GetComponent<SnapRotation>()).enabled = false;
    yield return (object) null;
  }
}
