using Il2CppBigscreen;
using Il2CppBigscreen.Hands;
using Il2CppBigscreen.UI;
using Il2CppBigscreen.Users;
using Il2CppInterop.Runtime;
using Il2CppSystem.Collections.Generic;
using Il2CppTMPro;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("BigscreenNoVR", "1.0.1", "Love")]
public class MainCameraControlMod : MelonMod
{
  private GameObject mainCamera;
  private Camera desktopCamera;
  private RenderTexture renderTexture;
  private GameObject canvasObject;
  private Canvas dropdownCanvas;
  private RawImage rawImage;
  private bool isWindowActive = false;
  private Transform head;
  private BlockingController blockingController;
  public Dropdown resolutionDropdown;
  private Text fpsText;
  private float deltaTime = 0.0f;
  private GameObject UIObject;
  private int currentRes = 0;
  private static int Fov = 60;
  public float lookSpeed = 2f;
  private float tapInterval = 0.3f;
  private float lastTapTimeW = -1f;
  private bool isFastSpeedW = false;
  private float pitch = 0.0f;
  private float yaw = 0.0f;
  private bool valid = false;
  private string url = "https://files-modcheck.phaze.org/desktopcamera.json";
  private bool disableMovement = true;
  private Slider speedSlider;
  private CanvasGroup speedSliderGroup;
  private float speedSliderHideDelay = 1f;
  private float speedSliderLastChangeTime;
  private GameObject presentationScreen;
  private bool isVideoMode = false;
  private bool isDebug = false;
  private TextMeshProUGUI muteText;
  public static bool muteInProgress = false;
  private MelonPreferences_Category BigscreenNoVRPreferences;
  private MelonPreferences_Entry<float> speed;
  private MelonPreferences_Entry<int> fastSpeed;
  private MelonPreferences_Entry<KeyCode> fullscreenKeybind;
  private MelonPreferences_Entry<KeyCode> menuKeybind;
  private MelonPreferences_Entry<KeyCode> movementToggleKeybind;
  private MelonPreferences_Entry<KeyCode> pttKeybind;

  private static bool NoSteam_Prefix() => false;

