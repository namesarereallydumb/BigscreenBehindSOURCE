using HarmonyLib;
using Il2CppBigscreen;
using Il2CppBigscreen.Cloud;
using Il2CppBigscreen.UI;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

#nullable enable
namespace BigscreenBehind;

[MultiMelonSubMod("FilterFriendsList", "0.5.0", "Love")]
internal class FilterFriendsList : MelonMod
{
  private static bool working;
  private static bool init;

  private static IEnumerator SetLastOnline(UserProfile_List __instance)
  {
    while (((SocialProfile) __instance.SocialProfile).SocialId == null)
      yield return (object) null;
    string id = ((SocialProfile) __instance.SocialProfile).SocialId;
    UserProfileFetcher fetcher = new UserProfileFetcher(Accounts.GetAccessToken(), CloudConfig.BIGSCREEN_API_KEY, CloudConfig.BIGSCREEN_CLOUD_API_URL, id);
    TextMeshPro original = __instance.joinedText;
    Transform parent = original.transform.parent;
    Transform displayTransform = parent.Find("LastOnlineText");
    TextMeshPro displayText;
    if ((UnityEngine.Object) displayTransform != (UnityEngine.Object) null)
    {
      displayText = displayTransform.GetComponent<TextMeshPro>();
      displayText.text = "Last seen: Loading...";
    }
    else
    {
      GameObject cloneGO = UnityEngine.Object.Instantiate<GameObject>(original.gameObject, parent);
      cloneGO.name = "LastOnlineText";
      displayText = cloneGO.GetComponent<TextMeshPro>();
      displayText.text = "Last seen: Loading...";
      Vector3 pos = original.transform.localPosition;
      pos.y += 0.015f;
      original.transform.localPosition = pos;
      Vector3 offset = new Vector3(0.0f, -0.03f, 0.0f);
      cloneGO.transform.localPosition = original.transform.localPosition + offset;
      cloneGO = (GameObject) null;
    }
    displayText.color = new Color(displayText.color.r, displayText.color.g, displayText.color.b, 0.0f);
    UserProfileFetcher.UserProfile profile = (UserProfileFetcher.UserProfile) null;
    yield return (object) fetcher.GetUserProfileCoroutine((Action<UserProfileFetcher.UserProfile>) (p => profile = p));
    if (profile?.Stats != null && profile.Stats.LastOnlineDate > 0L)
    {
      DateTime lastOnline = DateTimeOffset.FromUnixTimeSeconds(profile.Stats.LastOnlineDate).LocalDateTime;
      DateTime now = DateTime.Now.Date;
      DateTime lastOnlineDateOnly = lastOnline.Date;
      int daysAgo = (now - lastOnlineDateOnly).Days;
      string display;
      if (daysAgo == 0)
        display = "Today";
      else if (daysAgo == 1)
        display = "Yesterday";
      else if (daysAgo <= 30)
        display = $"{daysAgo} days ago";
      else
        display = lastOnline.ToString("MMMM yyyy", (IFormatProvider) CultureInfo.CurrentCulture);
      if (File.Exists(Path.Combine(MelonEnvironment.PluginsDirectory, "Debug.txt")))
        display = lastOnline.ToString();
      displayText.text = "Last seen: " + display;
      display = (string) null;
    }
    else
      displayText.text = "Last seen: Unknown";
    float duration = 1f;
    float elapsed = 0.0f;
    Color color = displayText.color;
    while ((double) elapsed < (double) duration)
    {
      elapsed += Time.deltaTime;
      float alpha = Mathf.Clamp01(elapsed / duration);
      displayText.color = new Color(color.r, color.g, color.b, alpha);
      yield return (object) null;
    }
  }

  public override void OnLateInitializeMelon()
  {
    this.LoggerInstance.Msg($"\n=========================\n{this.Info.Name} Mod loaded!\nMade with LOVE\n=========================\n");
    FilterFriendsList.init = true;
  }

