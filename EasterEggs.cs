using Il2CppBigscreen.Helpers;
using Il2CppBigscreen.Tools;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Collections;
using UnityEngine;

#nullable enable
namespace BigscreenBehind;

internal class EasterEggs
{
  public static void ToggleSceneRoot()
  {
    ((Component) BigSceneHierarchy.sceneRoot).gameObject.active = !((Component) BigSceneHierarchy.sceneRoot).gameObject.active;
  }

  public static IEnumerator SetAllPossibileMoveables()
  {
    Utils.FloatingNotification("Easter Egg Unlocked", duration: 4f);
    Transform movTransform = GameObject.Find("SceneRoot")?.transform;
    if (!((Object) movTransform == (Object) null))
    {
      Il2CppArrayBase<Transform> moveablesList = movTransform.GetComponentsInChildren<Transform>();
      foreach (Transform moveable in moveablesList)
      {
        if ((Object) moveable.GetComponent<MeshFilter>() != (Object) null)
        {
          MeshFilter meshFilter = moveable.GetComponent<MeshFilter>();
          if ((Object) meshFilter != (Object) null && (Object) meshFilter.sharedMesh != (Object) null)
          {
            Mesh originalMesh = meshFilter.sharedMesh;
            if (originalMesh.isReadable)
              originalMesh = (Mesh) null;
            else
              continue;
          }
          GameObject ga = moveable.gameObject;
          if ((Object) ga.GetComponent<Collider>() == (Object) null)
            ga.AddComponent<MeshCollider>();
          SimpleGraspable sg = ga.AddComponent<SimpleGraspable>();
          sg.targetTransform = moveable;
          ga.layer = 28;
          meshFilter = (MeshFilter) null;
          ga = (GameObject) null;
          sg = (SimpleGraspable) null;
        }
      }
      yield return (object) null;
    }
  }
}
