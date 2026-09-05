// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.WaveEasterEgg
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using Il2CppBigscreen.Hands;
using MelonLoader;
using System.Collections;
using UnityEngine;

#nullable enable
namespace BigscreenBehind;

public static class WaveEasterEgg
{
  public static Transform rightHand;
  public static Transform leftHand;
  public static Transform head;
  private static bool isWaving;

  public static void Wave(Handedness hand)
  {
    MelonCoroutines.Start(WaveEasterEgg.PerformWave(hand));
  }

  private static IEnumerator PerformWave(Handedness hand)
  {
    WaveEasterEgg.isWaving = true;
    Transform handTransform = hand == 1 ? WaveEasterEgg.rightHand : WaveEasterEgg.leftHand;
    Vector3 originalLocalPos = handTransform.localPosition;
    Quaternion originalLocalRot = handTransform.localRotation;
    float totalDuration = 2f;
    float waveSpeed = 8f;
    float elapsed = 0.0f;
    while ((double) elapsed < (double) totalDuration)
    {
      elapsed += Time.deltaTime;
      float progress = elapsed / totalDuration;
      Vector3 currentHeadRight = WaveEasterEgg.head.right;
      Vector3 currentHeadUp = WaveEasterEgg.head.up;
      Vector3 currentHeadForward = WaveEasterEgg.head.forward;
      if (hand == 0)
        currentHeadRight *= -1f;
      Vector3 currentBaseOffset = currentHeadRight * 0.3f + currentHeadUp * -0.1f + currentHeadForward * 0.2f;
      float verticalWave = Mathf.Sin((float) ((double) elapsed * (double) waveSpeed * 3.1415927410125732)) * 0.08f;
      float horizontalWave = Mathf.Sin((float) ((double) elapsed * (double) waveSpeed * 3.1415927410125732 * 0.699999988079071)) * 0.1f;
      Vector3 waveOffset = currentHeadUp * verticalWave + currentHeadRight * horizontalWave;
      Vector3 targetPos = WaveEasterEgg.head.position + currentBaseOffset + waveOffset;
      Quaternion targetRot = originalLocalRot * Quaternion.Euler(0.0f, 90f, 180f);
      float wristWave = Mathf.Sin((float) ((double) elapsed * (double) waveSpeed * 3.1415927410125732)) * 15f;
      targetRot *= Quaternion.Euler(0.0f, 0.0f, wristWave);
      float easedProgress = Mathf.SmoothStep(0.0f, 1f, progress);
      float intensity = Mathf.Sin(easedProgress * 3.14159274f);
      handTransform.position = Vector3.Lerp(handTransform.position, targetPos, intensity);
      handTransform.rotation = Quaternion.Slerp(originalLocalRot, targetRot, intensity);
      yield return (object) null;
    }
    WaveEasterEgg.isWaving = false;
  }
}
