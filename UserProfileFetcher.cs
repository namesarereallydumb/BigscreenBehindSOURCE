// Decompiled with JetBrains decompiler
// Type: UserProfileFetcher
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;
using UnityEngine.Networking;

#nullable enable
public class UserProfileFetcher
{
  private readonly HttpClient _httpClient;
  private readonly string accessToken;
  private readonly string bearerToken;
  private readonly string apiUrl;
  private readonly string id;

  public UserProfileFetcher(string accessToken, string bearerToken, string apiUrl, string id)
  {
    this.accessToken = accessToken;
    this.bearerToken = bearerToken;
    this.apiUrl = apiUrl;
    this.id = id;
    this._httpClient = new HttpClient();
  }

  public IEnumerator GetUserProfileCoroutine(System.Action<UserProfileFetcher.UserProfile> onComplete)
  {
    string url = $"{this.apiUrl}/social/profile/{this.id}";
    UnityWebRequest request = UnityWebRequest.Get(url);
    request.SetRequestHeader("x-access-token", this.accessToken);
    request.SetRequestHeader("Authorization", "Bearer " + this.bearerToken);
    request.SetRequestHeader("Accept", "application/json");
    yield return (object) request.SendWebRequest();
    if (request.result == UnityWebRequest.Result.Success)
    {
      string json = request.downloadHandler.text;
      try
      {
        UserProfileFetcher.UserProfile profile = JsonConvert.DeserializeObject<UserProfileFetcher.UserProfile>(json);
        System.Action<UserProfileFetcher.UserProfile> action = onComplete;
        if (action != null)
          action(profile);
        profile = (UserProfileFetcher.UserProfile) null;
      }
      catch (System.Exception ex)
      {
        Debug.LogError((Il2CppSystem.Object) ("JSON parse error: " + ex.Message));
        System.Action<UserProfileFetcher.UserProfile> action = onComplete;
        if (action != null)
          action((UserProfileFetcher.UserProfile) null);
      }
      json = (string) null;
    }
    else
    {
      Debug.LogError((Il2CppSystem.Object) ("Error fetching profile: " + request.error));
      System.Action<UserProfileFetcher.UserProfile> action = onComplete;
      if (action != null)
        action((UserProfileFetcher.UserProfile) null);
    }
    request.Dispose();
  }

  public class UserProfile
  {
    public long UpdatedAt { get; set; }

    public List<string> Badges { get; set; }

    public string SocialId { get; set; }

    public long CreatedAt { get; set; }

    public long AccountCreatedAt { get; set; }

    public bool IsVerified { get; set; }

    public string Username { get; set; }

    public string OculusId { get; set; }

    public string LargeImageUrl { get; set; }

    public UserProfileFetcher.Stats Stats { get; set; }

    public int SocialGraphType { get; set; }

    public UserProfileFetcher.Presence Presence { get; set; }

    public bool PendingFriendRequest { get; set; }
  }

  public class Stats
  {
    public int FriendsCount { get; set; }

    public int FollowersCount { get; set; }

    public long LastOnlineDate { get; set; }
  }

  public class Presence
  {
    public string Status { get; set; }
  }
}
