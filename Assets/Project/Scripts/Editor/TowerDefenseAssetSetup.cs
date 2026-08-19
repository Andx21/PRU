using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tạo sẵn bộ asset thông số (quái, tháp, đợt) với số liệu đã cân bằng thô,
/// khỏi phải ngồi tạo tay từng cái rồi gõ số.
///
/// Menu: Tower Defense ▸ Tạo asset thông số mặc định
///
/// Chạy nhiều lần cũng không sao: asset nào đã tồn tại thì bỏ qua, không ghi đè,
/// nên số liệu bạn tự chỉnh sẽ không bị mất.
/// </summary>
public static class TowerDefenseAssetSetup
{
    private const string Folder = "Assets/Project/ScriptableObjects";
    private const string SlimePrefabPath = "Assets/Project/Prefabs/Enemy.prefab";
    private const string OrcPrefabPath = "Assets/Project/Prefabs/EnemyOrc.prefab";
    private const string BossPrefabPath = "Assets/Project/Prefabs/EnemyBoss.prefab";

    [MenuItem("Tower Defense/Tạo asset thông số mặc định")]
    public static void CreateDefaultAssets()
    {
        EnsureFolder();

        EnemyStats slime = CreateOrGet<EnemyStats>("EnemyStats_Slime", asset =>
        {
            asset.displayName = "Slime";
            asset.isBoss = false;
            asset.maxHealth = 40;
            asset.moveSpeed = 1.8f;
            asset.coinReward = 8;
            asset.damageToBase = 1;
        });

        EnemyStats orc = CreateOrGet<EnemyStats>("EnemyStats_Orc", asset =>
        {
            asset.displayName = "Orc";
            asset.isBoss = false;
            asset.maxHealth = 80;
            asset.moveSpeed = 1.4f;
            asset.coinReward = 14;
            asset.damageToBase = 2;
        });

        CreateOrGet<EnemyStats>("EnemyStats_Boss", asset =>
        {
            asset.displayName = "Boss";
            asset.isBoss = true;
            asset.maxHealth = 600;
            asset.moveSpeed = 1.0f;
            asset.coinReward = 100;
            asset.damageToBase = 10;
        });

        CreateOrGet<TowerStats>("TowerStats_Archer", asset =>
        {
            asset.displayName = "Cung thủ";
            asset.description = "Tầm xa, bắn một mục tiêu. Rẻ, dựng sớm.";
            asset.attackType = TowerAttackType.Ranged;
            asset.damage = 12;
            asset.range = 3.2f;
            asset.fireRate = 1.4f;
            asset.cost = 50;
        });

        CreateOrGet<TowerStats>("TowerStats_Melee", asset =>
        {
            asset.displayName = "Lính cận chiến";
            asset.description = "Tầm rất ngắn nhưng sát thương cao, đánh trúng ngay.";
            asset.attackType = TowerAttackType.Melee;
            asset.damage = 25;
            asset.range = 1.4f;
            asset.fireRate = 2.0f;
            asset.cost = 75;
        });

        CreateOrGet<TowerStats>("TowerStats_Cannon", asset =>
        {
            asset.displayName = "Pháo";
            asset.description = "Bắn chậm, đạn nổ lan sát thương cả cụm quái.";
            asset.attackType = TowerAttackType.Splash;
            asset.damage = 18;
            asset.range = 2.8f;
            asset.fireRate = 0.7f;
            asset.splashRadius = 1.3f;
            asset.cost = 100;
        });

        WaveData waves = CreateWaveData(slime, orc);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Gán sẵn luôn thay vì bắt kéo thả tay — đây là chỗ hay sót nhất.
        AssignStatsToEnemyPrefab(SlimePrefabPath, slime);
        AssignStatsToEnemyPrefab(OrcPrefabPath, orc);
        AssignWaveDataToScene(waves);

        AssetDatabase.SaveAssets();

        Debug.Log($"[Tower Defense] Xong. Asset thông số nằm ở {Folder}, " +
                  "Wave Data đã gán vào EnemySpawner, EnemyStats đã gán vào 2 prefab quái. " +
                  "Bước tiếp: gán sprite vào 3 TowerStats rồi đặt đế tháp.");
    }

