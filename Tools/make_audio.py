"""Tổng hợp bộ âm thanh cho game bằng code.

Bộ art mua về không kèm âm thanh nào, mà tải nhạc trên mạng thì vướng bản quyền
— bài nộp mà dùng nhạc không rõ nguồn là rủi ro. Ở đây sinh thẳng dạng sóng nên
âm thanh hoàn toàn tự tạo, dùng thoải mái.

    python3 Tools/make_audio.py

Ghi ra Assets/Project/Audio/*.wav (16-bit mono 44.1kHz — Unity đọc trực tiếp).

Nhạc nền LẶP LIỀN MẠCH: mọi tần số đều được làm tròn về bội số của 1/độ_dài_vòng,
nên mỗi thành phần kết thúc đúng lúc hết một chu kỳ nguyên. Không làm vậy thì
chỗ nối sẽ nghe "cụp" mỗi lần lặp lại.
"""
import math
import os
import random
import struct
import wave

SR = 44100
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                   "Assets", "Project", "Audio")


# ───────────────────────────── tiện ích ─────────────────────────────

def write_wav(name, samples):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    peak = max(1e-9, max(abs(s) for s in samples))
    scale = 0.89 / peak if peak > 0.89 else 1.0
    frames = b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s * scale)) * 32767))
                      for s in samples)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(frames)
    print(f"  {name:24s} {len(samples)/SR:5.2f}s  {len(frames)//1024:5d} KB")


def env(i, n, attack=0.01, release=0.35):
    """Bao biên độ: lên nhanh, xuống mượt. Tránh tiếng 'tách' ở hai đầu."""
    t = i / n
    a = min(1.0, (i / SR) / attack) if attack > 0 else 1.0
    r = 1.0 if t < 1 - release else max(0.0, (1 - t) / release)
    return a * r


def tone(freq, t, kind="sine"):
    p = 2 * math.pi * freq * t
    if kind == "sine":
        return math.sin(p)
    if kind == "tri":
        return 2 / math.pi * math.asin(math.sin(p))
    if kind == "square":
        return 1.0 if math.sin(p) >= 0 else -1.0
    if kind == "saw":
        x = (freq * t) % 1.0
        return 2 * x - 1
    return 0.0


# ───────────────────────────── hiệu ứng ─────────────────────────────

def sfx_shoot():
    """Tiếng bắn: rất ngắn và nhỏ — mỗi giây có thể vang mấy lần, to là chói tai."""
    n = int(0.085 * SR)
    out = []
    for i in range(n):
        t = i / SR
        f = 760 - 300 * (i / n)
        s = 0.55 * tone(f, t, "square") + 0.45 * tone(f * 2, t, "sine")
        out.append(0.32 * s * env(i, n, 0.002, 0.7))
    return out


def sfx_enemy_death():
    """Quái chết: nhiễu bung ra + âm trầm tụt xuống."""
    n = int(0.28 * SR)
    rnd = random.Random(7)
    out = []
    for i in range(n):
        t = i / SR
        f = 320 - 240 * (i / n)
        s = 0.6 * tone(f, t, "saw") + 0.4 * (rnd.uniform(-1, 1) * (1 - i / n))
        out.append(0.5 * s * env(i, n, 0.003, 0.6))
    return out


def sfx_build():
    """Xây tháp: hai nốt đi lên, nghe ra 'đã đặt xuống'."""
    n = int(0.26 * SR)
    out = []
    for i in range(n):
        t = i / SR
        f = 523.25 if t < 0.09 else 783.99
        s = 0.7 * tone(f, t, "tri") + 0.3 * tone(f * 2, t, "sine")
        out.append(0.45 * s * env(i, n, 0.004, 0.5))
    return out


def sfx_base_hit():
    """Base ăn đòn: cú thịch trầm, đủ nặng để giật mình."""
    n = int(0.42 * SR)
    rnd = random.Random(3)
    out = []
    for i in range(n):
        t = i / SR
        f = 140 - 80 * (i / n)
        s = 0.75 * tone(f, t, "sine") + 0.25 * rnd.uniform(-1, 1) * (1 - i / n) ** 2
        out.append(0.62 * s * env(i, n, 0.002, 0.55))
    return out


