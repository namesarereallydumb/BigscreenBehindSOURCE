// Decompiled with JetBrains decompiler
// Type: BigscreenBehind.MultiMelonSubModAttribute
// Assembly: BigscreenBehind, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8CD1E9EE-0987-4B29-93F8-7443D82AE0EE
// Assembly location: C:\Users\CASHM\Downloads\BigscreenBehind.dll

using System;

#nullable enable
namespace BigscreenBehind;

public class MultiMelonSubModAttribute : Attribute
{
  public string Name;
  public string Version;
  public string Author;

  public MultiMelonSubModAttribute(string name, string version, string author)
  {
    this.Name = name;
    this.Version = version;
    this.Author = author;
  }
}
