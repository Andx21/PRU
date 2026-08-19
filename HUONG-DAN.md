# Chạy game — 3 bước

Mở project bằng **Unity 6000.4.11f1** (đúng phiên bản này, mở bằng bản khác Unity
sẽ đòi nâng cấp).

### 1. Mở scene

Trong cửa sổ **Project**, mở `Assets/Project/Scenes/Test.unity` (bấm đúp).

### 2. Bấm menu **Tower Defense ▸ DỰNG CẢ GAME (bấm 1 lần)**

Chờ vài giây. Cửa sổ **Console** sẽ in `[Tower Defense] Dựng xong.`

Menu này tự làm hết những việc đáng lẽ phải kéo thả tay:

- Tạo asset thông số quái, tháp và 5 đợt
- Tạo prefab `Tower`, `TowerSlot`, `Projectile_Arrow`, `Projectile_Cannonball`,
  và `EnemyBoss` (11 khung hoạt ảnh, tự đưa vào đợt 5)
- Gán ảnh và đạn vào từng loại tháp
- Đặt **17 đế tháp** dọc đường đi
- Gắn `GameManager`, `BuildManager`, `AudioManager` + 2 AudioSource, và nối
  sẵn 8 file âm thanh
- Dựng toàn bộ giao diện: máu, tiền, số đợt, shop 3 tháp, nút tạm dừng, bảng
  cài đặt, màn thắng/thua, kèm EventSystem

Bấm lại nhiều lần cũng không sao — thứ gì đã có thì bỏ qua, không tạo trùng.

### 3. `Ctrl + S` rồi bấm **Play**

---

## Cách chơi

**Không kéo thả tháp.** Cách xây là:

1. Bấm vào **một trong 3 thẻ tháp** ở đáy màn hình → thẻ sáng viền xanh
2. Rê chuột lên **đế đá** trên bản đồ → đế sáng xanh (đủ tiền) hoặc đỏ (thiếu tiền),
   kèm vòng tròn cho thấy tầm bắn
3. **Bấm vào đế** → tháp mọc lên, tiền bị trừ

Chuột phải để bỏ chọn. Phím `Esc` để tạm dừng.

| Tháp | Giá | Kiểu đánh |
|---|---|---|
| Cung thủ | 50 | Tầm xa, bắn 1 mục tiêu |
| Lính cận chiến | 75 | Tầm ngắn, sát thương cao, trúng ngay |
| Pháo | 100 | Bắn chậm, đạn nổ lan cả cụm |

Bắt đầu có **150 tiền** và **20 máu**. Giết quái ra tiền, quái đi lọt về Base thì
trừ máu. Hết 5 đợt mà còn máu là thắng.

**Đợt 5 có boss**: 600 máu, đi chậm, trừ 10 máu Base nếu lọt, giết được thưởng
100 tiền. To gấp rưỡi quái thường nên nhìn là nhận ra ngay.

---

## Âm thanh

8 file trong `Assets/Project/Audio/` do `Tools/make_audio.py` sinh ra bằng cách
tổng hợp dạng sóng — **tự tạo hoàn toàn, không dính bản quyền của ai**. Tải nhạc
trên mạng về dùng cho bài nộp là rủi ro không đáng.

| File | Dùng khi |
|---|---|
| `music_loop.wav` | nhạc nền, vòng lặp 16 giây liền mạch |
| `sfx_tower_shoot.wav` | tháp bắn |
| `sfx_enemy_death.wav` | quái chết |
| `sfx_build.wav` | xây xong tháp |
| `sfx_base_hit.wav` | quái lọt về Base |
| `sfx_error.wav` | thiếu tiền / đế đã có tháp |
| `sfx_victory.wav`, `sfx_defeat.wav` | thắng / thua |

Muốn đổi tiếng thì sửa `Tools/make_audio.py` rồi chạy `python3 Tools/make_audio.py`,
hoặc thay thẳng file `.wav` cùng tên.

---

## Nếu gặp trục trặc

**Menu "Tower Defense" không hiện** — Unity chưa biên dịch xong. Chờ vòng xoay ở
góc dưới bên phải biến mất. Nếu Console có lỗi đỏ thì chụp gửi lại.

**Bấm Play mà không có quái nào ra** — mở Console xem có dòng
`[EnemySpawner] Chưa gán Wave Data`. Nếu có, chạy lại menu ở bước 2.

**Đợt 5 không thấy boss** — mở Console xem có dòng `Sheet boss chưa được cắt`.
Nếu có, đợi Unity import xong art rồi chạy lại menu ở bước 2.

**Không nghe thấy tiếng** — kiểm tra thanh trượt trong bảng Cài đặt (bấm Esc ▸
Cài đặt). Âm lượng được nhớ lại giữa các lần chơi, nên nếu lần trước kéo về 0 thì
lần này vẫn im.

---

## Muốn xem trước mà chưa cài Unity

Thư mục `WebPreview/` có bản chơi thử chạy trên trình duyệt, dùng đúng bộ ảnh,
đúng đường đi và đúng các con số:

```bash
cd WebPreview
python3 -m http.server 8123
```

Rồi mở <http://localhost:8123>. Đây **không phải bài nộp**, chỉ để xem nhịp game.
