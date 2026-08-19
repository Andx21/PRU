using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng cả màn chơi bằng code: prefab, đế tháp, manager, UI — không phải kéo thả gì.
///
///     Menu: Tower Defense ▸ DỰNG CẢ GAME (bấm 1 lần)
///
/// Viết cái này vì phần ráp scene bằng tay có tới mấy chục bước, sai một ô là
/// game không chạy mà cũng không báo lỗi rõ ràng. Chạy lại nhiều lần vô hại:
/// thứ gì đã có thì bỏ qua, không tạo trùng.
///
/// Sau khi chạy: Ctrl+S rồi bấm Play.
/// </summary>
public static class TowerDefenseSceneSetup
{
    private const string PrefabFolder = "Assets/Project/Prefabs";
    private const string ArtTowers = "Assets/Project/Art/Towers";
    private const string ArtProjectiles = "Assets/Project/Art/Projectiles";
    private const string ArtUi = "Assets/Project/Art/UI";
    private const string SoFolder = "Assets/Project/ScriptableObjects";

    /// <summary>17 đế tháp rải dọc đường, cách tim đường 0.95 unit. Tính sẵn từ
    /// toạ độ waypoint thật của scene, đã loại điểm nằm trên đường và điểm quá sát nhau.</summary>
    private static readonly Vector2[] SlotPositions =
    {
        new(-5.20f, 3.14f), new(-5.21f, 0.93f), new(-5.21f, -1.28f), new(-5.65f, -3.33f),
        new(-3.15f, -1.22f), new(-3.11f, 1.16f), new(-2.59f, 3.30f), new(-1.12f, 1.52f),
        new(-0.93f, -1.97f), new(1.12f, -1.97f), new(3.06f, -3.89f), new(5.04f, -2.00f),
        new(5.02f, 0.13f), new(1.28f, 0.25f), new(1.31f, 2.37f), new(5.22f, 3.25f),
        new(6.84f, 1.35f),
    };

    [MenuItem("Tower Defense/DỰNG CẢ GAME (bấm 1 lần)", priority = 0)]
    public static void BuildEverything()
    {
        // 1. Thông số + import settings cho art.
        TowerDefenseAssetSetup.CreateDefaultAssets();
        TowerDefenseArtImport.ApplyImportSettings();

        EnsureFolder(PrefabFolder);

        // 2. Prefab.
        GameObject arrow = CreateProjectilePrefab("Projectile_Arrow", $"{ArtProjectiles}/Arrow.png", 11f, true);
        GameObject ball = CreateProjectilePrefab("Projectile_Cannonball", $"{ArtProjectiles}/Cannonball.png", 7f, false);
        GameObject tower = CreateTowerPrefab();
        GameObject slot = CreateSlotPrefab();

        // 3. Nối sprite và đạn vào TowerStats — thiếu bước này thì tháp xây ra
        //    không có hình và không bắn được.
        WireTowerStats("TowerStats_Archer", $"{ArtTowers}/Tower_Archer.png", arrow);
        WireTowerStats("TowerStats_Melee", $"{ArtTowers}/Tower_Barracks.png", null);
        WireTowerStats("TowerStats_Cannon", $"{ArtTowers}/Tower_Cannon.png", ball);

        // 3b. Boss và đưa nó vào đợt cuối.
        GameObject boss = CreateBossPrefab();
        AddBossToLastWave(boss);

        // 4. Scene.
        PlaceSlots(slot);
        SetupManagers(tower);
        BuildCanvas();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[Tower Defense] Dựng xong. Nhấn Ctrl+S để lưu scene rồi bấm Play.");
    }

    // ───────────────────────────── Prefab ─────────────────────────────

    private static GameObject CreateProjectilePrefab(string name, string spritePath,
                                                     float speed, bool rotate)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        sr.sortingOrder = 30;

