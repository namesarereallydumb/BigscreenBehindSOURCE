// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.Patches.UserNote
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using HarmonyLib;
using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.UI;
using Il2CppTMPro;
using System.Data.Common;
using System.Data.SQLite;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#nullable enable
namespace BigscreenBehind.Patches;

public static class UserNote
{
  private static SQLiteConnection _dbConnection;
  private static readonly string DatabasePath = "UserNotes.db";

  public static void InitializeDatabase()
  {
    UserNote._dbConnection = new SQLiteConnection($"Data Source={UserNote.DatabasePath};Version=3;");
    ((DbConnection) UserNote._dbConnection).Open();
    using (SQLiteCommand sqLiteCommand = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserNotes (\r\n                                      SocialId TEXT PRIMARY KEY,\r\n                                      Note     TEXT\r\n                                  );", UserNote._dbConnection))
      ((DbCommand) sqLiteCommand).ExecuteNonQuery();
  }

  private static void SaveNote(string socialId, string note)
  {
    using (SQLiteCommand sqLiteCommand = new SQLiteCommand("\r\n        INSERT INTO UserNotes (SocialId, Note)\r\n        VALUES                (@socialId, @note)\r\n        ON CONFLICT(SocialId) DO UPDATE SET Note = @note;\r\n    ", UserNote._dbConnection))
    {
      sqLiteCommand.Parameters.AddWithValue("@socialId", (object) socialId);
      sqLiteCommand.Parameters.AddWithValue("@note", (object) note);
      ((DbCommand) sqLiteCommand).ExecuteNonQuery();
    }
  }

  private static string? LoadNoteForUser(string socialId)
  {
    using (SQLiteCommand sqLiteCommand = new SQLiteCommand("SELECT Note FROM UserNotes WHERE SocialId = @socialId;", UserNote._dbConnection))
    {
      sqLiteCommand.Parameters.AddWithValue("@socialId", (object) socialId);
      using (SQLiteDataReader sqLiteDataReader = sqLiteCommand.ExecuteReader())
      {
        if (((DbDataReader) sqLiteDataReader).Read())
          return ((DbDataReader) sqLiteDataReader).IsDBNull(0) ? (string) null : ((DbDataReader) sqLiteDataReader).GetString(0);
      }
    }
    return (string) null;
  }

  [HarmonyPatch(typeof (UserProfile), "Show")]
  private static class UserProfile_Show
  {
    private static void Postfix(UserProfile __instance)
    {
      GameObject gameObject1 = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Bottom/ControlStrip(Clone)/Content/ToolbarRecenter/Btn (6)");
      Transform transform = ((Component) __instance).transform.Find("Content/Body");
      if ((bool) (UnityEngine.Object) transform.transform.Find("Btn_UserNote"))
        return;
      GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject1.gameObject, transform.transform);
      gameObject2.name = "Btn_UserNote";
      gameObject2.transform.localPosition = new Vector3(-0.225f, 0.117f, 0.0f);
      gameObject2.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
      gameObject2.transform.localScale = new Vector3(0.7f, 0.7f, 0.01f);
      gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetColors(new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f), new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f));
      Transform child = gameObject2.transform.GetChild(0);
      for (int index = 1; index < child.childCount; ++index)
        child.GetChild(index).gameObject.SetActive(false);
      child.transform.GetChild(0).GetComponent<TextMeshPro>().text = "\uF4FF";
      gameObject2.transform.GetChild(1).localPosition += new Vector3(0.0f, 0.03f, 0.0f);
      gameObject2.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Note";
      gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetupMeshes();
      Utils.ReplaceButtonEvent(gameObject2.gameObject.GetComponent<BigUIButton>().OnPoked, new System.Action<Il2CppSystem.Object>(UserNote.UserProfile_Show.OpenUserNote));
    }

    private static void OpenUserNote(Il2CppSystem.Object @object)
    {
      UserProfile component = ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI)?.GetPage((BigUIState) 27))?.gameObject.GetComponent<UserProfile>();
      if (string.IsNullOrEmpty(((SocialProfile) component?.SocialProfile).SocialId))
        return;
      string socialId = ((SocialProfile) component.SocialProfile).SocialId;
      string str = UserNote.LoadNoteForUser(socialId);
      InputFieldPopup InputFieldPopupNote = ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI)?.GetPage((BigUIState) 85))?.gameObject.GetComponent<InputFieldPopup>();
      InputFieldPopupNote.PageTitle = "Note";
      InputFieldPopupNote.KeypadStyle = (KeypadStyle) 5;
      InputFieldPopupNote.InputFieldText = str ?? string.Empty;
      InputFieldPopupNote.ForbiddenChars = "";
      InputFieldPopupNote.InputField.lineType = (BigInputField.LineType) 2;
      ((Component) InputFieldPopupNote.InputField).transform.localScale = new Vector3(0.0015f, 1f / 400f, 0.0015f);
      InputFieldPopupNote.InputField.textComponent.fontSize = 14;
      InputFieldPopupNote.InputField.placeholder.TryCast<Text>().text = "Enter note here...";
      InputFieldPopupNote.OnChangeConfirmed = (UnityAction<string>) (System.Action<string>) (noteText =>
      {
        if (string.IsNullOrEmpty(noteText))
        {
          ((BigUIPage) InputFieldPopupNote).EndState();
          Utils.UiMessage("Note cannot be empty.", stateOnDismissed: (BigUIState) 27);
        }
        else
        {
          UserNote.SaveNote(socialId, noteText);
          ((BigUIPage) InputFieldPopupNote).EndState();
          Utils.UiMessage("Note saved successfully.", stateOnDismissed: (BigUIState) 27);
        }
      });
      ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GoToPage((BigUIState) 85, false, true, false);
      ((BigUIPage) component).EndState();
    }
  }

  [HarmonyPatch(typeof (UserProfile_List), "Show")]
  private static class UserProfile_List_Show
  {
    private static void Postfix(UserProfile_List __instance)
    {
      GameObject gameObject1 = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Bottom/ControlStrip(Clone)/Content/ToolbarRecenter/Btn (6)");
      Transform transform = ((Component) __instance).transform.Find("Content/Body");
      if ((bool) (UnityEngine.Object) transform.transform.Find("Btn_UserNote"))
        return;
      GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject1.gameObject, transform.transform);
      gameObject2.name = "Btn_UserNote";
      gameObject2.transform.localPosition = new Vector3(-0.26f, 0.16f, 0.0f);
      gameObject2.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
      gameObject2.transform.localScale = new Vector3(0.7f, 0.7f, 0.01f);
      gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetColors(new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f), new Color(0.2132f, 0.2132f, 0.2132f, 0.7216f));
      Transform child = gameObject2.transform.GetChild(0);
      for (int index = 1; index < child.childCount; ++index)
        child.GetChild(index).gameObject.SetActive(false);
      child.transform.GetChild(0).GetComponent<TextMeshPro>().text = "\uF4FF";
      gameObject2.transform.GetChild(1).localPosition += new Vector3(0.0f, 0.03f, 0.0f);
      gameObject2.transform.GetChild(1).GetComponent<FadingTextTooltip>().tooltipText.text = "Note";
      gameObject2.transform.GetChild(1).localPosition += new Vector3(0.0f, 0.025f, 0.0f);
      gameObject2.gameObject.GetComponent<BigUIButton>().Visualization.SetupMeshes();
      Utils.ReplaceButtonEvent(gameObject2.gameObject.GetComponent<BigUIButton>().OnPoked, new System.Action<Il2CppSystem.Object>(UserNote.UserProfile_List_Show.OpenUserNote));
    }

    private static void OpenUserNote(Il2CppSystem.Object @object)
    {
      UserProfile_List component = ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI)?.GetPage((BigUIState) 207))?.gameObject.GetComponent<UserProfile_List>();
      if (string.IsNullOrEmpty(((SocialProfile) component?.SocialProfile).SocialId))
        return;
      string socialId = ((SocialProfile) component.SocialProfile).SocialId;
      string str = UserNote.LoadNoteForUser(socialId);
      InputFieldPopup InputFieldPopupNote = ((Component) ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI)?.GetPage((BigUIState) 85))?.gameObject.GetComponent<InputFieldPopup>();
      InputFieldPopupNote.PageTitle = "Note";
      InputFieldPopupNote.KeypadStyle = (KeypadStyle) 5;
      InputFieldPopupNote.InputFieldText = str ?? string.Empty;
      InputFieldPopupNote.ForbiddenChars = "";
      InputFieldPopupNote.InputField.lineType = (BigInputField.LineType) 2;
      ((Component) InputFieldPopupNote.InputField).transform.localScale = new Vector3(0.0015f, 1f / 400f, 0.0015f);
      InputFieldPopupNote.InputField.textComponent.fontSize = 14;
      InputFieldPopupNote.InputField.placeholder.TryCast<Text>().text = "Enter note here...";
      InputFieldPopupNote.OnChangeConfirmed = (UnityAction<string>) (System.Action<string>) (noteText =>
      {
        if (string.IsNullOrEmpty(noteText))
        {
          ((BigUIPage) InputFieldPopupNote).EndState();
          Utils.UiMessage("Note cannot be empty.", stateOnDismissed: (BigUIState) 207);
        }
        else
        {
          UserNote.SaveNote(socialId, noteText);
          ((BigUIPage) InputFieldPopupNote).EndState();
          Utils.UiMessage("Note saved successfully.", stateOnDismissed: (BigUIState) 207);
        }
      });
      ((BigUI) BIG_STATIC_SINGLETONS.bigTabletUI).GoToPage((BigUIState) 85, false, true, false);
      ((BigUIPage) component).EndState();
    }
  }
}
