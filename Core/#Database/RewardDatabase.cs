using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RewardDatabase 
{
    [SerializeField] private List<KeyValueWrapper<string, RewardAction>> actions;
    [SerializeField] private List<KeyValueWrapper<string, RewardResource>> resources;
    [SerializeField] private List<KeyValueWrapper<string, RewardItem>> items;

    #region PublicAPI

    public IReadOnlyCollection<KeyValueWrapper<string, RewardAction>> Actions => actions;
    public IReadOnlyCollection<KeyValueWrapper<string, RewardResource>> Resources => resources;
    public IReadOnlyCollection<KeyValueWrapper<string, RewardItem>> Items => items;

    #endregion
}