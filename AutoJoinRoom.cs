// Decompiled with JetBrains decompiler
// Type: AutoJoinRoom
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;
using UnityEngine.Networking;

#nullable enable
public class AutoJoinRoom
{
  private readonly HttpClient _httpClient;
  private readonly string accessToken;
  private readonly string bearerToken;
  private readonly string apiUrl;
  private readonly string roomID;

  public AutoJoinRoom(string accessToken, string bearerToken, string apiUrl, string roomID)
  {
    this.accessToken = accessToken;
    this.bearerToken = bearerToken;
    this.apiUrl = apiUrl;
    this.roomID = roomID;
    this._httpClient = new HttpClient();
  }

  public IEnumerator GetRoomCoroutine(
    System.Action<AutoJoinRoom.Room> onComplete,
    System.Action<long> onStatusCode = null)
  {
    string url = $"{this.apiUrl}/room/{this.roomID}";
    UnityWebRequest request = UnityWebRequest.Get(url);
    request.SetRequestHeader("x-access-token", this.accessToken);
    request.SetRequestHeader("Authorization", "Bearer " + this.bearerToken);
    request.SetRequestHeader("Accept", "application/json");
    yield return (object) request.SendWebRequest();
    System.Action<long> action1 = onStatusCode;
    if (action1 != null)
      action1(request.responseCode);
    if (request.result == UnityWebRequest.Result.Success)
    {
      string json = request.downloadHandler.text;
      try
      {
        AutoJoinRoom.Room profile = JsonConvert.DeserializeObject<AutoJoinRoom.Room>(json);
        System.Action<AutoJoinRoom.Room> action2 = onComplete;
        if (action2 != null)
          action2(profile);
        profile = (AutoJoinRoom.Room) null;
      }
      catch (System.Exception ex)
      {
        Debug.LogError((Il2CppSystem.Object) ("JSON parse error: " + ex.Message));
        System.Action<AutoJoinRoom.Room> action3 = onComplete;
        if (action3 != null)
          action3((AutoJoinRoom.Room) null);
      }
      json = (string) null;
    }
    else
    {
      Debug.LogError((Il2CppSystem.Object) ("Error fetching profile: " + request.error));
      System.Action<AutoJoinRoom.Room> action4 = onComplete;
      if (action4 != null)
        action4((AutoJoinRoom.Room) null);
    }
    request.Dispose();
  }

  public class Room
  {
    public string name { get; set; }

    public string description { get; set; }

    public string category { get; set; }

    public string environmentId { get; set; }

    public int size { get; set; }

    public string version { get; set; }

    public string roomType { get; set; }

    public string visibility { get; set; }

    public string status { get; set; }

    public List<AutoJoinRoom.RemoteUser> remoteUsers { get; set; }
  }

  public class RemoteUser
  {
    public bool isAdmin { get; set; }

    public bool isOwner { get; set; }

    public string version { get; set; }

    public string userSessionId { get; set; }

    public string legacyUserId { get; set; }

    public int seatIndex { get; set; }

    public System.DateTime createdAt { get; set; }

    public AutoJoinRoom.SocialProfile socialProfile { get; set; }
  }

  public class SocialProfile
  {
    public long updatedAt { get; set; }

    public List<string> badges { get; set; }

    public string socialId { get; set; }

    public long createdAt { get; set; }

    public long accountCreatedAt { get; set; }

    public bool isVerified { get; set; }

    public string username { get; set; }

    public string oculusId { get; set; }

    public string largeImageUrl { get; set; }

    public AutoJoinRoom.Stats stats { get; set; }
  }

  public class Stats
  {
    public int friendsCount { get; set; }

    public int followersCount { get; set; }

    public long lastOnlineDate { get; set; }
  }

  [Serializable]
  public class JoinRoomPayload
  {
    public string roomId;
    public string version;
  }
}
