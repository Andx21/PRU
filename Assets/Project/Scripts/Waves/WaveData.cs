using System;
using UnityEngine;

/// <summary>Một nhóm quái trong đợt: loại nào, mấy con, cách nhau bao lâu.</summary>
[Serializable]
public class WaveEntry
{
    [Tooltip("Prefab quái. Cùng một prefab dùng lại ở nhiều đợt thì vẫn chung một pool.")]
    public GameObject enemyPrefab;

    [Min(1)] public int count = 5;

    [Tooltip("Giãn cách giữa 2 con trong cùng nhóm (giây).")]
    [Min(0.05f)] public float spawnInterval = 0.8f;
}

/// <summary>Một đợt quái, gồm nhiều nhóm chạy lần lượt.</summary>
[Serializable]
public class Wave
{
    public string waveName = "Đợt";

    [Tooltip("Nghỉ bao lâu trước khi đợt này bắt đầu (giây).")]
    [Min(0f)] public float delayBeforeWave = 3f;

    public WaveEntry[] entries;
}

/// <summary>
/// Cấu hình toàn bộ các đợt của màn. Đề bài yêu cầu 5 đợt, nhưng để mảng cho
/// linh hoạt — muốn thêm đợt thì thêm phần tử, không phải sửa code.
/// </summary>
[CreateAssetMenu(fileName = "WaveData", menuName = "Tower Defense/Wave Data")]
public class WaveData : ScriptableObject
{
    public Wave[] waves;

    public int WaveCount => waves != null ? waves.Length : 0;
}
