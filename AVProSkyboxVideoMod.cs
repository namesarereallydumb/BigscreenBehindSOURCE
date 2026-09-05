// Decompiled with JetBrains decompiler
// Type: AVProSkyboxVideoMod
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using Il2CppBigscreen.Media;
using Il2CppRenderHeads.Media.AVProVideo;
using Il2CppSystem.Runtime.CompilerServices;
using Il2CppSystem.Threading.Tasks;
using MelonLoader;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Video;

#nullable enable
public class AVProSkyboxVideoMod
{
  private static MediaPlayer mediaPlayer;
  private static VideoPlayer videoPlayer;
  private static bool isPrepared;

  public static void Play(string videoPageUrl)
  {
    MelonCoroutines.Start(AVProSkyboxVideoMod.LoadAndPlayM3U8(Application.streamingAssetsPath + "/video.mp4"));
  }

  private static IEnumerator PlayVideoFromHLS(string videoPageUrl)
  {
    MelonCoroutines.Start(AVProSkyboxVideoMod.ExtractM3U8UrlAsync(videoPageUrl, "960", (System.Action<string>) (m3u8Urlb =>
    {
      if (m3u8Urlb != null)
      {
        MelonCoroutines.Start(AVProSkyboxVideoMod.LoadAndPlayM3U8(m3u8Urlb));
        MelonLogger.Msg("Using M3U8 URL: " + m3u8Urlb);
      }
      else
        MelonLogger.Msg("Failed to retrieve M3U8 URL.");
    })));
    yield return (object) null;
  }

  private static IEnumerator LoadAndPlayM3U8(string hls)
  {
    GameObject mpGO = new GameObject("AVProSkyboxPlayer");
    AVProSkyboxVideoMod.mediaPlayer = mpGO.AddComponent<MediaPlayer>();
    AVProSkyboxVideoMod.mediaPlayer.m_videoMapping = (VideoMapping) 2;
    MelonLogger.Msg("point 1");
    AVProSkyboxVideoMod.isPrepared = false;
    AVProSkyboxVideoMod.mediaPlayer.Events.AddListener((UnityAction<MediaPlayer, MediaPlayerEvent.EventType, ErrorCode>) new System.Action<MediaPlayer, MediaPlayerEvent.EventType, ErrorCode>(AVProSkyboxVideoMod.OnMediaPlayerEvent));
    MelonLogger.Msg("point 2");
    AVProSkyboxVideoMod.mediaPlayer.m_VideoPath = Application.streamingAssetsPath + "/video.mp4";
    AVProSkyboxVideoMod.mediaPlayer.PlatformOptionsWindows.videoApi = (Windows.VideoApi) 0;
    AVProSkyboxVideoMod.mediaPlayer.PlatformOptionsWindows.useHardwareDecoding = true;
    MelonLogger.Msg("point 3");
    AVProSkyboxVideoMod.mediaPlayer.OpenVideoFromFile((MediaPlayer.FileLocation) 0, Application.streamingAssetsPath + "/video.mp4", false);
    AVProSkyboxVideoMod.mediaPlayer.Play();
    AVProSkyboxVideoMod.mediaPlayer.Control.MuteAudio(true);
    MelonLogger.Msg("point 4");
    float timeout = 10f;
    while (!AVProSkyboxVideoMod.isPrepared && (double) timeout > 0.0)
    {
      timeout -= Time.deltaTime;
      yield return (object) null;
    }
    if (!AVProSkyboxVideoMod.isPrepared)
    {
      MelonLogger.Error("Failed to load video: Timeout waiting for FirstFrameReady.");
    }
    else
    {
      MelonLogger.Msg("point 5");
      AssetBundleCreateRequest bundleLoadRequest = AssetBundle.LoadFromFileAsync(Application.streamingAssetsPath + "/skyy");
      yield return (object) bundleLoadRequest;
      AssetBundle myLoadedAssetBundle = bundleLoadRequest.assetBundle;
      if ((UnityEngine.Object) myLoadedAssetBundle == (UnityEngine.Object) null)
      {
        MelonLogger.Error("Failed to load AssetBundle!");
      }
      else
      {
        Material skyboxMat = myLoadedAssetBundle.LoadAsset<Material>("SkyboxMat");
        if ((UnityEngine.Object) skyboxMat == (UnityEngine.Object) null)
        {
          MelonLogger.Error("Material not found in AssetBundle!");
          myLoadedAssetBundle.Unload(false);
        }
        else
        {
          myLoadedAssetBundle.Unload(false);
          MelonLogger.Msg("point 6");
          RenderSettings.skybox = skyboxMat;
          ApplyToMaterial apply = mpGO.AddComponent<ApplyToMaterial>();
          apply._media = AVProSkyboxVideoMod.mediaPlayer;
          apply._material = skyboxMat;
          apply._texturePropertyName = "_MainTex";
        }
      }
    }
  }

