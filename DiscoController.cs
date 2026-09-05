using Il2CppBigscreen;
using Il2CppBigscreen.Environments;
using Il2CppBigscreen.Lighting;
using Il2CppBigscreen.UI;
using MelonLoader;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("Disco", "1.0.0", "Love")]
public class DiscoController : MelonMod
{
  private MelonPreferences_Category disco;
  private MelonPreferences_Entry<bool> enableMod;
  private MelonPreferences_Entry<float> speed;
  private MelonPreferences_Entry<float> delay;
  private MelonPreferences_Entry<float> range;
  private MelonPreferences_Entry<float> multiplier;
  private WasapiLoopbackCapture capture;
  private volatile float currentRMS;
  private readonly object rmsLock = new object();
  public bool isEnvDim;
  private bool isAdmin;
  private object discoC;
  private object cacheRefresh;
  private LightDimmer dim;
  private MMDeviceEnumerator deviceEnumerator;
  private MMDevice defaultDevice;
  private AudioSessionManager sessionManager;
  public float cacheRefreshInterval = 5f;
  private List<AudioSessionControl> relevantSessions;
  private List<string> processNames = new List<string>()
  {
    "vlc",
    "Spotify",
    "JellyfinMediaPlayer",
    "firefox",
    "chrome",
    "msedge",
    "rekordbox"
  };
  private bool once_flag = false;

  public override void OnInitializeMelon()
  {
    this.disco = MelonPreferences.CreateCategory("Disco");
    this.enableMod = this.disco.CreateEntry<bool>("EnableMod", false, description: "Turn the Disco effect on or off. When enabled, lights will change based on sound.");
    this.speed = this.disco.CreateEntry<float>("Speed", 0.1f, description: "How fast the lights update. A lower number makes it more responsive, a higher number makes it smoother (1.0f means it will change every second, 0.1f means 10 times in a second).");
    this.delay = this.disco.CreateEntry<float>("Delay", 0.0f, description: "A delay in seconds, so other users will see it in time.");
    this.range = this.disco.CreateEntry<float>("Range", 4f, description: "Defines the maximum intensity of the lighting effect. Higher values allow more dramatic brightness changes");
    this.multiplier = this.disco.CreateEntry<float>("Multiplier", 5f, description: "Boosts the effect of the sound on the lights. A higher number makes lights react more strongly.");
    this.disco.SaveToFile();
  }