        var bullet = go.AddComponent<Bullet>();
        var so = new SerializedObject(bullet);
        SetFloat(so, "speed", speed);
        SetBool(so, "rotateTowardsTarget", rotate);
        so.ApplyModifiedProperties();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved;
    }

    private static GameObject CreateTowerPrefab()
    {
        string path = $"{PrefabFolder}/Tower.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go = new GameObject("Tower");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 20;

        // Điểm đạn bay ra: đặt trên thân tháp cho khỏi chui từ dưới đất lên.
        var fire = new GameObject("FirePoint");
        fire.transform.SetParent(go.transform, false);
        fire.transform.localPosition = new Vector3(0f, 0.55f, 0f);

        var tower = go.AddComponent<Tower>();
        var so = new SerializedObject(tower);
        SetRef(so, "bodyRenderer", sr);
        SetRef(so, "firePoint", fire.transform);
        so.ApplyModifiedProperties();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved;
    }

    private static GameObject CreateSlotPrefab()
    {
        string path = $"{PrefabFolder}/TowerSlot.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go = new GameObject("TowerSlot");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtTowers}/Tower_Slot.png");
        sr.sortingOrder = 10;

        var slot = go.AddComponent<TowerSlot>();
        var so = new SerializedObject(slot);
        SetRef(so, "slotRenderer", sr);
        so.ApplyModifiedProperties();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved;
    }

    // ───────────────────────────── Boss ─────────────────────────────

    /// <summary>
    /// Dựng prefab boss từ sheet đã cắt sẵn 11 khung.
    ///
    /// Dùng SpriteSheetAnimator chứ không dùng Animator: việc cần làm chỉ là lật
    /// qua 11 khung, mà dựng AnimatorController bằng code thì rườm rà và dễ hỏng.
    /// </summary>
    private static GameObject CreateBossPrefab()
    {
        string path = $"{PrefabFolder}/EnemyBoss.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        Sprite[] frames = LoadBossFrames();
        if (frames.Length == 0)
        {
            Debug.LogWarning("[Tower Defense] Sheet boss chưa được cắt — bỏ qua prefab boss. "
                             + "Chạy lại menu này sau khi Unity import xong art.");
            return null;
        }

        var go = new GameObject("EnemyBoss");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = 25;   // trên tháp, để con boss không bị tháp che

        var anim = go.AddComponent<SpriteSheetAnimator>();
        var aso = new SerializedObject(anim);
        var arr = aso.FindProperty("frames");
        if (arr != null)
        {
            arr.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }
        SetFloat(aso, "fps", 10f);
        aso.ApplyModifiedProperties();

        var enemy = go.AddComponent<Enemy>();
        var eso = new SerializedObject(enemy);
        SetRef(eso, "stats", AssetDatabase.LoadAssetAtPath<EnemyStats>($"{SoFolder}/EnemyStats_Boss.asset"));
        eso.ApplyModifiedProperties();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[Tower Defense] Đã tạo prefab boss ({frames.Length} khung).");
        return saved;
    }

    /// <summary>Lấy 11 sprite con của sheet boss, xếp đúng thứ tự Boss_Walk_0..10.</summary>
    private static Sprite[] LoadBossFrames()
    {
        Object[] all = AssetDatabase.LoadAllAssetRepresentationsAtPath(
            "Assets/Project/Art/Boss/Boss_Walk-Sheet.png");

        var list = new List<Sprite>();
        foreach (Object o in all)
        {
            if (o is Sprite s) list.Add(s);
        }

        // Tên là Boss_Walk_0..Boss_Walk_10; sắp theo SỐ chứ không theo chữ, không thì
        // thứ tự thành 0,1,10,2,3...
        list.Sort((a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));
        return list.ToArray();
    }

    private static int FrameIndex(string name)
    {
        int i = name.LastIndexOf('_');
        return i >= 0 && int.TryParse(name.Substring(i + 1), out int n) ? n : 0;
    }

    /// <summary>Điền prefab boss vào ô còn trống ở đợt cuối của WaveData.</summary>
    private static void AddBossToLastWave(GameObject boss)
    {
        if (boss == null) return;

        var data = AssetDatabase.LoadAssetAtPath<WaveData>($"{SoFolder}/WaveData_Level1.asset");
        if (data == null || data.waves == null || data.waves.Length == 0) return;

        Wave last = data.waves[data.waves.Length - 1];
        if (last?.entries == null) return;

        bool changed = false;
        foreach (WaveEntry entry in last.entries)
        {
            // Ô boss được tạo với prefab null từ lúc chưa có prefab boss.
            if (entry != null && entry.enemyPrefab == null)
            {
                entry.enemyPrefab = boss;
                changed = true;
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(data);
            Debug.Log("[Tower Defense] Đã đưa boss vào đợt cuối.");
        }
    }

    // ───────────────────────────── Âm thanh ─────────────────────────────

    /// <summary>Gán các file âm thanh sinh sẵn vào AudioManager.</summary>
    private static void WireAudio(AudioManager audio)
    {
        var so = new SerializedObject(audio);

        SetRef(so, "backgroundMusic", Clip("music_loop"));
        SetRef(so, "towerShootClip", Clip("sfx_tower_shoot"));
        SetRef(so, "enemyDeathClip", Clip("sfx_enemy_death"));
        SetRef(so, "buildClip", Clip("sfx_build"));
        SetRef(so, "baseHitClip", Clip("sfx_base_hit"));
        SetRef(so, "errorClip", Clip("sfx_error"));
        SetRef(so, "victoryClip", Clip("sfx_victory"));
        SetRef(so, "defeatClip", Clip("sfx_defeat"));

        so.ApplyModifiedProperties();
    }

    private static AudioClip Clip(string name)
        => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Project/Audio/{name}.wav");

    private static void WireTowerStats(string assetName, string spritePath, GameObject projectile)
    {
        var stats = AssetDatabase.LoadAssetAtPath<TowerStats>($"{SoFolder}/{assetName}.asset");
        if (stats == null) return;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite != null)
        {
            stats.towerSprite = sprite;
            stats.shopIcon = sprite;
        }
        if (projectile != null) stats.projectilePrefab = projectile;

        EditorUtility.SetDirty(stats);
    }

    // ───────────────────────────── Scene ─────────────────────────────

    private static void PlaceSlots(GameObject slotPrefab)
    {
        GameObject root = GameObject.Find("TowerSlots");
        if (root != null) return;                 // đã đặt rồi thì thôi

        root = new GameObject("TowerSlots");
        for (int i = 0; i < SlotPositions.Length; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab);
            go.name = $"Slot_{i:00}";
            go.transform.SetParent(root.transform);
            go.transform.position = SlotPositions[i];
        }
    }

    private static void SetupManagers(GameObject towerPrefab)
    {
        GameObject gm = GameObject.Find("GameManager") ?? new GameObject("GameManager");

        Add<GameManager>(gm);
        Add<AudioManager>(gm);

        var build = Add<BuildManager>(gm);
        var so = new SerializedObject(build);
        SetRef(so, "towerPrefab", towerPrefab);
        SetRef(so, "gameCamera", Camera.main);
        so.ApplyModifiedProperties();

        // AudioManager cần AudioSource, không có thì mọi lời gọi âm thanh im lặng.
        var audio = gm.GetComponent<AudioManager>();
        if (gm.GetComponent<AudioSource>() == null)
        {
            var music = gm.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            var sfx = gm.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            var aso = new SerializedObject(audio);
            SetRef(aso, "musicSource", music);
            SetRef(aso, "sfxSource", sfx);
            aso.ApplyModifiedProperties();
        }

        WireAudio(audio);
    }

    // ───────────────────────────── UI ─────────────────────────────

    private static void BuildCanvas()
    {
        if (GameObject.Find("UI Canvas") != null) return;

        // EventSystem: thiếu là mọi nút UI bấm không ăn. Project bật Input System
        // mới nên phải dùng InputSystemUIInputModule, không phải StandaloneInputModule.
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            es.name = "EventSystem";
        }

        var canvasGo = new GameObject("UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Font font = GetBuiltinFont();

        // ── HUD trên cùng ─────────────────────────────────────────────
        var hudGo = NewRect("HUD", canvasGo.transform, new Vector2(0, 1), new Vector2(0, 1),
                            new Vector2(0, 1), new Vector2(30, -30), new Vector2(600, 60));
        var lives = NewText("Lives", hudGo.transform, font, "20", 40, TextAnchor.MiddleLeft,
                            new Vector2(0, 0), new Vector2(160, 60));
        var coins = NewText("Coins", hudGo.transform, font, "150", 40, TextAnchor.MiddleLeft,
                            new Vector2(180, 0), new Vector2(200, 60));
        var wave = NewText("Wave", hudGo.transform, font, "Chuẩn bị…", 32, TextAnchor.MiddleLeft,
                           new Vector2(400, 0), new Vector2(320, 60));

        var hud = hudGo.AddComponent<HUD>();
        var hso = new SerializedObject(hud);
        SetRef(hso, "livesText", lives);
        SetRef(hso, "coinsText", coins);
        SetRef(hso, "waveText", wave);
        hso.ApplyModifiedProperties();

        // ── Shop dưới đáy ─────────────────────────────────────────────
        var shop = NewRect("Shop", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                           new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(720, 170));
        var layout = shop.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        MakeShopButton(shop.transform, font, "TowerStats_Archer");
        MakeShopButton(shop.transform, font, "TowerStats_Melee");
        MakeShopButton(shop.transform, font, "TowerStats_Cannon");

        // ── Nút tạm dừng + các bảng phủ ───────────────────────────────
        var pauseBtn = MakeButton("PauseButton", canvasGo.transform, font, "II", 40,
                                  new Vector2(1, 1), new Vector2(-40, -40), new Vector2(70, 70));

        var pausePanel = MakePanel("PausePanel", canvasGo.transform, "TẠM DỪNG", font, out _);
        var resumeBtn = MakeButton("Resume", pausePanel.transform, font, "Tiếp tục", 32,
                                   new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(360, 70));
        var restartBtn = MakeButton("Restart", pausePanel.transform, font, "Chơi lại", 32,
                                    new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(360, 70));
        var settingsBtn = MakeButton("Settings", pausePanel.transform, font, "Cài đặt", 32,
                                     new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(360, 70));

        var setPanel = MakePanel("SettingsPanel", canvasGo.transform, "CÀI ĐẶT", font, out _);
        var musicSlider = MakeSlider("Music", setPanel.transform, new Vector2(0, 40), 0.5f);
        var sfxSlider = MakeSlider("Sfx", setPanel.transform, new Vector2(0, -40), 0.8f);
        var closeSet = MakeButton("CloseSettings", setPanel.transform, font, "Đóng", 30,
                                  new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(300, 64));

        var settings = setPanel.AddComponent<SettingsPanel>();
        var sso = new SerializedObject(settings);
        SetRef(sso, "musicSlider", musicSlider);
        SetRef(sso, "sfxSlider", sfxSlider);
        sso.ApplyModifiedProperties();

        var pause = canvasGo.AddComponent<PauseMenu>();
        var pso = new SerializedObject(pause);
        SetRef(pso, "pausePanel", pausePanel);
        SetRef(pso, "settingsPanel", setPanel);
        SetRef(pso, "pauseButton", pauseBtn);
        SetRef(pso, "resumeButton", resumeBtn);
        SetRef(pso, "restartButton", restartBtn);
        SetRef(pso, "settingsButton", settingsBtn);
        SetRef(pso, "closeSettingsButton", closeSet);
        pso.ApplyModifiedProperties();

        // ── Màn thắng / thua ──────────────────────────────────────────
        var endPanel = MakePanel("EndPanel", canvasGo.transform, "—", font, out Text endTitle);
        var endMsg = NewText("Message", endPanel.transform, font, "—", 28, TextAnchor.MiddleCenter,
                             new Vector2(0, 60), new Vector2(600, 80));
        var endRestart = MakeButton("EndRestart", endPanel.transform, font, "Chơi lại", 32,
                                    new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(360, 70));

        var end = endPanel.AddComponent<GameEndPanel>();
        var eso = new SerializedObject(end);
        SetRef(eso, "panel", endPanel);
        SetRef(eso, "titleText", endTitle);
        SetRef(eso, "messageText", endMsg);
        SetRef(eso, "restartButton", endRestart);
        eso.ApplyModifiedProperties();

        pausePanel.SetActive(false);
        setPanel.SetActive(false);
        endPanel.SetActive(false);
    }

    private static void MakeShopButton(Transform parent, Font font, string statsName)
    {
        var stats = AssetDatabase.LoadAssetAtPath<TowerStats>($"{SoFolder}/{statsName}.asset");

        var go = new GameObject(statsName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(220, 150);
        go.GetComponent<Image>().color = new Color(0.17f, 0.11f, 0.07f, 0.93f);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 220;
        le.preferredHeight = 150;

        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(go.transform, false);
        var iconRt = (RectTransform)icon.transform;
        iconRt.anchoredPosition = new Vector2(0, 34);
        iconRt.sizeDelta = new Vector2(78, 78);
        var iconImg = icon.GetComponent<Image>();
        iconImg.preserveAspect = true;
        if (stats != null && stats.shopIcon != null) iconImg.sprite = stats.shopIcon;

        var nameText = NewText("Name", go.transform, font, stats != null ? stats.displayName : "?",
                               22, TextAnchor.MiddleCenter, new Vector2(0, -24), new Vector2(210, 32));
        var costText = NewText("Cost", go.transform, font, stats != null ? stats.cost.ToString() : "0",
                               26, TextAnchor.MiddleCenter, new Vector2(0, -54), new Vector2(210, 34));
        costText.color = new Color(1f, 0.83f, 0.36f);

        var shopBtn = go.AddComponent<TowerShopButton>();
        var so = new SerializedObject(shopBtn);
        SetRef(so, "towerStats", stats);
        SetRef(so, "iconImage", iconImg);
        SetRef(so, "nameText", nameText);
        SetRef(so, "costText", costText);
        so.ApplyModifiedProperties();
    }

    // ───────────────────────── Tiện ích dựng UI ─────────────────────────

    private static Font GetBuiltinFont()
    {
        // Unity 6 đổi tên font mặc định; thử tên mới rồi mới tới tên cũ.
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    private static GameObject NewRect(string name, Transform parent, Vector2 anchorMin,
                                      Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    private static Text NewText(string name, Transform parent, Font font, string content,
                                int size, TextAnchor align, Vector2 pos, Vector2 dim)
    {
        var go = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                         new Vector2(0.5f, 0.5f), pos, dim);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private static Button MakeButton(string name, Transform parent, Font font, string label,
                                     int size, Vector2 anchor, Vector2 pos, Vector2 dim)
    {
        var go = NewRect(name, parent, anchor, anchor, anchor, pos, dim);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.42f, 0.29f, 0.18f, 1f);
        var btn = go.AddComponent<Button>();
        NewText("Label", go.transform, font, label, size, TextAnchor.MiddleCenter,
                Vector2.zero, dim);
        return btn;
    }

    private static Slider MakeSlider(string name, Transform parent, Vector2 pos, float value)
    {
        var go = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                         new Vector2(0.5f, 0.5f), pos, new Vector2(420, 40));
        var slider = go.AddComponent<Slider>();

        var bg = NewRect("Background", go.transform, Vector2.zero, Vector2.one,
                         new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        var fillArea = NewRect("Fill Area", go.transform, Vector2.zero, Vector2.one,
                               new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var fill = NewRect("Fill", fillArea.transform, Vector2.zero, Vector2.one,
                           new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.49f, 0.88f, 0.54f);

        slider.fillRect = (RectTransform)fill.transform;
        slider.targetGraphic = fillImg;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        return slider;
    }

    private static GameObject MakePanel(string name, Transform parent, string title,
                                        Font font, out Text titleText)
    {
        var go = NewRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                         Vector2.zero, Vector2.zero);
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        var box = NewRect("Box", go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 460));
        var boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.17f, 0.11f, 0.07f, 1f);

        titleText = NewText("Title", go.transform, font, title, 52, TextAnchor.MiddleCenter,
                            new Vector2(0, 150), new Vector2(520, 80));
        return go;
    }

    // ───────────────────────── Tiện ích chung ─────────────────────────

    private static T Add<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static void SetRef(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = value;
    }

    private static void SetFloat(SerializedObject so, string prop, float value)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.floatValue = value;
    }

    private static void SetBool(SerializedObject so, string prop, bool value)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.boolValue = value;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