  private static IEnumerator LoadAndPlayVideo(string videoUrl)
  {
    GameObject videoPlayerGO = new GameObject("UnitySkyboxVideoPlayer");
    VideoPlayer videoPlayer = videoPlayerGO.AddComponent<VideoPlayer>();
    videoPlayer.source = VideoSource.Url;
    videoPlayer.url = videoUrl;
    videoPlayer.isLooping = true;
    videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
    RenderTexture sourceRT = new RenderTexture(7680, 3840 /*0x0F00*/, 0);
    RenderTexture flippedRT = new RenderTexture(7680, 3840 /*0x0F00*/, 0);
    videoPlayer.targetTexture = sourceRT;
    videoPlayer.Prepare();
    while (!videoPlayer.isPrepared)
      yield return (object) null;
    Graphics.Blit((Texture) sourceRT, flippedRT, new Vector2(1f, -1f), new Vector2(0.0f, 1f));
    AssetBundleCreateRequest bundleLoadRequest = AssetBundle.LoadFromFileAsync(Application.streamingAssetsPath + "/skyy");
    yield return (object) bundleLoadRequest;
    AssetBundle myLoadedAssetBundle = bundleLoadRequest.assetBundle;
    if ((UnityEngine.Object) myLoadedAssetBundle == (UnityEngine.Object) null)
    {
      Debug.LogError((Il2CppSystem.Object) "Failed to load AssetBundle!");
    }
    else
    {
      Material skyboxMat = myLoadedAssetBundle.LoadAsset<Material>("SkyboxMat");
      myLoadedAssetBundle.Unload(false);
      if ((UnityEngine.Object) skyboxMat == (UnityEngine.Object) null)
      {
        Debug.LogError((Il2CppSystem.Object) "Material not found in AssetBundle!");
      }
      else
      {
        skyboxMat.SetTexture("_MainTex", (Texture) flippedRT);
        RenderSettings.skybox = skyboxMat;
        videoPlayer.Play();
        Debug.Log((Il2CppSystem.Object) "Skybox video playback started.");
      }
    }
  }

  private static IEnumerator ExtractM3U8UrlAsync(
    string videoPageUrl,
    string resolution,
    System.Action<string> callback)
  {
    List<string> resolutionMap = new List<string>()
    {
      "1920x960",
      "1280x640",
      "854x428",
      "640x320",
      "426x214",
      "256x128"
    };
    int targetIndex = resolutionMap.FindIndex((System.Predicate<string>) (r => r.Contains(resolution)));
    if (targetIndex == -1)
    {
      MelonLogger.Msg("Unsupported resolution: " + resolution);
      callback((string) null);
    }
    else
    {
      Task<string> fetchTask = BigBrowserHelper.GetYoutubeHLS(videoPageUrl);
      TaskAwaiter<string> awaiter = fetchTask.GetAwaiter();
      while (!awaiter.IsCompleted)
        yield return (object) null;
      if (fetchTask.Status != TaskStatus.RanToCompletion)
      {
        MelonLogger.Error("Failed to fetch hls.");
      }
      else
      {
        string m3u8Url = fetchTask.Result;
        MelonLogger.Msg("Found M3U8 URL: " + m3u8Url);
        yield return (object) new WaitForSeconds(1f);
        UnityWebRequest m3u8Request = UnityWebRequest.Get(m3u8Url);
        yield return (object) m3u8Request.SendWebRequest();
        if (m3u8Request.result == UnityWebRequest.Result.Success)
        {
          string m3u8Content = m3u8Request.downloadHandler.text;
          for (int i = targetIndex; i < resolutionMap.Count; ++i)
          {
            string resolutionPattern = resolutionMap[i];
            Regex resolutionRegex = new Regex($"#EXT-X-STREAM-INF:.*RESOLUTION={resolutionPattern}.*\\s*(https?://[^\\s]+)", RegexOptions.IgnoreCase);
            Match resolutionMatch = resolutionRegex.Match(m3u8Content);
            if (resolutionMatch.Success)
            {
              string resolutionUrl = resolutionMatch.Groups[1].Value;
              MelonLogger.Msg($"Found {resolutionPattern} M3U8 URL: {resolutionUrl}");
              callback(resolutionUrl);
              yield break;
            }
            resolutionPattern = (string) null;
            resolutionRegex = (Regex) null;
            resolutionMatch = (Match) null;
          }
          MelonLogger.Msg("No suitable resolution URL found in M3U8 content.");
          callback((string) null);
          m3u8Content = (string) null;
        }
        else
        {
          MelonLogger.Msg("Failed to fetch M3U8 file: " + m3u8Request.error);
          callback((string) null);
        }
        m3u8Url = (string) null;
        m3u8Request = (UnityWebRequest) null;
      }
    }
  }

  private static void OnMediaPlayerEvent(
    MediaPlayer mp,
    MediaPlayerEvent.EventType eventType,
    ErrorCode errorCode)
  {
    if (eventType != 3)
      return;
    AVProSkyboxVideoMod.isPrepared = true;
  }

  public IEnumerator LoadScene(string sceneToLoad)
  {
    AssetBundleCreateRequest bundleLoadRequest = AssetBundle.LoadFromFileAsync($"{Application.streamingAssetsPath}/{sceneToLoad}");
    yield return (object) bundleLoadRequest;
    AssetBundle myLoadedAssetBundle = bundleLoadRequest.assetBundle;
    if (!((UnityEngine.Object) myLoadedAssetBundle == (UnityEngine.Object) null))
    {
      myLoadedAssetBundle.LoadAssetAsync<Shader>("skyboxEquirectangular360");
      myLoadedAssetBundle.Unload(false);
    }
  }
}
