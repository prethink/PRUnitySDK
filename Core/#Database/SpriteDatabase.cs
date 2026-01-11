using System;
using UnityEngine;

[Serializable]
public class SpriteDatabase : Database<KeyValueWrapper<string, Sprite>>
{
    public override string DataBaseKey => nameof(SpriteDatabase);
}
