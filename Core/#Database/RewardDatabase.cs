using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public partial class RewardDatabase 
{
    public static RewardDatabase Instance => PRUnitySDK.Database.Rewards;

    [SerializeField] private List<KeyValueWrapper<string, RewardAction>> actions = new();
    [SerializeField] private List<KeyValueWrapper<string, RewardResource>> resources = new();
    [SerializeField] private List<KeyValueWrapper<string, RewardItemBase>> items = new();

    #region PublicAPI

    public IReadOnlyCollection<KeyValueWrapper<string, RewardAction>> Actions => actions;
    public IReadOnlyCollection<KeyValueWrapper<string, RewardResource>> Resources => resources;
    public IReadOnlyCollection<KeyValueWrapper<string, RewardItemBase>> Items => items;
    #endregion
}
