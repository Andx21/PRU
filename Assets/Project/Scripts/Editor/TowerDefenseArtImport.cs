using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Đặt import settings cho bộ art mới và cắt sẵn sheet boss thành 11 frame.
///
/// Menu: Tower Defense ▸ Áp dụng import settings cho art
///
/// Làm bằng script vì hai lý do: gõ tay 12 file trong Inspector rất dễ sót, và
/// Pixels Per Unit ở đây đã tính sẵn để tháp/boss ra đúng tỉ lệ so với quái cũ
/// (Orc cao 15px ở PPU 100, scale 4 → 0.6 unit trên màn hình).
/// </summary>
public static class TowerDefenseArtImport
{
    private const string ArtRoot = "Assets/Project/Art";

    /// <summary>PPU cho từng file. Số càng lớn thì vật thể hiện ra càng nhỏ.</summary>
    private static readonly Dictionary<string, float> PixelsPerUnit = new Dictionary<string, float>
    {
        // Boss cao 278px ở PPU 300 → 0.93 unit, tức gấp ~1.5 lần con Orc thường.
        { "Boss/Boss_Walk-Sheet.png", 300f },

        { "Towers/Tower_Archer.png", 700f },
        { "Towers/Tower_Barracks.png", 700f },
        { "Towers/Tower_Cannon.png", 700f },
        { "Towers/Tower_Slot.png", 1000f },
        { "Projectiles/Arrow.png", 2600f },
        { "Projectiles/Cannonball.png", 2600f },

        { "UI/UI_Panel.png", 100f },
        { "UI/Icon_Heart.png", 100f },
        { "UI/Icon_Coin.png", 100f },
        { "UI/Icon_Pause.png", 100f },
        { "UI/Icon_Restart.png", 100f },
        { "UI/Icon_Settings.png", 100f },
        { "UI/Icon_Sound.png", 100f }
    };

    /// <summary>File pixel art phải để Point + không nén, không thì răng cưa bị làm mờ.</summary>
    private static readonly HashSet<string> PixelArt = new HashSet<string>
    {
        "Boss/Boss_Walk-Sheet.png"
    };

    private const int BossFrameWidth = 260;
    private const int BossFrameHeight = 284;
    private const int BossFrameCount = 11;

    [MenuItem("Tower Defense/Áp dụng import settings cho art")]
    public static void ApplyImportSettings()
    {
        int done = 0;

        foreach (var pair in PixelsPerUnit)
        {
            string path = $"{ArtRoot}/{pair.Key}";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                Debug.LogWarning($"[Art] Không thấy {path}, bỏ qua.");
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pair.Value;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            bool isPixelArt = PixelArt.Contains(pair.Key);
            importer.filterMode = isPixelArt ? FilterMode.Point : FilterMode.Bilinear;
            importer.textureCompression = isPixelArt
                ? TextureImporterCompression.Uncompressed
                : TextureImporterCompression.Compressed;

            if (pair.Key == "Boss/Boss_Walk-Sheet.png")
            {
                SliceBossSheet(importer);
            }
            else
            {
                importer.spriteImportMode = SpriteImportMode.Single;
            }

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            done++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Art] Đã đặt import settings cho {done} file. " +
                  $"Sheet boss được cắt sẵn {BossFrameCount} frame {BossFrameWidth}x{BossFrameHeight}.");
    }

    /// <summary>
    /// Cắt sheet boss theo lưới 11 khung.
    /// </summary>
    private static void SliceBossSheet(TextureImporter importer)
    {
        importer.spriteImportMode = SpriteImportMode.Multiple;

        var metas = new List<SpriteMetaData>();

        for (int i = 0; i < BossFrameCount; i++)
        {
            metas.Add(new SpriteMetaData
            {
                name = $"Boss_Walk_{i}",
                rect = new Rect(i * BossFrameWidth, 0, BossFrameWidth, BossFrameHeight),
                // Pivot GIỮA cho khớp Slime và Orc: waypoint nằm giữa đường, quái
                // pivot đáy sẽ trông như đang lơ lửng phía trên đường.
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            });
        }

#pragma warning disable CS0618 // spritesheet API cũ nhưng vẫn là cách gọn nhất để cắt lưới bằng script
        importer.spritesheet = metas.ToArray();
#pragma warning restore CS0618
    }
}
