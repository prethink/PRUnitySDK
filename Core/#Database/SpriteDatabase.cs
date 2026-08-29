using System;
using UnityEngine;

[Serializable]
[DatabaseExternalEditor(
    "PRUnitySDK/Windows/Common",
    WindowName = "Общее",
    Description = "Действия, спрайты и звуки правятся там.")]
public class SpriteDatabase : Database<KeyValueWrapper<string, Sprite>>
{
    [field: SerializeField] public EntitySprites Entities;
    public static SpriteDatabase Instance => PRUnitySDK.Database.Sprites;
}

[Serializable]
public class EntitySprites
{
    [field: SerializeField] public Sprite GameEventEntity;

    [field: SerializeField] public Sprite EntityBase;
}
