using UnityEngine;

/// <summary>
/// Thông số của một loại quái. Để riêng ra ScriptableObject để chỉnh cân bằng
/// game bằng asset, không phải sửa code hay sửa từng prefab.
/// </summary>
[CreateAssetMenu(fileName = "EnemyStats", menuName = "Tower Defense/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Nhận dạng")]
    public string displayName = "Quái";

    [Tooltip("Boss thì máu và thưởng cao hơn hẳn, đi chậm hơn.")]
    public bool isBoss = false;

    [Header("Chỉ số")]
    [Min(1)] public int maxHealth = 30;
    [Min(0.1f)] public float moveSpeed = 2f;

    [Tooltip("Số tiền nhận được khi giết con quái này.")]
    [Min(0)] public int coinReward = 10;

    [Tooltip("Số máu Base bị trừ nếu để con này đi lọt.")]
    [Min(1)] public int damageToBase = 1;
}