  private static IEnumerator SetTotalBlocked()
  {
    while ((UnityEngine.Object) GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Center/SocialBlockedTablePage(Clone)/StaticContent/Header/TextMeshPro") == (UnityEngine.Object) null)
      yield return (object) null;
    GameObject blockedText = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Center/SocialBlockedTablePage(Clone)/StaticContent/Header/TextMeshPro");
    TextMeshPro textComponent = blockedText.GetComponent<TextMeshPro>();
    int blockedCount = BIG_STATIC_SINGLETONS.localUserModel?.CurrentRoom?.Owner?.Stats?.BlockedCount?.Value.GetValueOrDefault();
    if (blockedCount > 0)
      textComponent.text = $"Blocked, Total: {blockedCount}";
    yield return (object) null;
  }

  private static IEnumerator SetTotalFriends()
  {
    while ((UnityEngine.Object) GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Center/SocialFriendsTablePage(Clone)/StaticContent/Header/TextMeshPro") == (UnityEngine.Object) null)
      yield return (object) null;
    GameObject friendsText = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Center/SocialFriendsTablePage(Clone)/StaticContent/Header/TextMeshPro");
    friendsText.GetComponent<TextMeshPro>().text = $"My Friends   Total: {((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel.CurrentRoom.Me).SocialProfile).Stats.NumberOfFriends.Value}";
    yield return (object) null;
  }

  private static IEnumerator OnlineFilter()
  {
    FilterFriendsList.working = true;
    yield return (object) new WaitForSeconds(1f);
    GameObject nav = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Center/SocialFriendsTablePage(Clone)/AnimatedContent/Body/ButtonList/SocialFriendsTableView/BtnRow_Online (4)/Grid");
    GameObject socialpage = GameObject.Find("UI/TabletUI/TranslationContainer/ScalingContainer/Panes/Pane_Center/SocialFriendsTablePage(Clone)/");
    GameObject[] directChildren = new GameObject[0];
    int i = 0;
    int friendsCount = ((SocialProfile) ((RoomUser) BIG_STATIC_SINGLETONS.localUserModel.CurrentUser).Profile).Stats.NumberOfFriends.Value;
    while (directChildren.Length < friendsCount + 2 && i <= 200)
    {
      yield return (object) new WaitForSeconds(1f);
      ++i;
      SocialFriendsTablePage socialFriendsTablePage = socialpage.GetComponent<SocialFriendsTablePage>();
      if ((UnityEngine.Object) nav == (UnityEngine.Object) null)
        yield break;
      directChildren = FilterFriendsList.GetDirectChildren(nav);
      List<(GameObject, string)> onlineFriends = new List<(GameObject, string)>();
      List<(GameObject, string)> offlineFriends = new List<(GameObject, string)>();
      GameObject[] gameObjectArray = directChildren;
      for (int index = 0; index < gameObjectArray.Length; ++index)
      {
        GameObject child = gameObjectArray[index];
        if (!((UnityEngine.Object) child == (UnityEngine.Object) null))
        {
          BigTableViewCell bigTableViewCell = child.GetComponent<BigTableViewCell>();
          if (!((UnityEngine.Object) bigTableViewCell == (UnityEngine.Object) null))
          {
            SocialGraphItem socialGraphItem = socialFriendsTablePage.dataSource.getDataForRow(((UITableView) socialFriendsTablePage.tableView).IndexPathForCell(bigTableViewCell));
            string username = ((SocialProfile) socialGraphItem?.Profile)?.Username ?? "";
            SocialGraphItem socialGraphItem1 = socialGraphItem;
            if (socialGraphItem1 != null && socialGraphItem1.Profile?.Presence?.Status.GetValueOrDefault() == 3)
              onlineFriends.Add((child, username));
            else
              offlineFriends.Add((child, username));
            bigTableViewCell = (BigTableViewCell) null;
            socialGraphItem = (SocialGraphItem) null;
            username = (string) null;
            child = (GameObject) null;
          }
        }
      }
      gameObjectArray = (GameObject[]) null;
      onlineFriends.Sort((Comparison<(GameObject, string)>) ((a, b) => string.Compare(a.username, b.username, StringComparison.OrdinalIgnoreCase)));
      offlineFriends.Sort((Comparison<(GameObject, string)>) ((a, b) => string.Compare(a.username, b.username, StringComparison.OrdinalIgnoreCase)));
      int siblingIndex = 0;
      foreach ((GameObject gameObject, string _) in onlineFriends)
      {
        gameObject.transform.SetSiblingIndex(siblingIndex++);
        gameObject = (GameObject) null;
      }
      foreach ((GameObject gameObject, string _) in offlineFriends)
      {
        gameObject.transform.SetSiblingIndex(siblingIndex++);
        gameObject = (GameObject) null;
      }
      ((UITableView) socialFriendsTablePage.tableView).UpdateList();
      socialFriendsTablePage.dataSource.LoadItemsWithCursor(new IndexPath(0, i * 10, i * 10));
      ((UITableView) socialFriendsTablePage.tableView).UpdateList();
      socialFriendsTablePage = (SocialFriendsTablePage) null;
      onlineFriends = (List<(GameObject, string)>) null;
      offlineFriends = (List<(GameObject, string)>) null;
    }
    FilterFriendsList.working = false;
  }

  private static GameObject[] GetDirectChildren(GameObject parent)
  {
    int childCount = parent.transform.childCount;
    GameObject[] directChildren = new GameObject[childCount];
    for (int index = 0; index < childCount; ++index)
      directChildren[index] = parent.transform.GetChild(index).gameObject;
    return directChildren;
  }

  [HarmonyPatch(typeof (NavMenu_Social), "OnSocialFriendsButtonPressed")]
  private static class NavMenu_Social_OnSocialFriendsButtonPressed_Patch
  {
    private static void Postfix(NavMenu_Social __instance)
    {
      if (!FilterFriendsList.init)
        return;
      if (!FilterFriendsList.working)
        MelonCoroutines.Start(FilterFriendsList.OnlineFilter());
      MelonCoroutines.Start(FilterFriendsList.SetTotalFriends());
    }
  }

  [HarmonyPatch(typeof (NavMenu_Social), "OnBlockedButtonPressed")]
  private static class NavMenu_Social_OnBlockedButtonPressed_Patch
  {
    private static void Postfix(NavMenu_Social __instance)
    {
      if (!FilterFriendsList.init)
        return;
      MelonCoroutines.Start(FilterFriendsList.SetTotalBlocked());
    }
  }

  [HarmonyPatch(typeof (UserProfile_List), "Show")]
  private static class UserProfile_List_OnShowComplete
  {
    private static void Postfix(UserProfile_List __instance)
    {
      if (!FilterFriendsList.init)
        return;
      MelonCoroutines.Start(FilterFriendsList.SetLastOnline(__instance));
    }
  }
}
