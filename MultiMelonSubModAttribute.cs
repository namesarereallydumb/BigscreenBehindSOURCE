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