    private static WaveData CreateWaveData(EnemyStats slime, EnemyStats orc)
    {
        string path = $"{Folder}/WaveData_Level1.asset";

        WaveData existing = AssetDatabase.LoadAssetAtPath<WaveData>(path);
        if (existing != null)
            return existing;

        GameObject slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlimePrefabPath);
        GameObject orcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OrcPrefabPath);
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);

        if (bossPrefab == null)
        {
            Debug.LogWarning("[Tower Defense] Chưa có EnemyBoss.prefab — đợt 5 sẽ để trống ô boss, " +
                             "tạo prefab boss xong nhớ kéo vào WaveData.");
        }

        var waveData = ScriptableObject.CreateInstance<WaveData>();

        waveData.waves = new[]
        {
            MakeWave("Đợt 1 — dò đường", 3f,
                Entry(slimePrefab, 6, 0.9f)),

            MakeWave("Đợt 2 — đông hơn", 8f,
                Entry(slimePrefab, 10, 0.7f)),

            MakeWave("Đợt 3 — có Orc", 10f,
                Entry(slimePrefab, 6, 0.6f),
                Entry(orcPrefab, 5, 1.0f)),

            MakeWave("Đợt 4 — Orc tràn", 12f,
                Entry(orcPrefab, 12, 0.7f)),

            MakeWave("Đợt 5 — BOSS", 14f,
                Entry(orcPrefab, 8, 0.6f),
                Entry(bossPrefab, 1, 1f))
        };

        AssetDatabase.CreateAsset(waveData, path);
        return waveData;
    }

    // ──────────────────────── Tự gán, khỏi kéo thả tay ────────────────────────

    /// <summary>
    /// Gán WaveData vào EnemySpawner đang có trong scene mở sẵn.
    /// Trường waveData là private [SerializeField] nên phải ghi qua SerializedObject.
    /// </summary>
    private static void AssignWaveDataToScene(WaveData waveData)
    {
        if (waveData == null)
            return;

        var spawner = Object.FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);

        if (spawner == null)
        {
            Debug.LogWarning("[Tower Defense] Không thấy EnemySpawner trong scene đang mở — " +
                             "mở scene Test rồi chạy lại menu này.");
            return;
        }

        var so = new SerializedObject(spawner);
        var prop = so.FindProperty("waveData");

        if (prop == null)
            return;

        if (prop.objectReferenceValue != null)
            return;                       // đã gán rồi thì không đè lên

        prop.objectReferenceValue = waveData;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        Debug.Log("[Tower Defense] Đã gán Wave Data vào EnemySpawner. Nhớ Ctrl+S để lưu scene.");
    }

    /// <summary>Gán EnemyStats vào prefab quái. Sửa prefab thì phải mở nội dung ra rồi lưu lại.</summary>
    private static void AssignStatsToEnemyPrefab(string prefabPath, EnemyStats stats)
    {
        if (stats == null || AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            var enemy = root.GetComponent<Enemy>();

            if (enemy == null)
            {
                Debug.LogWarning($"[Tower Defense] {prefabPath} chưa có component Enemy.");
                return;
            }

            var so = new SerializedObject(enemy);
            var prop = so.FindProperty("stats");

            if (prop == null || prop.objectReferenceValue != null)
                return;

            prop.objectReferenceValue = stats;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[Tower Defense] Đã gán {stats.name} vào {System.IO.Path.GetFileName(prefabPath)}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Wave MakeWave(string name, float delay, params WaveEntry[] entries)
    {
        return new Wave
        {
            waveName = name,
            delayBeforeWave = delay,
            entries = entries
        };
    }

    private static WaveEntry Entry(GameObject prefab, int count, float interval)
    {
        return new WaveEntry
        {
            enemyPrefab = prefab,
            count = count,
            spawnInterval = interval
        };
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Project"))
        {
            AssetDatabase.CreateFolder("Assets", "Project");
        }

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/Project", "ScriptableObjects");
        }
    }

    private static T CreateOrGet<T>(string assetName, System.Action<T> configure) where T : ScriptableObject
    {
        string path = $"{Folder}/{assetName}.asset";

        T existing = AssetDatabase.LoadAssetAtPath<T>(path);

        if (existing != null)
        {
            // Đã có rồi thì giữ nguyên số liệu người dùng đã chỉnh.
            return existing;
        }

        T asset = ScriptableObject.CreateInstance<T>();
        configure(asset);
        AssetDatabase.CreateAsset(asset, path);

        return asset;
    }
}