  public override void OnInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} Mod loaded!\nMade with LOVE\n=========================\n");
    this.BigscreenNoVRPreferences = MelonPreferences.CreateCategory("BigscreenNoVRPreferences");
    this.speed = this.BigscreenNoVRPreferences.CreateEntry<float>("Speed", 5f, description: "");
    this.fastSpeed = this.BigscreenNoVRPreferences.CreateEntry<int>("SuperSpeed", 15, description: "");
    this.fullscreenKeybind = this.BigscreenNoVRPreferences.CreateEntry<KeyCode>("FullscreenKeyBind", KeyCode.F2, description: "Key bind to toggle fullscreen mode\n For listed KeyCodes visit: https://docs.unity3d.com/2020.3/Documentation/ScriptReference/KeyCode.html");
    this.menuKeybind = this.BigscreenNoVRPreferences.CreateEntry<KeyCode>("MenuToggleKeyBind", KeyCode.Escape, description: "Key bind to toggle the desktop view");
    this.movementToggleKeybind = this.BigscreenNoVRPreferences.CreateEntry<KeyCode>("MovementToggleKeyBind", KeyCode.F3, description: "Key bind to toggle movement controls");
    this.pttKeybind = this.BigscreenNoVRPreferences.CreateEntry<KeyCode>("MicKeyBind", KeyCode.F5, description: "Key bind to use with PTT/PTM");
    this.BigscreenNoVRPreferences.SaveToFile(false);
  }

  private bool IsStreamerMode()
  {
    string[] commandLineArgs = System.Environment.GetCommandLineArgs();
    for (int index = 0; index < commandLineArgs.Length - 1; ++index)
    {
      if (commandLineArgs[index] == "-usertype" && commandLineArgs[index + 1].ToLower() == "streamer")
        return true;
    }
    return false;
  }

  public override void OnSceneWasInitialized(int buildIndex, string sceneName)
  {
    if (sceneName == "Master")
    {
      MelonCoroutines.Start(this.GetTextCoroutine(this.url, new System.Action<string>(this.OnValid)));
      this.blockingController = BIG_STATIC_SINGLETONS.blockingController;
      if ((UnityEngine.Object) GameObject.Find("DebugUser(Clone)") != (UnityEngine.Object) null)
        this.isDebug = true;
      this.CreateDropdownCanvas();
      this.CreateFPSText();
      this.CreateMuteCanvas();
      this.CreateSpeedSlider();
      this.SetFovValue();
      this.CreateFOVSlider();
    }
    if (!(sceneName == "UIMasterScene"))
      return;
    ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GetPage((BigUIState) 20)).GetComponent<SettingsScreen_Account>().OnDesktopOnlyToggled(false);
    ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GetPage((BigUIState) 20)).GetComponent<SettingsScreen_Account>().OnDesktopOnlyToggled(true);
  }

  private void SetFovValue()
  {
    if (!PlayerPrefs.HasKey("DesktopCameraFOV"))
      return;
    MainCameraControlMod.Fov = PlayerPrefs.GetInt("DesktopCameraFOV");
  }

  public override void OnUpdate()
  {
    if (!this.valid)
      return;
    if ((UnityEngine.Object) this.mainCamera == (UnityEngine.Object) null || (UnityEngine.Object) this.head == (UnityEngine.Object) null || (UnityEngine.Object) this.UIObject == (UnityEngine.Object) null)
    {
      this.mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
      if ((UnityEngine.Object) this.mainCamera == (UnityEngine.Object) null)
        return;
      this.mainCamera.active = true;
      this.head = this.mainCamera?.transform?.parent?.parent;
      if ((UnityEngine.Object) this.head == (UnityEngine.Object) null)
        return;
      this.UIObject = ((Component) BIG_STATIC_SINGLETONS.bigTabletUI).gameObject.transform.parent.gameObject;
    }
    if (!MainCameraControlMod.muteInProgress)
      this.blockingController?.ToggleMuteAll(false);
    if ((UnityEngine.Object) this.speedSliderGroup != (UnityEngine.Object) null && (double) this.speedSliderGroup.alpha > 0.0 && (double) Time.time - (double) this.speedSliderLastChangeTime > (double) this.speedSliderHideDelay)
      this.speedSliderGroup.alpha = 0.0f;
    if (Input.GetKeyDown(this.menuKeybind.Value))
      this.ToggleDesktopView();
    if (Input.GetKeyDown(this.fullscreenKeybind.Value))
      Screen.fullScreen = !Screen.fullScreen;
    if (Input.GetKeyDown(this.movementToggleKeybind.Value))
      this.disableMovement = !this.disableMovement;
    if (Input.GetKeyDown(KeyCode.F4))
    {
      try
      {
        this.ToggleVideoMode();
      }
      catch (System.Exception ex)
      {
        Melon<MainCameraControlMod>.Logger.Msg((object) ex);
      }
    }
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKey(KeyCode.H))
    {
      if (Input.GetKey(KeyCode.L))
      {
        GameObject gameObject = GameObject.Find("Left Hand Solver");
        WaveEasterEgg.head = this.head;
        WaveEasterEgg.leftHand = (UnityEngine.Object) gameObject != (UnityEngine.Object) null ? gameObject.transform : (Transform) null;
        WaveEasterEgg.Wave((Handedness) 0);
      }
      else
      {
        GameObject gameObject = GameObject.Find("Right Hand Solver");
        WaveEasterEgg.head = this.head;
        WaveEasterEgg.rightHand = (UnityEngine.Object) gameObject != (UnityEngine.Object) null ? gameObject.transform : (Transform) null;
        WaveEasterEgg.Wave((Handedness) 1);
      }
    }
    if (BIG_STATIC_SINGLETONS.micInput.pushToTalkEnabled)
    {
      if (Input.GetKeyDown(this.pttKeybind.Value))
        BIG_STATIC_SINGLETONS.micInput.Mute(false);
      if (Input.GetKeyUp(this.pttKeybind.Value))
        BIG_STATIC_SINGLETONS.micInput.Mute(true);
    }
    if (BIG_STATIC_SINGLETONS.micInput.pushToMuteEnabled)
    {
      if (Input.GetKeyDown(this.pttKeybind.Value))
        BIG_STATIC_SINGLETONS.micInput.Mute(true);
      if (Input.GetKeyUp(this.pttKeybind.Value))
        BIG_STATIC_SINGLETONS.micInput.Mute(false);
    }
    if (!BIG_STATIC_SINGLETONS.micInput.pushToMuteEnabled && !BIG_STATIC_SINGLETONS.micInput.pushToTalkEnabled && Input.GetKeyDown(this.pttKeybind.Value))
      BIG_STATIC_SINGLETONS.micInput.Mute(!BIG_STATIC_SINGLETONS.micInput.isMuted);
    this.muteText.text = BIG_STATIC_SINGLETONS.micInput.isMuted ? "\uF131" : "\uF130";
    this.muteText.color = BIG_STATIC_SINGLETONS.micInput.isMuted ? Color.red : Color.green;
    if (this.isWindowActive && (UnityEngine.Object) this.desktopCamera != (UnityEngine.Object) null)
      this.SyncCameras();
    if (!this.disableMovement)
    {
      this.HandleCameraControls();
      if (this.isDebug)
        this.HandleMouseLook();
    }
    if (!((UnityEngine.Object) this.fpsText != (UnityEngine.Object) null))
      return;
    this.deltaTime += (float) (((double) Time.unscaledDeltaTime - (double) this.deltaTime) * 0.10000000149011612);
    this.fpsText.text = $"{1f / this.deltaTime:0.0} FPS";
  }

  private void ToggleDesktopView()
  {
    if (this.isWindowActive)
      this.DestroyDesktopView();
    else
      this.CreateDesktopView(this.currentRes);
  }

  private void CreateDesktopView(int index)
  {
    GameObject target = new GameObject("DesktopCamera");
    UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object) target);
    this.desktopCamera = target.AddComponent<Camera>();
    if ((UnityEngine.Object) Camera.main != (UnityEngine.Object) null)
      this.desktopCamera.CopyFrom(Camera.main);
    this.SetRenderTextureResolution(index);
    this.desktopCamera.targetTexture = this.renderTexture;
    this.desktopCamera.fieldOfView = (float) MainCameraControlMod.Fov;
    this.canvasObject = new GameObject("DesktopCanvas");
    UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object) this.canvasObject);
    this.canvasObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
    this.rawImage = this.canvasObject.AddComponent<RawImage>();
    this.rawImage.texture = (Texture) this.renderTexture;
    this.isWindowActive = true;
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
    this.UIObject.active = false;
    this.disableMovement = false;
    if (!((UnityEngine.Object) this.presentationScreen == (UnityEngine.Object) null))
      return;
    this.presentationScreen = GameObject.Find("PresentationScreen(Clone)/Section/Display_Main");
  }

  private void ToggleVideoMode()
  {
  }

  private void DestroyDesktopView()
  {
    this.UIObject.active = true;
    this.disableMovement = true;
    if ((UnityEngine.Object) this.desktopCamera != (UnityEngine.Object) null)
      UnityEngine.Object.Destroy((UnityEngine.Object) this.desktopCamera.gameObject);
    if ((UnityEngine.Object) this.canvasObject != (UnityEngine.Object) null)
      UnityEngine.Object.Destroy((UnityEngine.Object) this.canvasObject);
    this.renderTexture.Release();
    this.isWindowActive = false;
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
  }

  private void SyncCameras()
  {
    if ((UnityEngine.Object) this.mainCamera == (UnityEngine.Object) null || (UnityEngine.Object) this.desktopCamera == (UnityEngine.Object) null)
      return;
    this.desktopCamera.transform.position = this.mainCamera.transform.position;
    this.desktopCamera.transform.rotation = this.mainCamera.transform.rotation;
  }

  private void HandleCameraControls()
  {
    float axis = Input.GetAxis("Mouse ScrollWheel");
    if ((double) axis != 0.0)
    {
      float num = 1.05f;
      if ((double) Mathf.Sign(axis) > 0.0)
        this.speed.Value *= num;
      else
        this.speed.Value /= num;
      this.speed.Value = Mathf.Clamp(this.speed.Value, 0.1f, 100f);
      this.BigscreenNoVRPreferences.SaveToFile(false);
      this.ShowSpeedSlider(this.speed.Value);
    }
    if (Input.GetKeyDown(KeyCode.W))
    {
      if ((double) Time.time - (double) this.lastTapTimeW <= (double) this.tapInterval)
        this.isFastSpeedW = true;
      this.lastTapTimeW = Time.time;
    }
    if (Input.GetKeyUp(KeyCode.W))
      this.isFastSpeedW = false;
    float num1 = this.isFastSpeedW ? (float) this.fastSpeed.Value : this.speed.Value;
    if (Input.GetKey(KeyCode.W))
      this.head.transform.position += this.head.transform.forward * num1 * Time.deltaTime;
    if (Input.GetKey(KeyCode.S))
      this.head.transform.position -= this.head.transform.forward * this.speed.Value * Time.deltaTime;
    if (Input.GetKey(KeyCode.A))
      this.head.transform.position -= this.head.transform.right * this.speed.Value * Time.deltaTime;
    if (Input.GetKey(KeyCode.D))
      this.head.transform.position += this.head.transform.right * this.speed.Value * Time.deltaTime;
    if (Input.GetKey(KeyCode.Space))
      this.head.transform.position += this.head.transform.up * this.speed.Value * Time.deltaTime;
    if (!Input.GetKey(KeyCode.LeftShift))
      return;
    this.head.transform.position -= this.head.transform.up * this.speed.Value * Time.deltaTime;
  }

  private void HandleMouseLook()
  {
    float axis1 = Input.GetAxis("Mouse X");
    float axis2 = Input.GetAxis("Mouse Y");
    this.yaw += axis1 * this.lookSpeed;
    this.pitch -= axis2 * this.lookSpeed;
    this.pitch = Mathf.Clamp(this.pitch, -90f, 90f);
    this.head.transform.eulerAngles = new Vector3(this.pitch, this.yaw, 0.0f);
  }

  private void OnResolutionChanged(int index)
  {
    this.currentRes = index;
    if (this.isWindowActive)
      this.DestroyDesktopView();
    this.CreateDesktopView(index);
  }

  private void SetRenderTextureResolution(int index)
  {
    this.renderTexture = new RenderTexture(1280 /*0x0500*/, 720, 24);
    switch (index)
    {
      case 0:
        this.renderTexture.width = 1280 /*0x0500*/;
        this.renderTexture.height = 720;
        this.renderTexture.depth = 24;
        break;
      case 1:
        this.renderTexture.width = 1920;
        this.renderTexture.height = 1080;
        this.renderTexture.depth = 24;
        break;
      case 2:
        this.renderTexture.width = 2560 /*0x0A00*/;
        this.renderTexture.height = 1440;
        this.renderTexture.depth = 32 /*0x20*/;
        break;
      case 3:
        this.renderTexture.width = 3840 /*0x0F00*/;
        this.renderTexture.height = 2160;
        this.renderTexture.depth = 32 /*0x20*/;
        break;
      case 4:
        this.renderTexture.width = 7680;
        this.renderTexture.height = 4320;
        this.renderTexture.depth = 32 /*0x20*/;
        break;
    }
    this.renderTexture.Release();
    this.renderTexture = new RenderTexture(this.renderTexture.width, this.renderTexture.height, this.renderTexture.depth);
  }

  private void CreateDropdownCanvas()
  {
    this.dropdownCanvas = new GameObject("DropdownCanvas").AddComponent<Canvas>();
    this.dropdownCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
    this.dropdownCanvas.sortingOrder = 9999;
    CanvasScaler canvasScaler = this.dropdownCanvas.gameObject.AddComponent<CanvasScaler>();
    canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
    canvasScaler.matchWidthOrHeight = 0.5f;
    this.dropdownCanvas.gameObject.AddComponent<GraphicRaycaster>();
    GameObject gameObject1 = new GameObject("ResolutionDropdown", new Il2CppSystem.Type[4]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Image>(),
      Il2CppType.Of<Dropdown>()
    });
    this.resolutionDropdown = gameObject1.GetComponent<Dropdown>();
    gameObject1.transform.SetParent(this.dropdownCanvas.transform, false);
    RectTransform component1 = gameObject1.GetComponent<RectTransform>();
    component1.anchorMin = new Vector2(0.0f, 1f);
    component1.anchorMax = new Vector2(0.0f, 1f);
    component1.pivot = new Vector2(0.0f, 1f);
    component1.anchoredPosition = new Vector2(10f, -10f);
    component1.sizeDelta = new Vector2(160f, 30f);
    gameObject1.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
    GameObject gameObject2 = new GameObject("Label", new Il2CppSystem.Type[2]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<Text>()
    });
    gameObject2.transform.SetParent(gameObject1.transform, false);
    RectTransform component2 = gameObject2.GetComponent<RectTransform>();
    component2.anchorMin = Vector2.zero;
    component2.anchorMax = Vector2.one;
    component2.offsetMin = Vector2.zero;
    component2.offsetMax = Vector2.zero;
    Text component3 = gameObject2.GetComponent<Text>();
    component3.color = Color.white;
    component3.alignment = TextAnchor.MiddleCenter;
    component3.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
    component3.text = "Resolution";
    this.resolutionDropdown.captionText = component3;
    GameObject gameObject3 = new GameObject("Template", new Il2CppSystem.Type[3]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<Image>(),
      Il2CppType.Of<Mask>()
    });
    gameObject3.transform.SetParent(gameObject1.transform, false);
    gameObject3.SetActive(false);
    RectTransform component4 = gameObject3.GetComponent<RectTransform>();
    component4.anchorMin = new Vector2(0.0f, 1f);
    component4.anchorMax = new Vector2(1f, 1f);
    component4.pivot = new Vector2(0.5f, 1f);
    component4.anchoredPosition = new Vector2(0.0f, 0.0f);
    component4.sizeDelta = new Vector2(160f, 150f);
    gameObject3.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 1f);
    GameObject gameObject4 = new GameObject("Item", new Il2CppSystem.Type[3]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<Toggle>(),
      Il2CppType.Of<CanvasRenderer>()
    });
    gameObject4.transform.SetParent(gameObject3.transform, false);
    RectTransform component5 = gameObject4.GetComponent<RectTransform>();
    component5.anchorMin = new Vector2(0.0f, 1f);
    component5.anchorMax = new Vector2(1f, 1f);
    component5.pivot = new Vector2(0.5f, 1f);
    component5.anchoredPosition = Vector2.zero;
    component5.sizeDelta = new Vector2(0.0f, 30f);
    GameObject gameObject5 = new GameObject("Item Background", new Il2CppSystem.Type[2]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<Image>()
    });
    gameObject5.transform.SetParent(gameObject4.transform, false);
    RectTransform component6 = gameObject5.GetComponent<RectTransform>();
    component6.anchorMin = Vector2.zero;
    component6.anchorMax = Vector2.one;
    component6.offsetMin = Vector2.zero;
    component6.offsetMax = Vector2.zero;
    gameObject5.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
    GameObject gameObject6 = new GameObject("Item Label", new Il2CppSystem.Type[2]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<Text>()
    });
    gameObject6.transform.SetParent(gameObject4.transform, false);
    RectTransform component7 = gameObject6.GetComponent<RectTransform>();
    component7.anchorMin = Vector2.zero;
    component7.anchorMax = Vector2.one;
    component7.offsetMin = Vector2.zero;
    component7.offsetMax = Vector2.zero;
    Text component8 = gameObject6.GetComponent<Text>();
    component8.color = Color.white;
    component8.alignment = TextAnchor.MiddleCenter;
    component8.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
    component8.text = "Option";
    this.resolutionDropdown.template = component4;
    this.resolutionDropdown.itemText = component8;
    this.AddResolutionOptions();
    this.resolutionDropdown.onValueChanged.AddListener((UnityAction<int>) (System.Action<int>) (index => this.OnResolutionChanged(index)));
    LayoutRebuilder.ForceRebuildLayoutImmediate(this.dropdownCanvas.GetComponent<RectTransform>());
  }

  private void CreateFOVSlider()
  {
    GameObject gameObject1 = new GameObject("FOVSlider", new Il2CppSystem.Type[4]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Image>(),
      Il2CppType.Of<Slider>()
    });
    Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
    if ((UnityEngine.Object) canvas == (UnityEngine.Object) null)
    {
      canvas = new GameObject("StandaloneCanvas", new Il2CppSystem.Type[3]
      {
        Il2CppType.Of<Canvas>(),
        Il2CppType.Of<CanvasScaler>(),
        Il2CppType.Of<GraphicRaycaster>()
      }).GetComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    }
    gameObject1.transform.SetParent(canvas.transform, false);
    RectTransform component1 = gameObject1.GetComponent<RectTransform>();
    component1.anchorMin = new Vector2(0.0f, 1f);
    component1.anchorMax = new Vector2(0.0f, 1f);
    component1.pivot = new Vector2(0.5f, 0.5f);
    component1.anchoredPosition = new Vector2(350f, -50f);
    component1.sizeDelta = new Vector2(160f, 30f);
    gameObject1.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
    Slider component2 = gameObject1.GetComponent<Slider>();
    component2.minValue = 60f;
    component2.maxValue = 170f;
    component2.value = (float) MainCameraControlMod.Fov;
    component2.wholeNumbers = true;
    GameObject gameObject2 = new GameObject("Fill Area", new Il2CppSystem.Type[1]
    {
      Il2CppType.Of<RectTransform>()
    });
    gameObject2.transform.SetParent(gameObject1.transform, false);
    RectTransform component3 = gameObject2.GetComponent<RectTransform>();
    component3.anchorMin = new Vector2(0.0f, 0.0f);
    component3.anchorMax = new Vector2(1f, 1f);
    component3.offsetMin = new Vector2(10f, 5f);
    component3.offsetMax = new Vector2(-10f, -5f);
    GameObject gameObject3 = new GameObject("Fill", new Il2CppSystem.Type[2]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<Image>()
    });
    gameObject3.transform.SetParent(gameObject2.transform, false);
    gameObject3.GetComponent<Image>().color = Color.green;
    RectTransform component4 = gameObject3.GetComponent<RectTransform>();
    component4.anchorMin = Vector2.zero;
    component4.anchorMax = Vector2.one;
    component4.offsetMin = Vector2.zero;
    component4.offsetMax = Vector2.zero;
    component2.fillRect = component4;
    GameObject gameObject4 = new GameObject("Handle Slide Area", new Il2CppSystem.Type[1]
    {
      Il2CppType.Of<RectTransform>()
    });
    gameObject4.transform.SetParent(gameObject1.transform, false);
    RectTransform component5 = gameObject4.GetComponent<RectTransform>();
    component5.anchorMin = new Vector2(0.0f, 0.0f);
    component5.anchorMax = new Vector2(1f, 1f);
    component5.offsetMin = new Vector2(10f, 5f);
    component5.offsetMax = new Vector2(-10f, -5f);
    GameObject gameObject5 = new GameObject("Handle", new Il2CppSystem.Type[2]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<Image>()
    });
    gameObject5.transform.SetParent(gameObject4.transform, false);
    Image component6 = gameObject5.GetComponent<Image>();
    component6.color = Color.white;
    RectTransform component7 = gameObject5.GetComponent<RectTransform>();
    component7.sizeDelta = new Vector2(20f, 20f);
    component2.handleRect = component7;
    component2.targetGraphic = (Graphic) component6;
    GameObject gameObject6 = new GameObject("FOVLabel", new Il2CppSystem.Type[2]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<TextMeshProUGUI>()
    });
    gameObject6.transform.SetParent(gameObject1.transform, false);
    RectTransform component8 = gameObject6.GetComponent<RectTransform>();
    component8.anchorMin = new Vector2(0.0f, 1f);
    component8.anchorMax = new Vector2(1f, 1f);
    component8.pivot = new Vector2(0.5f, 0.0f);
    component8.anchoredPosition = new Vector2(0.0f, 5f);
    component8.sizeDelta = new Vector2(160f, 24f);
    TextMeshProUGUI fovLabel = gameObject6.GetComponent<TextMeshProUGUI>();
    fovLabel.fontSize = 18f;
    fovLabel.color = Color.white;
    fovLabel.alignment = TextAlignmentOptions.Center;
    fovLabel.text = $"FOV: {component2.value}";
    component2.onValueChanged.AddListener((UnityAction<float>) (System.Action<float>) (val =>
    {
      fovLabel.text = $"FOV: {(int) val}";
      this.OnFOVChanged((int) val);
    }));
  }

  private void CreateSpeedSlider()
  {
    GameObject gameObject1 = new GameObject("SpeedSlider", new Il2CppSystem.Type[5]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Image>(),
      Il2CppType.Of<Slider>(),
      Il2CppType.Of<CanvasGroup>()
    });
    Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
    if ((UnityEngine.Object) canvas == (UnityEngine.Object) null)
    {
      canvas = new GameObject("SpeedCanvas", new Il2CppSystem.Type[3]
      {
        Il2CppType.Of<Canvas>(),
        Il2CppType.Of<CanvasScaler>(),
        Il2CppType.Of<GraphicRaycaster>()
      }).GetComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    }
    gameObject1.transform.SetParent(canvas.transform, false);
    RectTransform component1 = gameObject1.GetComponent<RectTransform>();
    component1.anchorMin = new Vector2(0.5f, 0.5f);
    component1.anchorMax = new Vector2(0.5f, 0.5f);
    component1.pivot = new Vector2(0.5f, 0.5f);
    component1.anchoredPosition = Vector2.zero;
    component1.sizeDelta = new Vector2(300f, 30f);
    gameObject1.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    this.speedSlider = gameObject1.GetComponent<Slider>();
    this.speedSlider.minValue = 0.1f;
    this.speedSlider.maxValue = 100f;
    this.speedSlider.value = this.speed.Value;
    this.speedSlider.interactable = false;
    this.speedSliderGroup = gameObject1.GetComponent<CanvasGroup>();
    this.speedSliderGroup.alpha = 0.0f;
    GameObject gameObject2 = new GameObject("Fill", new Il2CppSystem.Type[2]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<Image>()
    });
    gameObject2.transform.SetParent(gameObject1.transform, false);
    gameObject2.GetComponent<Image>().color = Color.green;
    RectTransform component2 = gameObject2.GetComponent<RectTransform>();
    component2.anchorMin = Vector2.zero;
    component2.anchorMax = Vector2.one;
    component2.offsetMin = Vector2.zero;
    component2.offsetMax = Vector2.zero;
    this.speedSlider.fillRect = component2;
  }

  private void OnFOVChanged(int fov)
  {
    MainCameraControlMod.Fov = fov;
    PlayerPrefs.SetInt("DesktopCameraFOV", fov);
    PlayerPrefs.Save();
  }

  private void CreateFPSText()
  {
    GameObject gameObject = new GameObject("FPSText", new Il2CppSystem.Type[2]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<Text>()
    });
    gameObject.transform.SetParent(this.dropdownCanvas.transform, false);
    RectTransform component = gameObject.GetComponent<RectTransform>();
    component.anchorMin = new Vector2(1f, 0.0f);
    component.anchorMax = new Vector2(1f, 0.0f);
    component.pivot = new Vector2(1f, 0.0f);
    component.anchoredPosition = new Vector2(-10f, 10f);
    component.sizeDelta = new Vector2(160f, 30f);
    this.fpsText = gameObject.GetComponent<Text>();
    this.fpsText.color = Color.white;
    this.fpsText.alignment = TextAnchor.MiddleRight;
    this.fpsText.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
    this.fpsText.text = "Loading FPS...";
  }

  private void CreateMuteCanvas()
  {
    GameObject gameObject1 = new GameObject("MuteButton", new Il2CppSystem.Type[4]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Image>(),
      Il2CppType.Of<Button>()
    });
    Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
    if ((UnityEngine.Object) canvas == (UnityEngine.Object) null)
    {
      canvas = new GameObject("StandaloneCanvas", new Il2CppSystem.Type[3]
      {
        Il2CppType.Of<Canvas>(),
        Il2CppType.Of<CanvasScaler>(),
        Il2CppType.Of<GraphicRaycaster>()
      }).GetComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    }
    gameObject1.transform.SetParent(canvas.transform, false);
    RectTransform component1 = gameObject1.GetComponent<RectTransform>();
    component1.anchorMin = new Vector2(0.0f, 1f);
    component1.anchorMax = new Vector2(0.0f, 1f);
    component1.pivot = new Vector2(0.5f, 0.5f);
    component1.anchoredPosition = new Vector2(220f, -30f);
    component1.sizeDelta = new Vector2(80f, 80f);
    gameObject1.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
    gameObject1.GetComponent<Button>().onClick.AddListener((UnityAction) new System.Action(this.OnMuteToggle));
    GameObject gameObject2 = new GameObject("MuteText", new Il2CppSystem.Type[2]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<TextMeshProUGUI>()
    });
    gameObject2.transform.SetParent(gameObject1.transform, false);
    this.muteText = gameObject2.GetComponent<TextMeshProUGUI>();
    TMP_FontAsset tmpFontAsset1 = (TMP_FontAsset) null;
    foreach (TMP_FontAsset tmpFontAsset2 in (TMP_FontAsset[]) UnityEngine.Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
    {
      if (tmpFontAsset2.name.Contains("Font Awesome 5 Free-Solid-900") || tmpFontAsset2.name.Contains("FontAwesome") || tmpFontAsset2.name.Contains("fa-solid-900"))
      {
        tmpFontAsset1 = tmpFontAsset2;
        break;
      }
    }
    if ((UnityEngine.Object) tmpFontAsset1 == (UnityEngine.Object) null)
      tmpFontAsset1 = UnityEngine.Resources.Load<TMP_FontAsset>("Font Awesome 5 Free-Solid-900 SDF");
    if ((UnityEngine.Object) tmpFontAsset1 != (UnityEngine.Object) null)
    {
      this.muteText.font = tmpFontAsset1;
      this.muteText.text = "\uF6A9";
    }
    this.muteText.fontSize = 48f;
    this.muteText.alignment = TextAlignmentOptions.Center;
    this.muteText.color = Color.red;
    RectTransform component2 = gameObject2.GetComponent<RectTransform>();
    component2.anchorMin = Vector2.zero;
    component2.anchorMax = Vector2.one;
    component2.offsetMin = Vector2.zero;
    component2.offsetMax = Vector2.zero;
  }

  private void ShowSpeedSlider(float currentSpeed)
  {
    if ((UnityEngine.Object) this.speedSlider == (UnityEngine.Object) null)
      return;
    this.speedSlider.value = currentSpeed;
    this.speedSliderGroup.alpha = 1f;
    this.speedSliderLastChangeTime = Time.time;
  }

  private void OnMuteToggle()
  {
    BIG_STATIC_SINGLETONS.micInput.Mute(!BIG_STATIC_SINGLETONS.micInput.isMuted);
  }

  private void AddResolutionOptions()
  {
    List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
    options.Add(new Dropdown.OptionData("720p"));
    options.Add(new Dropdown.OptionData("1080p"));
    options.Add(new Dropdown.OptionData("2K"));
    options.Add(new Dropdown.OptionData("4K"));
    options.Add(new Dropdown.OptionData("8K"));
    this.resolutionDropdown.ClearOptions();
    this.resolutionDropdown.AddOptions(options);
  }

  private IEnumerator GetTextCoroutine(string url, System.Action<string> callback)
  {
    UnityWebRequest www = UnityWebRequest.Get(url);
    yield return (object) www.SendWebRequest();
    if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
    {
      MelonLogger.Error("Error fetching details: " + www.error);
      System.Action<string> action = callback;
      if (action != null)
        action((string) null);
    }
    else
    {
      string textFromLink = www.downloadHandler.text;
      System.Action<string> action = callback;
      if (action != null)
        action(textFromLink);
      textFromLink = (string) null;
    }
  }

  private void OnValid(string info)
  {
    if (!(info == "true"))
      return;
    this.valid = true;
  }
}
