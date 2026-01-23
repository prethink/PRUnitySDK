using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
public partial class GameSessionSettings
{
    [field: Header("Количество локальных игроков")]
    [field: Range(1, 10)]
    [field: SerializeField] public int PlayersCount { get; private set; }


    [field: Header("Настройка ботов")]
    [field: Range(0, 100)]
    [field: SerializeField] public int BotCounts { get; private set; }

    [field: Range(0, 100)]
    [field: SerializeField] public int BotMobileCounts { get; private set; }
    [field: SerializeField] public bool isActiveDebugLog { get; private set; }
    [field: SerializeField] public float WaitingSpawn { get; private set; }
}
