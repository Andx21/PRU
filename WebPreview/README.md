# WebPreview — bản chơi thử trên trình duyệt

**Đây không phải bài nộp.** Bài nộp là project Unity ở thư mục `Assets/`.
Thư mục này nằm ngoài `Assets/` nên Unity không hề đụng tới nó.

Mục đích: chạy thử ngay khi chưa cài Unity, để cảm nhận nhịp game và chốt các
con số cân bằng trước khi ráp scene.

## Chạy

```bash
cd WebPreview
python3 -m http.server 8123
```

Rồi mở <http://localhost:8123>.

Phải chạy qua máy chủ, không mở thẳng file được: trình duyệt chặn `fetch`
đọc `assets/scene.json` khi ở giao thức `file://`.

## Dùng chung gì với bản Unity

| | Nguồn |
|---|---|
| Toạ độ 15 waypoint | trích thẳng từ `Assets/Project/Scenes/Test.unity` |
| Ảnh map, quái, tháp, đạn, icon | chính bộ art trong `Assets/Project/Art/` |
| Máu, tiền, chỉ số quái và tháp, 5 đợt | khớp `TowerDefenseAssetSetup.cs` |
| Tỉ lệ hiển thị | khớp Pixels Per Unit trong `TowerDefenseArtImport.cs` |
| Cách pool | cùng kiểu: giữ một danh sách, chỉ bật/tắt cờ, không huỷ đối tượng |

Chỉnh số trong phần đầu `game.js` thấy hợp lý thì chép sang asset Unity tương ứng.

## 17 vị trí đế tháp

Nằm trong `assets/scene.json`, khoá `slots`. Tính bằng cách rải dọc hai bên
đường, cách tim đường 0.95 unit, loại bỏ điểm nằm trên đường hoặc quá sát nhau.
Dùng lại đúng danh sách này khi đặt đế trong scene Unity.

## Khác biệt so với bản Unity

- Âm thanh ở đây tổng hợp bằng WebAudio (tiếng bíp), vì bộ art không kèm file
  âm thanh. Bản Unity dùng `AudioClip` gắn vào `AudioManager`.
- Không có Animator: hoạt ảnh chạy bằng cách đổi khung hình theo thời gian.
- Đây là code riêng, **không dùng lại được** cho Unity.