def sfx_error():
    """Không đủ tiền / đế đã có tháp: tiếng ù ngắn, khó chịu vừa đủ."""
    n = int(0.16 * SR)
    out = []
    for i in range(n):
        t = i / SR
        s = tone(150, t, "square") * (0.7 + 0.3 * math.sin(2 * math.pi * 28 * t))
        out.append(0.34 * s * env(i, n, 0.003, 0.45))
    return out


def _arpeggio(freqs, note_len, kind, amp, total=None):
    n_note = int(note_len * SR)
    n = total or n_note * len(freqs)
    out = [0.0] * n
    for k, f in enumerate(freqs):
        start = k * n_note
        for i in range(n_note):
            if start + i >= n:
                break
            t = i / SR
            s = 0.65 * tone(f, t, kind) + 0.35 * tone(f * 2, t, "sine")
            out[start + i] += amp * s * env(i, n_note, 0.005, 0.55)
    return out


def sfx_victory():
    return _arpeggio([523.25, 659.25, 783.99, 1046.50], 0.16, "tri", 0.5,
                     total=int(1.15 * SR))


def sfx_defeat():
    return _arpeggio([392.00, 329.63, 261.63, 196.00], 0.22, "saw", 0.42,
                     total=int(1.35 * SR))


# ───────────────────────────── nhạc nền ─────────────────────────────

LOOP = 16.0   # giây


def snap(freq):
    """Làm tròn tần số về bội số của 1/LOOP để vòng lặp khép kín không bị 'cụp'."""
    return max(1, round(freq * LOOP)) / LOOP


def music_loop():
    """Nền u ám nhè nhẹ: bè trầm giữ nguyên + hợp âm chậm + vài tiếng chuông thưa.

    Cố ý nhạt và không có giai điệu bắt tai: nghe suốt cả màn chơi, nổi quá là
    mệt tai. Nhiệm vụ của nó chỉ là lấp khoảng lặng.
    """
    n = int(LOOP * SR)
    out = [0.0] * n

    # Bè trầm A1 + quãng năm, chạy suốt.
    for f, amp in ((snap(55.0), 0.30), (snap(82.41), 0.16)):
        for i in range(n):
            t = i / SR
            lfo = 0.85 + 0.15 * math.sin(2 * math.pi * snap(0.125) * t)
            out[i] += amp * tone(f, t, "sine") * lfo

    # Pad: bốn hợp âm, mỗi hợp âm 4 giây.
    chords = [
        (220.00, 261.63, 329.63),   # Am
        (196.00, 246.94, 293.66),   # G
        (174.61, 220.00, 261.63),   # F
        (196.00, 246.94, 329.63),   # Gsus
    ]
    seg = n // len(chords)
    for c, notes in enumerate(chords):
        for f in notes:
            fs = snap(f)
            for i in range(seg):
                idx = c * seg + i
                if idx >= n:
                    break
                t = idx / SR
                # vào/ra mượt trong từng đoạn để không nghe thấy chỗ chuyển hợp âm
                e = math.sin(math.pi * (i / seg)) ** 1.5
                out[idx] += 0.085 * tone(fs, t, "tri") * e

    # Chuông thưa, mỗi 2 giây một tiếng, rất nhỏ.
    bell_notes = [880.00, 659.25, 987.77, 659.25, 880.00, 1046.50, 783.99, 659.25]
    for k, f in enumerate(bell_notes):
        start = int(k * 2.0 * SR)
        dur = int(1.6 * SR)
        fs = snap(f)
        for i in range(dur):
            idx = start + i
            if idx >= n:
                break
            t = idx / SR
            decay = math.exp(-3.0 * (i / dur))
            out[idx] += 0.05 * tone(fs, t, "sine") * decay

    return out


# ───────────────────────────── chạy ─────────────────────────────

if __name__ == "__main__":
    print(f"Ghi vào {OUT}")
    write_wav("sfx_tower_shoot.wav", sfx_shoot())
    write_wav("sfx_enemy_death.wav", sfx_enemy_death())
    write_wav("sfx_build.wav", sfx_build())
    write_wav("sfx_base_hit.wav", sfx_base_hit())
    write_wav("sfx_error.wav", sfx_error())
    write_wav("sfx_victory.wav", sfx_victory())
    write_wav("sfx_defeat.wav", sfx_defeat())
    write_wav("music_loop.wav", music_loop())
    print("Xong.")
