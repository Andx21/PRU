using UnityEngine;

/// <summary>
/// Viên đạn bay tới mục tiêu. Cũng được pool — trúng đích thì tắt đi chứ không Destroy.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bay")]
    [SerializeField] private float speed = 10f;

    [Tooltip("Bay quá lâu mà không tới đích thì tự thu hồi, tránh đạn lạc nằm mãi ngoài map.")]
    [SerializeField] private float maxLifetime = 3f;

    [Tooltip("Xoay đầu đạn theo hướng bay. Đạn tròn thì tắt đi.")]
    [SerializeField] private bool rotateTowardsTarget = true;

    private Tower owner;
    private Enemy target;
    private GameObjectPool pool;
    private Vector3 lastKnownTargetPosition;
    private float lifetime;

    /// <summary>Tower gọi ngay trước khi bật viên đạn lên.</summary>
    public void Launch(Tower tower, Enemy enemyTarget, GameObjectPool owningPool)
    {
        owner = tower;
        target = enemyTarget;
        pool = owningPool;
        lifetime = 0f;

        if (enemyTarget != null)
        {
            lastKnownTargetPosition = enemyTarget.transform.position;
        }
    }

    private void Update()
    {
        lifetime += Time.deltaTime;

        if (lifetime >= maxLifetime)
        {
            ReturnToPool();
            return;
        }

        // Mục tiêu chết giữa chừng thì đạn vẫn bay nốt tới chỗ cũ rồi mới nổ —
        // quan trọng với đạn lan, vì chỗ đó thường còn quái khác đứng gần.
        if (target != null && target.IsAlive)
        {
            lastKnownTargetPosition = target.transform.position;
        }

        Vector3 direction = lastKnownTargetPosition - transform.position;
        float step = speed * Time.deltaTime;

        if (direction.magnitude <= step)
        {
            Hit();
            return;
        }

        transform.position += direction.normalized * step;

        if (rotateTowardsTarget)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void Hit()
    {
        if (owner != null)
        {
            owner.ApplyHit(target, lastKnownTargetPosition);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        target = null;
        owner = null;

        if (pool != null)
        {
            pool.Return(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
