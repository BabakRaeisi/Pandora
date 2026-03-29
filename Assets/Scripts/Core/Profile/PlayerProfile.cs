// PlayerProfile.cs
using System;

[Serializable]
public class PlayerProfile
{
    public string phoneNumber;  // stable identity key — used as unique player ID
    public string playerName;
    public int    age;
    public int    avatarIndex;
    public string gender;
}