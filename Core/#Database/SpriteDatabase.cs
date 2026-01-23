using System;
using UnityEngine;

[Serializable]
public class SpriteDatabase : Database<KeyValueWrapper<string, Sprite>>
{
    [field: SerializeField] public EntitySprites Entities;
}

[Serializable]
public class EntitySprites
{
    [field: SerializeField] public Sprite GameEventEntity;

    [field: SerializeField] public Sprite EntityBase;
}
