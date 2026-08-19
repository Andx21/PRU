using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý Wave và Object Pool của Enemy.
///
/// Mỗi loại Enemy có một Pool riêng.
/// Enemy không bị Destroy.
/// Khi chết hoặc tới Base -> SetActive(false).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Đường đi")]
    [SerializeField] private Transform spawnPoint;

    [SerializeField] private WaypointPath waypointPath;

    [Header("Các đợt")]
    [SerializeField] private WaveData waveData;

    [Header("Pooling")]
    [Tooltip("Số Enemy tạo sẵn cho mỗi loại.")]
    [SerializeField] private int poolSizePerType = 10;

    [Tooltip(
        "Nếu bật, Pool tự tạo thêm Enemy khi hết Enemy rảnh."
    )]
    [SerializeField] private bool poolCanGrow = true;

    // =========================================================
    // POOLS
    // =========================================================

    /// <summary>
    /// Một Pool cho mỗi loại Enemy.
    /// </summary>
    private readonly List<GameObjectPool> pools =
        new List<GameObjectPool>();

    // =========================================================
    // WAVE
    // =========================================================

    /// <summary>
    /// Wave hiện tại.
    /// </summary>
    public int CurrentWave { get; private set; }

    /// <summary>
    /// Tổng số Wave.
    /// </summary>
    public int TotalWaves =>
        waveData != null
            ? waveData.WaveCount
            : 0;

    /// <summary>
    /// Event gửi thông tin Wave cho UI.
    /// </summary>
    public event Action<int, int> OnWaveChanged;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (!ValidateSetup())
            return;

        BuildPools();

        StartCoroutine(
            RunWaves()
        );
    }

    // =========================================================
    // VALIDATE
    // =========================================================

    private bool ValidateSetup()
    {
        if (spawnPoint == null)
        {
            Debug.LogError(
                "[EnemySpawner] Chưa gán Spawn Point.",
                this
            );

            return false;
        }

        if (
            waypointPath == null ||
            waypointPath.waypoints == null ||
            waypointPath.waypoints.Length == 0
        )
        {
            Debug.LogError(
                "[EnemySpawner] Chưa gán Waypoint Path hoặc path rỗng.",
                this
            );

            return false;
        }

        if (
            waveData == null ||
            waveData.WaveCount == 0
        )
        {
            Debug.LogError(
                "[EnemySpawner] Chưa gán Wave Data hoặc chưa có Wave.",
                this
            );

            return false;
        }

        return true;
    }

    // =========================================================
    // BUILD POOLS
    // =========================================================

    private void BuildPools()
    {
        foreach (Wave wave in waveData.waves)
        {
            if (wave == null)
                continue;

            if (wave.entries == null)
                continue;

            foreach (WaveEntry entry in wave.entries)
            {
                if (entry == null)
                    continue;

                if (entry.enemyPrefab == null)
                    continue;

                // Nếu prefab đã có Pool thì không tạo lại.
                if (FindPool(entry.enemyPrefab) != null)
                    continue;

                GameObjectPool pool =
                    new GameObjectPool(
                        entry.enemyPrefab,
                        poolSizePerType,
                        transform,
                        poolCanGrow
                    );

                pools.Add(pool);
            }
        }
    }

    // =========================================================
    // FIND POOL
    // =========================================================

    private GameObjectPool FindPool(
        GameObject prefab
    )
    {
        for (int i = 0; i < pools.Count; i++)
        {
            if (pools[i].Prefab == prefab)
            {
                return pools[i];
            }
        }

        return null;
    }

    // =========================================================
    // RETURN ENEMY
    // =========================================================

    public void ReturnEnemyToPool(
        GameObject enemy
    )
    {
        if (enemy == null)
            return;

        // Không Destroy.
        enemy.SetActive(false);
    }

    // =========================================================
    // RUN WAVES
    // =========================================================

    private IEnumerator RunWaves()
    {
        for (
            int i = 0;
            i < waveData.waves.Length;
            i++
        )
        {
            Wave wave =
                waveData.waves[i];

            if (wave == null)
                continue;

            // -------------------------------------------------
            // DELAY BEFORE WAVE
            // -------------------------------------------------

            if (wave.delayBeforeWave > 0f)
            {
                yield return new WaitForSeconds(
                    wave.delayBeforeWave
                );
            }

            // -------------------------------------------------
            // WAVE NUMBER
            // -------------------------------------------------

            CurrentWave = i + 1;

            OnWaveChanged?.Invoke(
                CurrentWave,
                TotalWaves
            );

            // -------------------------------------------------
            // SPAWN WAVE
            // -------------------------------------------------

            yield return StartCoroutine(
                SpawnWave(wave)
            );
        }

        // -----------------------------------------------------
        // WAIT ALL ENEMIES
        // -----------------------------------------------------

        while (
            Enemy.ActiveEnemies.Count > 0
        )
        {
            yield return null;
        }

        // -----------------------------------------------------
        // ALL WAVES CLEARED
        // -----------------------------------------------------

        GameManager.Instance?.ReportAllWavesCleared();
    }

    // =========================================================
    // SPAWN WAVE
    // =========================================================

    private IEnumerator SpawnWave(
        Wave wave
    )
    {
        if (wave == null)
            yield break;

        if (wave.entries == null)
            yield break;

        foreach (
            WaveEntry entry
            in wave.entries
        )
        {
            if (entry == null)
                continue;

            if (entry.enemyPrefab == null)
                continue;

            for (
                int i = 0;
                i < entry.count;
                i++
            )
            {
                yield return StartCoroutine(
                    SpawnOne(
                        entry.enemyPrefab
                    )
                );

                if (entry.spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(
                        entry.spawnInterval
                    );
                }
            }
        }
    }

    // =========================================================
    // SPAWN ONE
    // =========================================================

    private IEnumerator SpawnOne(
        GameObject prefab
    )
    {
        if (prefab == null)
            yield break;

        // -----------------------------------------------------
        // FIND POOL
        // -----------------------------------------------------

        GameObjectPool pool =
            FindPool(prefab);

        if (pool == null)
        {
            Debug.LogError(
                $"[EnemySpawner] Không tìm thấy Pool cho prefab: {prefab.name}",
                this
            );

            yield break;
        }

        // -----------------------------------------------------
        // GET ENEMY
        // -----------------------------------------------------

        GameObject enemyObject =
            pool.Get();

        // Pool hết Enemy.
        // Nếu Pool không grow -> chờ Enemy cũ chết.
        while (enemyObject == null)
        {
            yield return null;

            enemyObject = pool.Get();
        }

        // -----------------------------------------------------
        // RESET TRANSFORM
        // -----------------------------------------------------

        enemyObject.transform.position =
            spawnPoint.position;

        enemyObject.transform.rotation =
            Quaternion.identity;

        // -----------------------------------------------------
        // GET ENEMY COMPONENT
        // -----------------------------------------------------

        Enemy enemy =
            enemyObject.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.SetSpawner(this);

            // Reset toàn bộ trạng thái
            // trong khi Enemy đang inactive.
            enemy.SetPath(
                waypointPath.waypoints
            );
        }
        else
        {
            Debug.LogError(
                $"[EnemySpawner] Prefab {prefab.name} không có component Enemy.",
                enemyObject
            );
        }

        // -----------------------------------------------------
        // ENABLE
        // -----------------------------------------------------

        // OnEnable() của Enemy sẽ được gọi.
        //
        // Animator KHÔNG được Play bằng code.
        // Animator sẽ tự chạy Default State.
        enemyObject.SetActive(true);
    }
}