using BigscreenBehind.Patches;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#nullable enable
namespace BigscreenBehind;

public class Main : MelonMod
{
  public static bool isPluginLoaded;
  private Canvas modCanvas;
  private GameObject dropdownPanel;
  private Button dropdownButton;
  private Text buttonText;
  private VerticalLayoutGroup contentLayout;
  private List<Toggle> modToggles = new List<Toggle>();
  private List<string> selectedMods = new List<string>();
  private List<MelonMod> mods = MelonTypeBase<MelonMod>.RegisteredMelons.ToList<MelonMod>();
  private Dictionary<string, MelonMod> modDict = new Dictionary<string, MelonMod>();
  private GameObject descriptionPanel;
  private Text descriptionText;
  private GameObject popupPanel;
  private Text popupText;
  private Button closeButton;
  private float popupDuration = 3f;
  private object autoCloseCoroutine;

  public override void OnPreSupportModule()
  {
    if (this.ValidatePlugin())
      this.OnRegister.Subscribe(new LemonAction(this.OnRegistered));
    this.LoggerInstance.Msg("MultiMelonMain - OnPreSupportModule. Subscribed to OnRegister.");
  }

  private void OnRegistered()
  {
    this.LoggerInstance.Msg("MultiMelonMain - OnRegistered");
    List<System.Type> list = ((IEnumerable<System.Type>) this.GetType().Assembly.GetTypes()).Where<System.Type>((System.Func<System.Type, bool>) (t => t.GetCustomAttribute<MultiMelonSubModAttribute>() != null)).Where<System.Type>((System.Func<System.Type, bool>) (t => t.GetCustomAttribute<MultiMelonSubModDebugAttribute>() == null)).ToList<System.Type>();
    PropertyInfo gamesProp = typeof (MelonBase).GetProperty("Games", BindingFlags.Instance | BindingFlags.Public);
    PropertyInfo infoProp = typeof (MelonBase).GetProperty("Info", BindingFlags.Instance | BindingFlags.Public);
    PropertyInfo melonAssemblyProp = typeof (MelonBase).GetProperty("MelonAssembly", BindingFlags.Instance | BindingFlags.Public);
    PropertyInfo consoleColorProp = typeof (MelonBase).GetProperty("ConsoleColor", BindingFlags.Instance | BindingFlags.Public);
    PropertyInfo authorConsoleColorProp = typeof (MelonBase).GetProperty("AuthorConsoleColor", BindingFlags.Instance | BindingFlags.Public);
    MelonBase.RegisterSorted<MelonBase>((IEnumerable<MelonBase>) list.Select<System.Type, MelonBase>((System.Func<System.Type, MelonBase>) (t =>
    {
      MelonBase melonBase = (MelonBase) (System.Activator.CreateInstance(t) ?? throw new System.Exception($"Failed to construct a sub mod of type {t}"));
      MultiMelonSubModAttribute customAttribute = t.GetCustomAttribute<MultiMelonSubModAttribute>();
      gamesProp.SetValue((object) melonBase, (object) this.Games);
      infoProp.SetValue((object) melonBase, (object) new MelonInfoAttribute(t, customAttribute.Name, customAttribute.Version, customAttribute.Author));
      melonAssemblyProp.SetValue((object) melonBase, (object) this.MelonAssembly);
      PropertyInfo property1 = this.GetType().GetProperty("ConsoleColor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      PropertyInfo property2 = this.GetType().GetProperty("AuthorConsoleColor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      object obj1 = property1?.GetValue((object) this);
      object obj2 = property2?.GetValue((object) this);
      consoleColorProp?.SetValue((object) melonBase, obj1);
      authorConsoleColorProp?.SetValue((object) melonBase, obj2);
      return melonBase;
    })).ToList<MelonBase>().Where<MelonBase>((System.Func<MelonBase, bool>) (mod => this.GetModToggleState(mod.Info.Name))).ToList<MelonBase>());
  }

  public override void OnInitializeMelon()
  {
  }

  public override void OnLateInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} loaded!\nMade with LOVE\n=========================\n");
    UserNote.InitializeDatabase();
    MelonCoroutines.Start(FreeFly.GetUsersFromCloudB());
  }

  private bool ValidatePlugin()
  {
    Main.isPluginLoaded = MelonTypeBase<MelonPlugin>.RegisteredMelons.Any<MelonPlugin>((System.Func<MelonPlugin, bool>) (p => p.Info.Name == "BigscreenBehindPlugin"));
    this.LoggerInstance.Msg("Plugin " + (Main.isPluginLoaded ? "Loaded" : "Not Loaded"));
    return Main.isPluginLoaded;
  }

  private void UnloadThisMod()
  {
    this.LoggerInstance.Warning("Unregistering mod...");
    this.Unregister();
    this.LoggerInstance.Msg("Mod successfully unloaded.");
  }

  private void ApplyPatches()
  {
    foreach (System.Type validType in typeof (Main).Assembly.GetValidTypes())
    {
      if (validType.GetCustomAttribute<HarmonyPatch>() != null)
      {
        try
        {
          if (MelonDebug.IsEnabled())
            this.LoggerInstance.Msg("Applying " + validType.Name);
          this.HarmonyInstance.PatchAll(validType);
        }
        catch (System.Exception ex)
        {
          this.LoggerInstance.Error($"Exception while attempting to apply {validType.Name}: {ex}");
        }
      }
    }
  }

  public override void OnSceneWasInitialized(int buildIndex, string sceneName)
  {
    if (!(sceneName == "Master"))
      return;
    if (!this.ValidatePlugin())
    {
      this.ShowRestartPopup("Plugin not installed\nMod disabled", false);
    }
    else
    {
      this.CreateUI();
      MelonCoroutines.Start(BigscreenBehind.Utils.Username());
      MelonCoroutines.Start(BehindMenu.SetMenu());
    }
  }

  public bool GetModToggleState(string modName)
  {
    return PlayerPrefs.HasKey(modName) && PlayerPrefs.GetInt(modName) == 1;
  }

  private void CreateUI()
  {
    List<System.Type> list = ((IEnumerable<System.Type>) this.GetType().Assembly.GetTypes()).Where<System.Type>((System.Func<System.Type, bool>) (t => t.GetCustomAttribute<MultiMelonSubModAttribute>() != null)).Where<System.Type>((System.Func<System.Type, bool>) (t => t.GetCustomAttribute<MultiMelonSubModDebugAttribute>() == null)).ToList<System.Type>();
    PropertyInfo gamesProp = typeof (MelonBase).GetProperty("Games", BindingFlags.Instance | BindingFlags.Public);
    PropertyInfo infoProp = typeof (MelonBase).GetProperty("Info", BindingFlags.Instance | BindingFlags.Public);
    PropertyInfo melonAssemblyProp = typeof (MelonBase).GetProperty("MelonAssembly", BindingFlags.Instance | BindingFlags.Public);
    foreach (MelonBase melonBase in list.Select<System.Type, MelonBase>((System.Func<System.Type, MelonBase>) (t =>
    {
      MelonBase ui = (MelonBase) (System.Activator.CreateInstance(t) ?? throw new System.Exception($"Failed to construct a sub mod of type {t}"));
      MultiMelonSubModAttribute customAttribute = t.GetCustomAttribute<MultiMelonSubModAttribute>();
      gamesProp.SetValue((object) ui, (object) this.Games);
      infoProp.SetValue((object) ui, (object) new MelonInfoAttribute(t, customAttribute.Name, customAttribute.Version, customAttribute.Author));
      melonAssemblyProp.SetValue((object) ui, (object) this.MelonAssembly);
      return ui;
    })).ToList<MelonBase>())
      this.modDict[melonBase.Info.Name] = (MelonMod) melonBase;
    this.modCanvas = new GameObject("ModCanvas").AddComponent<Canvas>();
    this.modCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
    this.modCanvas.sortingOrder = 100;
    this.modCanvas.gameObject.AddComponent<GraphicRaycaster>();
    GameObject gameObject1 = new GameObject("DropdownButton", new Il2CppSystem.Type[4]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Image>(),
      Il2CppType.Of<Button>()
    });
    gameObject1.transform.SetParent(this.modCanvas.transform, false);
    this.dropdownButton = gameObject1.GetComponent<Button>();
    this.dropdownButton.onClick.AddListener((UnityAction) new System.Action(this.ToggleDropdown));
    RectTransform component1 = gameObject1.GetComponent<RectTransform>();
    component1.anchorMin = new Vector2(1f, 1f);
    component1.anchorMax = new Vector2(1f, 1f);
    component1.pivot = new Vector2(1f, 1f);
    component1.anchoredPosition = new Vector2(-20f, -2f);
    component1.sizeDelta = new Vector2(200f, 40f);
    gameObject1.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
    GameObject gameObject2 = new GameObject("ButtonText", new Il2CppSystem.Type[1]
    {
      Il2CppType.Of<Text>()
    });
    gameObject2.transform.SetParent(gameObject1.transform, false);
    this.buttonText = gameObject2.GetComponent<Text>();
    this.buttonText.text = "Bigscreen Behind\n↓     Mod Selection     ↓";
    this.buttonText.alignment = TextAnchor.MiddleCenter;
    this.buttonText.color = Color.white;
    this.buttonText.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
    RectTransform component2 = gameObject2.GetComponent<RectTransform>();
    component2.anchorMin = Vector2.zero;
    component2.anchorMax = Vector2.one;
    component2.offsetMin = Vector2.zero;
    component2.offsetMax = Vector2.zero;
    GameObject gameObject3 = new GameObject("GearButton", new Il2CppSystem.Type[4]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<Button>(),
      Il2CppType.Of<Image>(),
      Il2CppType.Of<CanvasRenderer>()
    });
    gameObject3.transform.SetParent(this.modCanvas.transform, false);
    gameObject3.GetComponent<Button>().onClick.AddListener((UnityAction) new System.Action(this.OpenMelonPreferences));
    RectTransform component3 = gameObject3.GetComponent<RectTransform>();
    component3.anchorMin = new Vector2(1f, 1f);
    component3.anchorMax = new Vector2(1f, 1f);
    component3.pivot = new Vector2(1f, 1f);
    component3.anchoredPosition = new Vector2(-222f, -2f);
    component3.sizeDelta = new Vector2(40f, 40f);
    gameObject3.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
    GameObject gameObject4 = new GameObject("GearText", new Il2CppSystem.Type[2]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<TextMeshProUGUI>()
    });
    gameObject4.transform.SetParent(gameObject3.transform, false);
    TextMeshProUGUI component4 = gameObject4.GetComponent<TextMeshProUGUI>();
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
      component4.font = tmpFontAsset1;
      component4.text = "\uF013";
    }
    component4.fontSize = 24f;
    component4.alignment = TextAlignmentOptions.Center;
    component4.color = Color.white;
    RectTransform component5 = gameObject4.GetComponent<RectTransform>();
    component5.anchorMin = Vector2.zero;
    component5.anchorMax = Vector2.one;
    component5.offsetMin = Vector2.zero;
    component5.offsetMax = Vector2.zero;
    this.dropdownPanel = new GameObject("DropdownPanel", new Il2CppSystem.Type[3]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Image>()
    });
    this.dropdownPanel.transform.SetParent(this.modCanvas.transform, false);
    this.dropdownPanel.SetActive(false);
    RectTransform component6 = this.dropdownPanel.GetComponent<RectTransform>();
    component6.anchorMin = new Vector2(1f, 1f);
    component6.anchorMax = new Vector2(1f, 1f);
    component6.pivot = new Vector2(1f, 1f);
    component6.anchoredPosition = new Vector2(-20f, -42f);
    component6.sizeDelta = new Vector2(200f, 150f);
    this.dropdownPanel.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
    GameObject contentObj = new GameObject("Content", new Il2CppSystem.Type[2]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<VerticalLayoutGroup>()
    });
    contentObj.transform.SetParent(this.dropdownPanel.transform, false);
    this.contentLayout = contentObj.GetComponent<VerticalLayoutGroup>();
    this.contentLayout.childAlignment = TextAnchor.UpperLeft;
    this.contentLayout.spacing = 5f;
    this.contentLayout.padding = new RectOffset(10, 10, 10, 10);
    RectTransform component7 = contentObj.GetComponent<RectTransform>();
    component7.anchorMin = Vector2.zero;
    component7.anchorMax = Vector2.one;
    component7.offsetMin = new Vector2(0.0f, 0.0f);
    component7.offsetMax = new Vector2(0.0f, 0.0f);
    int b = this.modDict.Count * 40 + (this.modDict.Count - 1) * 5 + 20;
    component6.sizeDelta = new Vector2(200f, (float) Mathf.Min(300, b));
    this.PopulateModToggles(contentObj);
  }

  private void OpenMelonPreferences()
  {
    string str = Path.Combine(MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg");
    Process.Start(new ProcessStartInfo()
    {
      FileName = str,
      UseShellExecute = true
    });
  }

  private void PopulateModToggles(GameObject contentObj)
  {
    foreach (KeyValuePair<string, MelonMod> keyValuePair in this.modDict)
    {
      string modName = keyValuePair.Key;
      MelonMod modInstance = keyValuePair.Value;
      if (!(modName == "BigscreenBehind"))
      {
        GameObject gameObject1 = new GameObject(modName, new Il2CppSystem.Type[4]
        {
          Il2CppType.Of<RectTransform>(),
          Il2CppType.Of<CanvasRenderer>(),
          Il2CppType.Of<Toggle>(),
          Il2CppType.Of<Image>()
        });
        gameObject1.transform.SetParent(contentObj.transform, false);
        Toggle component1 = gameObject1.GetComponent<Toggle>();
        this.modToggles.Add(component1);
        gameObject1.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 30f);
        gameObject1.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
        GameObject gameObject2 = new GameObject("Background", new Il2CppSystem.Type[3]
        {
          Il2CppType.Of<RectTransform>(),
          Il2CppType.Of<CanvasRenderer>(),
          Il2CppType.Of<Image>()
        });
        gameObject2.transform.SetParent(gameObject1.transform, false);
        RectTransform component2 = gameObject2.GetComponent<RectTransform>();
        component2.anchorMin = new Vector2(0.0f, 0.5f);
        component2.anchorMax = new Vector2(0.0f, 0.5f);
        component2.pivot = new Vector2(0.0f, 0.5f);
        component2.anchoredPosition = new Vector2(10f, 0.0f);
        component2.sizeDelta = new Vector2(20f, 20f);
        Image component3 = gameObject2.GetComponent<Image>();
        component3.color = Color.white;
        GameObject gameObject3 = new GameObject("Checkmark", new Il2CppSystem.Type[3]
        {
          Il2CppType.Of<RectTransform>(),
          Il2CppType.Of<CanvasRenderer>(),
          Il2CppType.Of<Image>()
        });
        gameObject3.transform.SetParent(gameObject2.transform, false);
        RectTransform component4 = gameObject3.GetComponent<RectTransform>();
        component4.anchorMin = new Vector2(0.5f, 0.5f);
        component4.anchorMax = new Vector2(0.5f, 0.5f);
        component4.pivot = new Vector2(0.5f, 0.5f);
        component4.anchoredPosition = Vector2.zero;
        component4.sizeDelta = new Vector2(12f, 12f);
        Image component5 = gameObject3.GetComponent<Image>();
        component5.color = Color.green;
        component1.targetGraphic = (Graphic) component3;
        component1.graphic = (Graphic) component5;
        component1.isOn = MelonTypeBase<MelonMod>.RegisteredMelons.Any<MelonMod>((System.Func<MelonMod, bool>) (p => p.Info.Name == modInstance.Info.Name));
        GameObject gameObject4 = new GameObject("Label", new Il2CppSystem.Type[1]
        {
          Il2CppType.Of<Text>()
        });
        gameObject4.transform.SetParent(gameObject1.transform, false);
        Text component6 = gameObject4.GetComponent<Text>();
        component6.text = modName;
        component6.alignment = TextAnchor.MiddleLeft;
        component6.fontSize = 14;
        component6.color = Color.white;
        component6.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
        component6.horizontalOverflow = HorizontalWrapMode.Overflow;
        component6.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform component7 = gameObject4.GetComponent<RectTransform>();
        component7.anchorMin = new Vector2(0.0f, 0.0f);
        component7.anchorMax = new Vector2(1f, 1f);
        component7.offsetMin = new Vector2(35f, 5f);
        component7.offsetMax = new Vector2(-5f, -5f);
        component1.onValueChanged.AddListener((UnityAction<bool>) (System.Action<bool>) (isOn => this.UpdateSelection(modName, modInstance, isOn)));
      }
    }
  }

  private void CreateDescriptionPanel()
  {
    this.descriptionPanel = new GameObject("DescriptionPanel", new Il2CppSystem.Type[3]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Image>()
    });
    this.descriptionPanel.transform.SetParent(this.modCanvas.transform, false);
    RectTransform component1 = this.descriptionPanel.GetComponent<RectTransform>();
    component1.pivot = new Vector2(0.0f, 1f);
    component1.sizeDelta = new Vector2(250f, 0.0f);
    this.descriptionPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    GameObject gameObject = new GameObject("DescriptionText", new Il2CppSystem.Type[3]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Text>()
    });
    gameObject.transform.SetParent(this.descriptionPanel.transform, false);
    this.descriptionText = gameObject.GetComponent<Text>();
    this.descriptionText.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
    this.descriptionText.fontSize = 14;
    this.descriptionText.color = Color.white;
    this.descriptionText.alignment = TextAnchor.UpperLeft;
    this.descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
    this.descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
    RectTransform component2 = gameObject.GetComponent<RectTransform>();
    component2.anchorMin = Vector2.zero;
    component2.anchorMax = Vector2.one;
    component2.offsetMin = new Vector2(10f, 10f);
    component2.offsetMax = new Vector2(-10f, -10f);
    this.descriptionPanel.SetActive(false);
  }

  private void ShowDescription(string modName, string description, Vector2 position)
  {
    this.descriptionText.text = $"<b>{modName}</b>\n\n{description}";
    RectTransform component = this.descriptionPanel.GetComponent<RectTransform>();
    this.descriptionText.rectTransform.sizeDelta = new Vector2(230f, 0.0f);
    Canvas.ForceUpdateCanvases();
    float y = this.descriptionText.preferredHeight + 20f;
    component.sizeDelta = new Vector2(250f, y);
    float width = (float) Screen.width;
    float height = (float) Screen.height;
    Vector2 vector2 = new Vector2(position.x + 20f, position.y);
    if ((double) vector2.x + (double) component.sizeDelta.x > (double) width)
      vector2.x = (float) ((double) position.x - (double) component.sizeDelta.x - 20.0);
    if ((double) vector2.y - (double) component.sizeDelta.y < 0.0)
      vector2.y = component.sizeDelta.y;
    component.position = (Vector3) vector2;
    this.descriptionPanel.SetActive(true);
  }

  private void HideDescription() => this.descriptionPanel.SetActive(false);

  private void UpdateSelection(string modName, MelonMod modInstance, bool isOn)
  {
    PlayerPrefs.SetInt(modName, isOn ? 1 : 0);
    PlayerPrefs.Save();
    string str = isOn ? "ON" : "OFF";
    this.ShowRestartPopup($"{modName} - {str}\nRestart the game for changes to take effect.", isOn);
  }

  private void ToggleDropdown() => this.dropdownPanel.SetActive(!this.dropdownPanel.activeSelf);

  private void ShowRestartPopup(string message, bool on)
  {
    if ((UnityEngine.Object) this.popupPanel == (UnityEngine.Object) null)
      this.CreatePopupUI();
    this.popupText.text = message;
    this.popupText.color = on ? Color.green : Color.red;
    this.popupPanel.SetActive(true);
    if (this.autoCloseCoroutine != null)
      MelonCoroutines.Stop(this.autoCloseCoroutine);
    this.autoCloseCoroutine = MelonCoroutines.Start(this.AutoClosePopup());
  }

  private void CreatePopupUI()
  {
    this.popupPanel = new GameObject("PopupPanel", new Il2CppSystem.Type[3]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Image>()
    });
    this.popupPanel.transform.SetParent(this.modCanvas.transform, false);
    RectTransform component1 = this.popupPanel.GetComponent<RectTransform>();
    component1.anchorMin = new Vector2(0.5f, 0.5f);
    component1.anchorMax = new Vector2(0.5f, 0.5f);
    component1.pivot = new Vector2(0.5f, 0.5f);
    component1.anchoredPosition = Vector2.zero;
    component1.sizeDelta = new Vector2(300f, 150f);
    this.popupPanel.GetComponent<Image>().color = Color.black;
    GameObject gameObject1 = new GameObject("PopupText", new Il2CppSystem.Type[3]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Text>()
    });
    gameObject1.transform.SetParent(this.popupPanel.transform, false);
    this.popupText = gameObject1.GetComponent<Text>();
    this.popupText.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
    this.popupText.fontSize = 16 /*0x10*/;
    this.popupText.alignment = TextAnchor.UpperCenter;
    this.popupText.color = Color.white;
    RectTransform component2 = gameObject1.GetComponent<RectTransform>();
    component2.anchorMin = new Vector2(0.0f, 0.0f);
    component2.anchorMax = new Vector2(1f, 0.8f);
    component2.offsetMin = new Vector2(10f, 10f);
    component2.offsetMax = new Vector2(-10f, 0.0f);
    GameObject gameObject2 = new GameObject("CloseButton", new Il2CppSystem.Type[4]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Image>(),
      Il2CppType.Of<Button>()
    });
    gameObject2.transform.SetParent(this.popupPanel.transform, false);
    this.closeButton = gameObject2.GetComponent<Button>();
    RectTransform component3 = gameObject2.GetComponent<RectTransform>();
    component3.anchorMin = new Vector2(0.5f, 0.0f);
    component3.anchorMax = new Vector2(0.5f, 0.0f);
    component3.pivot = new Vector2(0.5f, 0.0f);
    component3.anchoredPosition = new Vector2(0.0f, 20f);
    component3.sizeDelta = new Vector2(100f, 30f);
    gameObject2.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
    GameObject gameObject3 = new GameObject("ButtonText", new Il2CppSystem.Type[3]
    {
      Il2CppType.Of<RectTransform>(),
      Il2CppType.Of<CanvasRenderer>(),
      Il2CppType.Of<Text>()
    });
    gameObject3.transform.SetParent(gameObject2.transform, false);
    Text component4 = gameObject3.GetComponent<Text>();
    component4.text = "OK";
    component4.alignment = TextAnchor.MiddleCenter;
    component4.color = Color.white;
    component4.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
    component4.fontSize = 14;
    RectTransform component5 = gameObject3.GetComponent<RectTransform>();
    component5.anchorMin = Vector2.zero;
    component5.anchorMax = Vector2.one;
    component5.offsetMin = Vector2.zero;
    component5.offsetMax = Vector2.zero;
    this.closeButton.onClick.AddListener((UnityAction) (System.Action) (() => this.popupPanel.SetActive(false)));
    this.popupPanel.SetActive(false);
  }

  private IEnumerator AutoClosePopup()
  {
    yield return (object) new WaitForSeconds(this.popupDuration);
    if ((UnityEngine.Object) this.popupPanel != (UnityEngine.Object) null)
      this.popupPanel.SetActive(false);
  }
}
