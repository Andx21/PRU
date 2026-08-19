using UnityEngine;

/// <summary>
/// Tháp đã xây. Tự tìm mục tiêu trong tầm rồi bắn theo nhịp.
///
/// Chỉ có MỘT class tháp cho cả 3 loại — khác nhau nằm hết ở
/// <see cref="TowerStats"/>. Thêm loại tháp mới chỉ là tạo thêm asset.
/// </summary>
public class Tower : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    [Tooltip("Điểm đạn bay ra. Để trống thì lấy tâm tháp.")]
    [SerializeField] private Transform firePoint;

    private TowerStats stats;
    private float fireTimer;
    private GameObjectPool projectilePool;
    private Transform projectileContainer;

    public TowerStats Stats => stats;

    /// <summary>BuildManager gọi ngay sau khi tạo tháp.</summary>
    public void Initialize(TowerStats towerStats)
    {
        stats = towerStats;

        if (stats == null)
        {
            Debug.LogError("[Tower] Được khởi tạo mà không có TowerStats.", this);
            return;
        }

        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
        }

        if (bodyRenderer != null && stats.towerSprite != null)
        {
            bodyRenderer.sprite = stats.towerSprite;
        }

        // Bắn được ngay phát đầu, không bắt người chơi chờ hết cooldown.
        fireTimer = 0f;

        if (stats.attackType != TowerAttackType.Melee && stats.projectilePrefab != null)
        {
            // Đạn cũng pool như quái. Bắn 1 phát/giây suốt màn mà Instantiate/Destroy
            // thì sinh rác liên tục.
            var container = new GameObject($"{name}_Projectiles");
            container.transform.SetParent(transform, false);
            projectileContainer = container.transform;

            projectilePool = new GameObjectPool(stats.projectilePrefab, 6, projectileContainer);
        }
    }

    private void Update()
    {
        if (stats == null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            return;

        fireTimer -= Time.deltaTime;

        Enemy target = FindTarget();

        if (target == null)
            return;

        AimAt(target);

        if (fireTimer <= 0f)
        {
            Fire(target);
            fireTimer = stats.FireCooldown;
        }
    }

    /// <summary>
    /// Chọn con quái đi xa nhất trên đường mà vẫn nằm trong tầm — con gần Base
    /// nhất là con nguy hiểm nhất, ưu tiên diệt trước.
    /// </summary>
    private Enemy FindTarget()
    {
        Enemy best = null;
        float bestProgress = float.MinValue;

        var enemies = Enemy.ActiveEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];

            if (enemy == null || !enemy.IsAlive)
                continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance > stats.range)
                continue;

            if (enemy.PathProgress > bestProgress)
            {
                bestProgress = enemy.PathProgress;
                best = enemy;
            }
        }

        return best;
    }

    private void AimAt(Enemy target)
    {
        if (bodyRenderer == null)
            return;

        // Xoay thì sprite tháp trông kỳ, chỉ lật trái/phải cho tự nhiên.
        float dx = target.transform.position.x - transform.position.x;

        if (Mathf.Abs(dx) > 0.05f)
        {
            bodyRenderer.flipX = dx < 0f;
        }
    }

    private void Fire(Enemy target)
    {
        AudioManager.Instance?.PlayTowerShoot();

        if (stats.attackType == TowerAttackType.Melee)
        {
            // Tầm gần: trúng ngay, không cần đạn bay.
            target.TakeDamage(stats.damage);
            return;
        }

        if (projectilePool == null)
        {
            // Thiếu prefab đạn thì vẫn phải gây sát thương, không thì tháp thành vô dụng.
            ApplyHit(target, target.transform.position);
            return;
        }

        GameObject projectileObject = projectilePool.Get();

        if (projectileObject == null)
            return;

        projectileObject.transform.position = firePoint != null
            ? firePoint.position
            : transform.position;

        Bullet bullet = projectileObject.GetComponent<Bullet>();

        if (bullet != null)
        {
            bullet.Launch(this, target, projectilePool);
        }

        projectileObject.SetActive(true);
    }

    /// <summary>Bullet gọi lại khi trúng đích. Gom logic sát thương về một chỗ.</summary>
    public void ApplyHit(Enemy directTarget, Vector3 hitPosition)
    {
        if (stats == null)
            return;

        if (stats.attackType == TowerAttackType.Splash)
        {
            ApplySplashDamage(hitPosition);
            return;
        }

        if (directTarget != null && directTarget.IsAlive)
        {
            directTarget.TakeDamage(stats.damage);
        }
    }

    private void ApplySplashDamage(Vector3 center)
    {
        var enemies = Enemy.ActiveEnemies;

        // Duyệt ngược: TakeDamage có thể giết quái, mà quái chết thì tự gỡ khỏi
        // danh sách ngay trong vòng lặp — duyệt xuôi sẽ nhảy cóc mất phần tử.
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];

            if (enemy == null || !enemy.IsAlive)
                continue;

            if (Vector2.Distance(center, enemy.transform.position) <= stats.splashRadius)
            {
                enemy.TakeDamage(stats.damage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (stats == null)
            return;

        Gizmos.color = Color.cyan;
        DrawCircle(transform.position, stats.range);

        if (stats.attackType == TowerAttackType.Splash)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            DrawCircle(transform.position, stats.splashRadius);
        }
    }

    private static void DrawCircle(Vector3 center, float radius, int segments = 40)
    {
        Vector3 previous = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
