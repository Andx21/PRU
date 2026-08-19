using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy đi theo waypoint về Base.
///
/// Enemy sử dụng Object Pooling:
/// - Không Destroy.
/// - Khi chết hoặc tới Base -> SetActive(false).
/// - Khi spawn lại -> SetPath() reset toàn bộ trạng thái.
/// - Animator tự chạy Default State khi Enemy được bật.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Thông số")]
    [SerializeField] private EnemyStats stats;

    [Header("Thanh máu")]
    [Tooltip("Kéo Transform phần ruột thanh máu vào đây.")]
    [SerializeField] private Transform healthBarFill;

    [Header("Hiệu ứng chết")]
    [Tooltip("Thời gian mờ dần trước khi trả về Pool.")]
    [SerializeField] private float deathFadeDuration = 0.6f;

    [Tooltip("Khoảng cách lún xuống trong lúc chết.")]
    [SerializeField] private float deathSinkDistance = 0.15f;

    // =========================================================
    // ACTIVE ENEMIES
    // =========================================================

    /// <summary>
    /// Danh sách tất cả Enemy đang active trên map.
    /// Tower có thể sử dụng danh sách này để tìm mục tiêu.
    /// </summary>
    private static readonly List<Enemy> activeEnemies =
        new List<Enemy>();

    public static IReadOnlyList<Enemy> ActiveEnemies =>
        activeEnemies;

    // =========================================================
    // STATE
    // =========================================================

    private Transform[] waypoints;

    private int currentWaypointIndex;

    private bool isDying;

    private int currentHealth;

    private Animator animator;

    private EnemySpawner spawner;

    private SpriteRenderer spriteRenderer;

    // =========================================================
    // PUBLIC PROPERTIES
    // =========================================================

    public EnemyStats Stats => stats;

    public bool IsAlive =>
        !isDying && currentHealth > 0;

    /// <summary>
    /// Enemy đã đi được bao xa trên đường.
    /// Giá trị càng lớn thì càng gần Base.
    /// </summary>
    public float PathProgress { get; private set; }

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        animator = GetComponent<Animator>();

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Được gọi khi Enemy được SetActive(true).
    ///
    /// Không gọi Animator.Play() ở đây.
    /// Animator sẽ tự chạy Default State của Animator Controller.
    /// </summary>
    private void OnEnable()
    {
        if (!activeEnemies.Contains(this))
        {
            activeEnemies.Add(this);
        }

        // Reset trigger Die nếu nó đang còn trạng thái cũ.
        if (animator != null)
        {
            animator.ResetTrigger("Die");
        }
    }

    /// <summary>
    /// Được gọi khi Enemy được SetActive(false).
    /// </summary>
    private void OnDisable()
    {
        activeEnemies.Remove(this);
    }

    /// <summary>
    /// Xóa danh sách Enemy khi bắt đầu scene mới.
    /// </summary>
    public static void ClearRegistry()
    {
        activeEnemies.Clear();
    }

    // =========================================================
    // SPAWNER
    // =========================================================

    public void SetSpawner(EnemySpawner enemySpawner)
    {
        spawner = enemySpawner;
    }

    // =========================================================
    // SET PATH
    // =========================================================

    /// <summary>
    /// Thiết lập đường đi và reset Enemy về trạng thái ban đầu.
    ///
    /// Hàm này được gọi khi Enemy còn inactive.
    /// Vì vậy không gọi Animator.Play() ở đây.
    /// </summary>
    public void SetPath(Transform[] path)
    {
        waypoints = path;

        currentWaypointIndex = 0;

        PathProgress = 0f;

        isDying = false;

        // -----------------------------------------------------
        // RESET HEALTH
        // -----------------------------------------------------

        currentHealth =
            stats != null
                ? stats.maxHealth
                : 30;

        UpdateHealthBar();

        // -----------------------------------------------------
        // RESET SPRITE ALPHA
        // -----------------------------------------------------

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;

            color.a = 1f;

            spriteRenderer.color = color;
        }

        // -----------------------------------------------------
        // RESET ANIMATOR TRIGGER
        // -----------------------------------------------------

        if (animator != null)
        {
            animator.ResetTrigger("Die");
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (isDying)
            return;

        if (waypoints == null ||
            waypoints.Length == 0)
        {
            return;
        }

        MoveToWaypoint();
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void MoveToWaypoint()
    {
        if (currentWaypointIndex >= waypoints.Length)
        {
            ReachBase();

            return;
        }

        Transform target =
            waypoints[currentWaypointIndex];

        if (target == null)
            return;

        float speed =
            stats != null
                ? stats.moveSpeed
                : 2f;

        // -----------------------------------------------------
        // MOVE
        // -----------------------------------------------------

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );

        float distanceToTarget =
            Vector3.Distance(
                transform.position,
                target.position
            );

        // -----------------------------------------------------
        // FLIP
        // -----------------------------------------------------

        if (spriteRenderer != null)
        {
            float dx =
                target.position.x -
                transform.position.x;

            if (Mathf.Abs(dx) > 0.01f)
            {
                spriteRenderer.flipX =
                    dx < 0f;
            }
        }

        // -----------------------------------------------------
        // PATH PROGRESS
        // -----------------------------------------------------

        PathProgress =
            currentWaypointIndex +
            (1f -
             Mathf.Clamp01(distanceToTarget));

        // -----------------------------------------------------
        // REACHED WAYPOINT
        // -----------------------------------------------------

        if (distanceToTarget < 0.05f)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >=
                waypoints.Length)
            {
                ReachBase();
            }
        }
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(int amount)
    {
        if (isDying)
            return;

        if (amount <= 0)
            return;

        currentHealth -= amount;

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            currentHealth = 0;

            Die();
        }
    }

    // =========================================================
    // DIE
    // =========================================================

    private void Die()
    {
        if (isDying)
            return;

        isDying = true;

        // -----------------------------------------------------
        // COIN REWARD
        // -----------------------------------------------------

        if (stats != null)
        {
            GameManager.Instance?.AddCoins(
                stats.coinReward
            );
        }

        // -----------------------------------------------------
        // SOUND
        // -----------------------------------------------------

        AudioManager.Instance?.PlayEnemyDeath();

        // -----------------------------------------------------
        // DIE ANIMATION
        // -----------------------------------------------------

        if (animator != null)
        {
            animator.ResetTrigger("Die");

            animator.SetTrigger("Die");
        }

        // -----------------------------------------------------
        // FADE + SINK
        // -----------------------------------------------------

        StartCoroutine(
            ReturnToPoolAfterDeath()
        );
    }

    // =========================================================
    // DEATH EFFECT
    // =========================================================

    private IEnumerator ReturnToPoolAfterDeath()
    {
        Vector3 startPosition =
            transform.position;

        float elapsed = 0f;

        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    deathFadeDuration
                );

            // -------------------------------------------------
            // FADE
            // -------------------------------------------------

            if (spriteRenderer != null)
            {
                Color color =
                    spriteRenderer.color;

                color.a = 1f - t;

                spriteRenderer.color = color;
            }

            // -------------------------------------------------
            // SINK
            // -------------------------------------------------

            transform.position =
                startPosition +
                new Vector3(
                    0f,
                    -deathSinkDistance * t,
                    0f
                );

            yield return null;
        }

        ReturnToPool();
    }

    // =========================================================
    // REACH BASE
    // =========================================================

    private void ReachBase()
    {
        if (isDying)
            return;

        isDying = true;

        int damage =
            stats != null
                ? stats.damageToBase
                : 1;

        GameManager.Instance?.TakeDamage(
            damage
        );

        // Không chạy animation Die.
        // Enemy tới Base -> trả Pool ngay.
        ReturnToPool();
    }

    // =========================================================
    // RETURN TO POOL
    // =========================================================

    public void ReturnToPool()
    {
        StopAllCoroutines();

        isDying = true;

        if (spawner != null)
        {
            spawner.ReturnEnemyToPool(
                gameObject
            );
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // =========================================================
    // HEALTH BAR
    // =========================================================

    private void UpdateHealthBar()
    {
        if (healthBarFill == null)
            return;

        if (stats == null)
            return;

        if (stats.maxHealth <= 0)
            return;

        float ratio =
            Mathf.Clamp01(
                (float)currentHealth /
                stats.maxHealth
            );

        Vector3 scale =
            healthBarFill.localScale;

        scale.x = ratio;

        healthBarFill.localScale =
            scale;
    }
}