using UnityEngine;

/// <summary>Ba kiểu đánh theo đề bài: tầm xa, tầm gần, đánh lan.</summary>
public enum TowerAttackType
{
    /// <summary>Tầm xa — bắn đạn bay, trúng một mục tiêu.</summary>
    Ranged,

    /// <summary>Tầm gần — đánh trúng ngay lập tức, không có đạn, sát thương cao, tầm ngắn.</summary>
    Melee,

    /// <summary>Đánh lan — đạn nổ, gây sát thương cho mọi quái trong bán kính nổ.</summary>
    Splash
}

/// <summary>
/// Thông số một loại tháp. Cả 3 loại tháp dùng CHUNG một prefab; sprite, tầm,
/// sát thương và kiểu đánh đều lấy từ asset này. Muốn thêm loại tháp thứ 4 thì
/// chỉ cần tạo thêm một asset, không phải viết thêm class.
/// </summary>
[CreateAssetMenu(fileName = "TowerStats", menuName = "Tower Defense/Tower Stats")]
public class TowerStats : ScriptableObject
{
    [Header("Nhận dạng")]
    public string displayName = "Tháp";

    [TextArea]
    public string description = "";

    [Tooltip("Ảnh tháp. Tower tự gán vào SpriteRenderer lúc được xây.")]
    public Sprite towerSprite;

    [Tooltip("Ảnh nhỏ hiển thị trên nút mua ở thanh shop.")]
    public Sprite shopIcon;

    [Header("Chiến đấu")]
    public TowerAttackType attackType = TowerAttackType.Ranged;

    [Min(1)] public int damage = 10;

    [Tooltip("Bán kính bắn, tính bằng unit của Unity.")]
    [Min(0.1f)] public float range = 3f;

    [Tooltip("Số phát bắn mỗi giây.")]
    [Min(0.1f)] public float fireRate = 1f;

    [Header("Chỉ dùng cho kiểu Splash")]
    [Tooltip("Bán kính nổ lan. Kiểu khác thì bỏ qua.")]
    [Min(0f)] public float splashRadius = 1.2f;

    [Header("Đạn (Ranged / Splash)")]
    [Tooltip("Prefab viên đạn. Kiểu Melee không cần.")]
    public GameObject projectilePrefab;

    [Header("Giá")]
    [Min(0)] public int cost = 50;

    /// <summary>Giây giữa 2 phát bắn.</summary>
    public float FireCooldown => 1f / Mathf.Max(0.1f, fireRate);
}
