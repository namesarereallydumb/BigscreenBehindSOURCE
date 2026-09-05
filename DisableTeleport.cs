// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.DisableTeleport
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using Il2CppBigscreen;
using MelonLoader;
using System.Collections;
using UnityEngine;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("DisableTeleport", "1.0.0", "Love")]
public class DisableTeleport : MelonMod
{
  public override void OnLateInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} loaded!\nMade with LOVE\n=========================\n");
  }

  public override void OnSceneWasInitialized(int buildIndex, string sceneName)
  {
    if (!(sceneName == "Master"))
      return;
    MelonCoroutines.Start(this.DisableTeleportCoroutine());
  }

  private IEnumerator DisableTeleportCoroutine()
  {
    while ((Object) BIG_STATIC_SINGLETONS.bigSeatSelectionUI == (Object) null)
      yield return (object) null;
    ((Component) BIG_STATIC_SINGLETONS.bigSeatSelectionUI).gameObject.SetActive(false);
    yield return (object) null;
  }
}