  public override void OnLateInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} loaded!\nMade with LOVE\n=========================\n");
  }

  public override void OnSceneWasInitialized(int buildIndex, string sceneName)
  {
    this.disco.LoadFromFile(false);
    if (!(sceneName == "Master"))
      return;
    MelonCoroutines.Start(this.RefreshSessionCache());
    MelonCoroutines.Start(this.Disco());
  }

  private void ApplyLowPassFilter(
    float[] buffer,
    float cutoffFreq,
    float qFactor,
    float sampleRate)
  {
    float f = 6.28318548f * cutoffFreq / sampleRate;
    float num1 = Mathf.Sin(f) / (2f * qFactor);
    float num2 = Mathf.Cos(f);
    float num3 = 1f + num1;
    float num4 = -2f * num2;
    float num5 = 1f - num1;
    float num6 = (float) ((1.0 - (double) num2) / 2.0);
    float num7 = 1f - num2;
    float num8 = (float) ((1.0 - (double) num2) / 2.0);
    float num9 = num6 / num3;
    float num10 = num7 / num3;
    float num11 = num8 / num3;
    float num12 = num4 / num3;
    float num13 = num5 / num3;
    float num14 = 0.0f;
    float num15 = 0.0f;
    float num16 = 0.0f;
    float num17 = 0.0f;
    for (int index = 0; index < buffer.Length; ++index)
    {
      float num18 = buffer[index];
      float num19 = (float) ((double) num9 * (double) num18 + (double) num10 * (double) num14 + (double) num11 * (double) num15 - (double) num12 * (double) num16 - (double) num13 * (double) num17);
      buffer[index] = num19;
      num15 = num14;
      num14 = num18;
      num17 = num16;
      num16 = num19;
    }
  }

  private void OnAudioDataAvailable(object sender, WaveInEventArgs e)
  {
    if (e.BytesRecorded == 0)
      return;
    float[] numArray = new float[e.BytesRecorded / 4];
    Buffer.BlockCopy((Array) e.Buffer, 0, (Array) numArray, 0, e.BytesRecorded);
    this.ApplyLowPassFilter(numArray, 100f, 0.707f, 44100f);
    float num1 = 0.0f;
    foreach (float num2 in numArray)
      num1 += num2 * num2;
    float num3 = Mathf.Sqrt(num1 / (float) numArray.Length);
    lock (this.rmsLock)
      this.currentRMS = num3;
  }

  private IEnumerator RefreshSessionCache()
  {
    while (true)
    {
      this.disco.LoadFromFile(false);
      this.deviceEnumerator?.Dispose();
      this.defaultDevice?.Dispose();
      this.sessionManager?.Dispose();
      this.deviceEnumerator = new MMDeviceEnumerator();
      MMDeviceCollection deviceCollection = this.deviceEnumerator.EnumerateAudioEndPoints((DataFlow) 0, (DeviceState) 1);
      this.defaultDevice = this.GetCurrentPlayingDevice(deviceCollection);
      if (this.defaultDevice == null)
      {
        yield return (object) new WaitForSeconds(this.cacheRefreshInterval);
      }
      else
      {
        this.sessionManager = this.defaultDevice.AudioSessionManager;
        this.isEnvDim = this.IsEnvSupportDim();
        this.isAdmin = this.IsUserAdmin();
        while ((UnityEngine.Object) DiscoController.GetDimmer() == (UnityEngine.Object) null)
          yield return (object) null;
        this.dim = DiscoController.GetDimmer();
        SessionCollection sessions = this.sessionManager.Sessions;
        Task.Run((Action) (() => this.RefreshSessionCacheTask(sessions)));
        if (this.capture != null)
        {
          ((WasapiCapture) this.capture).StopRecording();
          ((WasapiCapture) this.capture).Dispose();
          this.capture = (WasapiLoopbackCapture) null;
        }
        this.capture = new WasapiLoopbackCapture(this.defaultDevice);
        ((WasapiCapture) this.capture).DataAvailable += new EventHandler<WaveInEventArgs>(this.OnAudioDataAvailable);
        ((WasapiCapture) this.capture).RecordingStopped += (EventHandler<StoppedEventArgs>) ((s, a) => ((WasapiCapture) this.capture).Dispose());
        ((WasapiCapture) this.capture).StartRecording();
        yield return (object) new WaitForSeconds(5f);
        deviceCollection = (MMDeviceCollection) null;
      }
    }
  }

  private void RefreshSessionCacheTask(SessionCollection sessions)
  {
    List<AudioSessionControl> audioSessionControlList = new List<AudioSessionControl>();
    for (int index = 0; index < sessions.Count; ++index)
    {
      AudioSessionControl session = sessions[index];
      try
      {
        AudioSessionControl audioSessionControl = session;
        string processName = Process.GetProcessById((int) audioSessionControl.GetProcessID).ProcessName;
        processName.ToString();
        if (this.processNames.Contains(processName))
          audioSessionControlList.Add(audioSessionControl);
      }
      catch (Exception ex)
      {
        this.LoggerInstance.Msg(ex.ToString());
      }
    }
    this.relevantSessions = audioSessionControlList;
  }

  private MMDevice GetCurrentPlayingDevice(MMDeviceCollection deviceCollection)
  {
    foreach (MMDevice device in deviceCollection)
    {
      if ((double) device.AudioMeterInformation.MasterPeakValue > 0.0 && device.ToString().Contains("Bigscreen"))
        return device;
    }
    return (MMDevice) null;
  }

  private IEnumerator Disco()
  {
    Queue<float> volumeQueue = new Queue<float>();
    while (this.relevantSessions == null || this.relevantSessions.Count < 1)
      yield return (object) null;
    while (true)
    {
      if (this.enableMod.Value && this.isAdmin && this.isEnvDim)
      {
        float rms;
        lock (this.rmsLock)
          rms = this.currentRMS;
        float processedValue = rms * this.multiplier.Value;
        processedValue = Mathf.Clamp(processedValue, 0.0f, this.range.Value);
        volumeQueue.Enqueue(processedValue);
        if ((double) volumeQueue.Count > (double) this.delay.Value / (double) this.speed.Value)
          processedValue = volumeQueue.Dequeue();
        this.dim.DimLights(processedValue);
      }
      yield return (object) new WaitForSeconds(this.speed.Value);
    }
  }

  private bool IsEnvSupportDim()
  {
    EnvironmentController environmentController = BIG_STATIC_SINGLETONS.environmentController;
    return environmentController != null && environmentController.EnvironmentSupportsDimmer;
  }

  private static LightDimmer GetDimmer()
  {
    return ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI)?.GetPage((BigUIState) 7))?.gameObject.GetComponent<SettingsScreen_Graphics>()?.lightDimmer;
  }

  private bool IsUserAdmin()
  {
    return BIG_STATIC_SINGLETONS.currentApp?.CurrentRoom?.IsLocalUserAdmin.GetValueOrDefault();
  }
}
